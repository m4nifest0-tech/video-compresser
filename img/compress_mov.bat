@echo off
setlocal enabledelayedexpansion

for /f "usebackq delims=" %%F in ("output.txt") do (
    set "input=%%F"
    set "filename=%%~nF"
    ffmpeg -hwaccel cuda -i "!input!" -c:v hevc_nvenc -preset p7 -rc vbr -cq 28 -b:v 0 -pix_fmt p010le -c:a aac -b:a 160k -movflags +faststart -r 120 "!filename!_compressed.mp4"
)

echo Operazione completata.
pause
