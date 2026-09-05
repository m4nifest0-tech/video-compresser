# Video Compresser

App desktop per comprimere video in batch con l'encoder GPU NVIDIA (NVENC) tramite ffmpeg, pensata per un PC Windows con scheda video RTX.

## VideoCompressorApp/

App desktop **C# / WPF (.NET 8)** con interfaccia grafica:

- Selezione multipla di file o cartelle (ricorsiva)
- Scelta del codec (H.264, H.265/HEVC, AV1 - richiede RTX serie 40+) e del livello di compressione
- Stima della compressione prima di avviare: codifica un breve campione reale di ogni file per prevedere la dimensione finale
- Cartella di destinazione, con opzione mantieni struttura cartelle
- Avanzamento per singolo file (con barra di progresso) e complessivo, con tempo rimanente stimato
- Temi Chiaro/Scuro con 4 colori accento (Blu, Verde, Viola, Arancione), salvati automaticamente
- Interfaccia web opzionale (scheda Impostazioni > "Accesso da remoto"): permette di caricare/scaricare video e pilotare la coda da un browser su un altro PC della stessa rete locale, con pagina di login dedicata (sessione via cookie, protezione anti brute-force), monitoraggio GPU in tempo reale (temperatura, utilizzo, memoria, potenza) tramite `nvidia-smi` e temi Chiaro/Scuro/Sistema con gli stessi 4 colori accento dell'app desktop, salvati per browser
- Controllo aggiornamenti integrato (scheda Impostazioni > "Aggiornamenti"): confronta la versione installata con l'ultima release GitHub e, se disponibile, scarica e installa l'aggiornamento con un click (l'app si riavvia da sola)

| Tema chiaro | Tema scuro |
|---|---|
| ![Tema chiaro](https://github.com/m4nifest0-tech/video-compresser/releases/download/v1.1.0/screenshot-light.png) | ![Tema scuro](https://github.com/m4nifest0-tech/video-compresser/releases/download/v1.1.0/screenshot-dark.png) |

Build/pubblicazione come `.exe` singolo self-contained (richiede [.NET 8 SDK](https://dotnet.microsoft.com/download)):

```bash
cd VideoCompressorApp
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

L'eseguibile viene generato in `bin/Release/net8.0-windows/win-x64/publish/VideoCompressor.exe`.

Richiede `ffmpeg` e `ffprobe` raggiungibili dal PATH di sistema.
