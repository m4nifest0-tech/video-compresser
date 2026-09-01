# Video Compresser

Strumenti per comprimere video in batch con l'encoder GPU NVIDIA (NVENC) tramite ffmpeg, pensati per un PC Windows con scheda video RTX.

## VideoCompressorApp/ (consigliato)

App desktop **C# / WPF (.NET 8)** con interfaccia grafica: selezione multipla di file o cartelle, scelta del livello di compressione e del codec (H.264/H.265), cartella di destinazione, avanzamento per file e complessivo.

Build/pubblicazione come `.exe` singolo self-contained (richiede [.NET 8 SDK](https://dotnet.microsoft.com/download)):

```bash
cd VideoCompressorApp
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

L'eseguibile viene generato in `bin/Release/net8.0-windows/win-x64/publish/VideoCompressor.exe`.

Richiede `ffmpeg` e `ffprobe` raggiungibili dal PATH di sistema.

## video_compressor_gui.py

Prototipo equivalente in Python/tkinter (nessuna dipendenza esterna oltre a Python stesso).

## Script originali (comprimi.ps1, img/Identifica-Video.ps1, img/compress_mov.bat)

Script PowerShell/batch di partenza, mantenuti per riferimento.
