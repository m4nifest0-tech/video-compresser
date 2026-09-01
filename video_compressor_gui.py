#!/usr/bin/env python3
"""
GUI per comprimere video in batch con l'encoder GPU NVIDIA (NVENC) via ffmpeg.

Requisiti sul PC Windows:
  - Python 3.8+ (da python.org, con "tcl/tk" incluso di default)
  - ffmpeg e ffprobe raggiungibili dal PATH (o nella stessa cartella dello script)
  - Driver NVIDIA aggiornati (RTX 4060 supporta sia h264_nvenc che hevc_nvenc)

Avvio:
  python video_compressor_gui.py
"""

import os
import re
import shutil
import subprocess
import threading
import queue
import tkinter as tk
from tkinter import ttk, filedialog, messagebox

VIDEO_EXTENSIONS = {".mp4", ".mov", ".avi", ".mkv", ".wmv", ".flv", ".webm", ".m4v", ".ts", ".3gp"}

COMPRESSION_LEVELS = [
    ("Qualita massima (file piu grande)", 18),
    ("Alta qualita", 23),
    ("Bilanciato (consigliato)", 28),
    ("Compressione alta", 32),
    ("Compressione massima (file piu piccolo)", 36),
]

CODECS = [
    ("H.264 (massima compatibilita)", "h264_nvenc"),
    ("H.265 / HEVC (file piu piccoli)", "hevc_nvenc"),
]

TIME_RE = re.compile(r"out_time=(\d+):(\d+):(\d+)\.(\d+)")

# Flag di creazione processo per non far comparire finestre console su Windows
CREATE_NO_WINDOW = 0x08000000 if os.name == "nt" else 0


def which_or_raise(name):
    path = shutil.which(name)
    return path if path else name


def human_size(num_bytes):
    if num_bytes is None:
        return "-"
    step = 1024.0
    for unit in ("B", "KB", "MB", "GB"):
        if num_bytes < step:
            return f"{num_bytes:.1f} {unit}"
        num_bytes /= step
    return f"{num_bytes:.1f} TB"


def get_duration_seconds(path, ffprobe_path):
    try:
        out = subprocess.run(
            [ffprobe_path, "-v", "error", "-show_entries", "format=duration",
             "-of", "default=noprint_wrappers=1:nokey=1", path],
            capture_output=True, text=True, timeout=30,
            creationflags=CREATE_NO_WINDOW,
        )
        return float(out.stdout.strip())
    except Exception:
        return None


def unique_dest_path(dest_path):
    if not os.path.exists(dest_path):
        return dest_path
    base, ext = os.path.splitext(dest_path)
    i = 1
    while True:
        candidate = f"{base} ({i}){ext}"
        if not os.path.exists(candidate):
            return candidate
        i += 1


class VideoItem:
    def __init__(self, src, base_root=None):
        self.src = src
        self.base_root = base_root  # None se aggiunto come file singolo
        self.size = os.path.getsize(src) if os.path.exists(src) else 0
        self.dest = None
        self.status = "In attesa"
        self.result_size = None
        self.row_id = None


