$rootSource = "F:\img"                    
$rootDest = "F:\compressed\"          
$fileList = "output.txt"             

$ffmpegPath = "ffmpeg"                 

Get-Content $fileList | ForEach-Object {
    $sourceFile = $_.Trim()
    if (-Not (Test-Path $sourceFile)) {
        Write-Warning "File non trovato: $sourceFile"
        return
    }

    $relativePath = Resolve-Path $sourceFile | ForEach-Object {
        $_.Path.Substring($rootSource.Length)
    }

    $destFile = Join-Path $rootDest $relativePath

    $destDir = Split-Path $destFile
    if (-Not (Test-Path $destDir)) {
        New-Item -ItemType Directory -Path $destDir -Force | Out-Null
    }

    $arguments = @(
        '-i', "`"$sourceFile`""
        '-c:v', 'h264_nvenc'
        '-preset', 'p7'
        '-rc', 'vbr'
        '-cq', '28'
        '-b:v', '3M'
        '-maxrate', '4M'
        '-bufsize', '6M'
        '-spatial-aq', '1'
        '-temporal-aq', '1'
        '-aq-strength', '8'
        '-rc-lookahead', '20'
        '-c:a', 'aac'
        '-b:a', '128k'
        "`"$destFile`""
    )

    Write-Host "Comprimendo con GPU (ottimizzato): $sourceFile"
    & $ffmpegPath @arguments
}