class CompressorApp:
    def __init__(self, root):
        self.root = root
        root.title("Compressore Video (GPU NVENC)")
        root.geometry("880x620")
        root.minsize(780, 560)

        self.items = []
        self.ui_queue = queue.Queue()
        self.cancel_event = threading.Event()
        self.worker_thread = None
        self.current_proc = None

        self.ffmpeg_path = which_or_raise("ffmpeg")
        self.ffprobe_path = which_or_raise("ffprobe")

        self._build_ui()
        self.root.after(100, self._poll_queue)
        self.root.protocol("WM_DELETE_WINDOW", self._on_close)

    # ---------- UI ----------
    def _build_ui(self):
        top = ttk.Frame(self.root, padding=8)
        top.pack(fill="x")

        ttk.Button(top, text="Aggiungi file...", command=self.add_files).pack(side="left", padx=2)
        ttk.Button(top, text="Aggiungi cartella...", command=self.add_folder).pack(side="left", padx=2)
        ttk.Button(top, text="Rimuovi selezionati", command=self.remove_selected).pack(side="left", padx=2)
        ttk.Button(top, text="Svuota lista", command=self.clear_list).pack(side="left", padx=2)

        columns = ("size", "status", "progress", "result")
        self.tree = ttk.Treeview(self.root, columns=columns, show="tree headings", height=14)
        self.tree.heading("#0", text="File")
        self.tree.heading("size", text="Dimensione originale")
        self.tree.heading("status", text="Stato")
        self.tree.heading("progress", text="Avanzamento")
        self.tree.heading("result", text="Dimensione finale")
        self.tree.column("#0", width=340)
        self.tree.column("size", width=120, anchor="e")
        self.tree.column("status", width=100, anchor="center")
        self.tree.column("progress", width=100, anchor="center")
        self.tree.column("result", width=120, anchor="e")
        self.tree.pack(fill="both", expand=True, padx=8, pady=4)

        dest_frame = ttk.Frame(self.root, padding=(8, 4))
        dest_frame.pack(fill="x")
        ttk.Label(dest_frame, text="Cartella destinazione:").pack(side="left")
        self.dest_var = tk.StringVar()
        ttk.Entry(dest_frame, textvariable=self.dest_var).pack(side="left", fill="x", expand=True, padx=6)
        ttk.Button(dest_frame, text="Sfoglia...", command=self.choose_dest).pack(side="left")

        opts_frame = ttk.Frame(self.root, padding=(8, 4))
        opts_frame.pack(fill="x")

        ttk.Label(opts_frame, text="Codec:").grid(row=0, column=0, sticky="w")
        self.codec_var = tk.StringVar(value=CODECS[0][0])
        ttk.Combobox(opts_frame, textvariable=self.codec_var, values=[c[0] for c in CODECS],
                     state="readonly", width=32).grid(row=0, column=1, sticky="w", padx=6)

        ttk.Label(opts_frame, text="Livello compressione:").grid(row=0, column=2, sticky="w", padx=(16, 0))
        self.level_var = tk.StringVar(value=COMPRESSION_LEVELS[2][0])
        ttk.Combobox(opts_frame, textvariable=self.level_var, values=[l[0] for l in COMPRESSION_LEVELS],
                     state="readonly", width=36).grid(row=0, column=3, sticky="w", padx=6)

        self.preserve_structure_var = tk.BooleanVar(value=True)
        ttk.Checkbutton(opts_frame, text="Mantieni struttura cartelle (per elementi aggiunti da cartella)",
                         variable=self.preserve_structure_var).grid(row=1, column=0, columnspan=2, sticky="w", pady=(6, 0))

        self.skip_existing_var = tk.BooleanVar(value=True)
        ttk.Checkbutton(opts_frame, text="Salta se il file di destinazione esiste gia",
                         variable=self.skip_existing_var).grid(row=1, column=2, columnspan=2, sticky="w", pady=(6, 0))

        self.delete_source_var = tk.BooleanVar(value=False)
        ttk.Checkbutton(opts_frame, text="Elimina l'originale dopo compressione riuscita (irreversibile)",
                         variable=self.delete_source_var).grid(row=2, column=0, columnspan=4, sticky="w", pady=(6, 0))

        prog_frame = ttk.Frame(self.root, padding=8)
        prog_frame.pack(fill="x")
        self.overall_progress = ttk.Progressbar(prog_frame, orient="horizontal", mode="determinate")
        self.overall_progress.pack(fill="x")
        self.status_label = ttk.Label(prog_frame, text="Pronto.")
        self.status_label.pack(anchor="w", pady=(4, 0))

        btn_frame = ttk.Frame(self.root, padding=8)
        btn_frame.pack(fill="x")
        self.start_btn = ttk.Button(btn_frame, text="Avvia compressione", command=self.start)
        self.start_btn.pack(side="left")
        self.cancel_btn = ttk.Button(btn_frame, text="Annulla", command=self.cancel, state="disabled")
        self.cancel_btn.pack(side="left", padx=6)

    # ---------- gestione lista file ----------
    def add_files(self):
        paths = filedialog.askopenfilenames(title="Seleziona video")
        for p in paths:
            self.items.append(VideoItem(p, base_root=None))
        self._refresh_tree()

    def add_folder(self):
        folder = filedialog.askdirectory(title="Seleziona cartella")
        if not folder:
            return
        found = []
        for dirpath, _, filenames in os.walk(folder):
            for fn in filenames:
                if os.path.splitext(fn)[1].lower() in VIDEO_EXTENSIONS:
                    found.append(os.path.join(dirpath, fn))
        if not found:
            messagebox.showinfo("Nessun video trovato", "Nessun file video trovato in questa cartella.")
            return
        for p in sorted(found):
            self.items.append(VideoItem(p, base_root=folder))
        self._refresh_tree()

    def remove_selected(self):
        selected = set(self.tree.selection())
        self.items = [it for it in self.items if it.row_id not in selected]
        self._refresh_tree()

    def clear_list(self):
        self.items = []
        self._refresh_tree()

    def _refresh_tree(self):
        self.tree.delete(*self.tree.get_children())
        for it in self.items:
            it.row_id = self.tree.insert("", "end", text=it.src,
                                          values=(human_size(it.size), it.status, "", ""))

    def choose_dest(self):
        folder = filedialog.askdirectory(title="Seleziona cartella di destinazione")
        if folder:
            self.dest_var.set(folder)

    # ---------- avvio / annullamento ----------
    def start(self):
        if not self.items:
            messagebox.showwarning("Lista vuota", "Aggiungi almeno un file o una cartella.")
            return
        dest_dir = self.dest_var.get().strip()
        if not dest_dir:
            messagebox.showwarning("Destinazione mancante", "Scegli una cartella di destinazione.")
            return
        if not shutil.which("ffmpeg"):
            messagebox.showerror("ffmpeg non trovato",
                                  "ffmpeg non e stato trovato nel PATH. Installalo e riprova.")
            return

        os.makedirs(dest_dir, exist_ok=True)

        self.cancel_event.clear()
        self.start_btn.config(state="disabled")
        self.cancel_btn.config(state="normal")
        self.overall_progress.config(value=0, maximum=len(self.items))

        codec_label = self.codec_var.get()
        codec = next(c[1] for c in CODECS if c[0] == codec_label)
        level_label = self.level_var.get()
        cq = next(l[1] for l in COMPRESSION_LEVELS if l[0] == level_label)

        self.worker_thread = threading.Thread(
            target=self._worker,
            args=(dest_dir, codec, cq, self.preserve_structure_var.get(),
                  self.skip_existing_var.get(), self.delete_source_var.get()),
            daemon=True,
        )
        self.worker_thread.start()

    def cancel(self):
        self.cancel_event.set()
        if self.current_proc and self.current_proc.poll() is None:
            self.current_proc.terminate()
        self.status_label.config(text="Annullamento in corso...")

    def _on_close(self):
        if self.worker_thread and self.worker_thread.is_alive():
            if not messagebox.askyesno("Compressione in corso",
                                        "Una compressione e in corso. Vuoi davvero uscire?"):
                return
            self.cancel()
        self.root.destroy()

    # ---------- worker thread ----------
    def _compute_dest(self, item, dest_dir, preserve_structure):
        if preserve_structure and item.base_root:
            rel = os.path.relpath(item.src, item.base_root)
            rel_no_ext = os.path.splitext(rel)[0] + ".mp4"
            dest = os.path.join(dest_dir, rel_no_ext)
        else:
            name = os.path.splitext(os.path.basename(item.src))[0] + ".mp4"
            dest = os.path.join(dest_dir, name)
        return dest

    def _worker(self, dest_dir, codec, cq, preserve_structure, skip_existing, delete_source):
        for idx, item in enumerate(self.items):
            if self.cancel_event.is_set():
                self.ui_queue.put(("item_status", item.row_id, "Annullato", ""))
                continue

            dest = self._compute_dest(item, dest_dir, preserve_structure)

            if os.path.exists(dest):
                if skip_existing:
                    item.status = "Saltato"
                    self.ui_queue.put(("item_status", item.row_id, "Saltato", "100%"))
                    self.ui_queue.put(("overall", idx + 1))
                    continue
                dest = unique_dest_path(dest)

            os.makedirs(os.path.dirname(dest), exist_ok=True)
            item.dest = dest

            self.ui_queue.put(("item_status", item.row_id, "In corso", "0%"))
            self.ui_queue.put(("status_text", f"[{idx + 1}/{len(self.items)}] {os.path.basename(item.src)}"))

            duration = get_duration_seconds(item.src, self.ffprobe_path)

            cmd = [
                self.ffmpeg_path, "-y", "-hide_banner", "-loglevel", "error",
                "-i", item.src,
                "-c:v", codec,
                "-preset", "p7",
                "-rc", "vbr",
                "-cq", str(cq),
                "-b:v", "0",
                "-spatial-aq", "1",
                "-temporal-aq", "1",
                "-aq-strength", "8",
                "-rc-lookahead", "20",
                "-c:a", "aac",
                "-b:a", "160k",
                "-movflags", "+faststart",
                "-progress", "pipe:1",
                "-nostats",
                dest,
            ]

            try:
                self.current_proc = subprocess.Popen(
                    cmd, stdout=subprocess.PIPE, stderr=subprocess.DEVNULL,
                    text=True, bufsize=1, creationflags=CREATE_NO_WINDOW,
                )
            except FileNotFoundError:
                self.ui_queue.put(("item_status", item.row_id, "Errore: ffmpeg non trovato", ""))
                continue

            for line in self.current_proc.stdout:
                if self.cancel_event.is_set():
                    self.current_proc.terminate()
                    break
                match = TIME_RE.search(line)
                if match and duration:
                    h, m, s, frac = match.groups()
                    elapsed = int(h) * 3600 + int(m) * 60 + int(s) + float(f"0.{frac}")
                    pct = max(0, min(100, elapsed / duration * 100))
                    self.ui_queue.put(("item_status", item.row_id, "In corso", f"{pct:.0f}%"))

            self.current_proc.wait()
            returncode = self.current_proc.returncode
            self.current_proc = None

            if self.cancel_event.is_set():
                self.ui_queue.put(("item_status", item.row_id, "Annullato", ""))
                if os.path.exists(dest):
                    try:
                        os.remove(dest)
                    except OSError:
                        pass
                continue

            if returncode != 0:
                self.ui_queue.put(("item_status", item.row_id, "Errore", ""))
                self.ui_queue.put(("overall", idx + 1))
                continue

            result_size = os.path.getsize(dest) if os.path.exists(dest) else None
            item.result_size = result_size
            self.ui_queue.put(("item_done", item.row_id, human_size(result_size)))

            if delete_source and result_size:
                try:
                    os.remove(item.src)
                except OSError as e:
                    self.ui_queue.put(("status_text", f"Impossibile eliminare originale: {e}"))

            self.ui_queue.put(("overall", idx + 1))

        self.ui_queue.put(("finished", None))

    # ---------- polling coda UI ----------
    def _poll_queue(self):
        try:
            while True:
                msg = self.ui_queue.get_nowait()
                kind = msg[0]
                if kind == "item_status":
                    _, row_id, status, progress = msg
                    self.tree.set(row_id, "status", status)
                    self.tree.set(row_id, "progress", progress)
                elif kind == "item_done":
                    _, row_id, result_size_text = msg
                    self.tree.set(row_id, "status", "Completato")
                    self.tree.set(row_id, "progress", "100%")
                    self.tree.set(row_id, "result", result_size_text)
                elif kind == "overall":
                    _, done_count = msg
                    self.overall_progress.config(value=done_count)
                elif kind == "status_text":
                    _, text = msg
                    self.status_label.config(text=text)
                elif kind == "finished":
                    self.start_btn.config(state="normal")
                    self.cancel_btn.config(state="disabled")
                    self.status_label.config(text="Completato." if not self.cancel_event.is_set() else "Annullato.")
        except queue.Empty:
            pass
        self.root.after(100, self._poll_queue)


def main():
    root = tk.Tk()
    try:
        style = ttk.Style()
        if "vista" in style.theme_names():
            style.theme_use("vista")
    except Exception:
        pass
    CompressorApp(root)
    root.mainloop()


if __name__ == "__main__":
    main()
