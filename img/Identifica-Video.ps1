# Parametri configurabili
$rootPath = "F:\img\"                  
$outputFile = "F:\img\output.txt"      

# Estensioni video comuni
$videoExtensions = @(".mp4", ".mov", ".avi", ".mkv", ".wmv", ".flv", ".webm")

# Funzione per ottenere dimensione leggibile
function Get-ReadableSize {
    param ([long]$bytes)
    switch ($bytes) {
        {$_ -ge 1GB} { "{0:N1} GB" -f ($bytes / 1GB); break }
        {$_ -ge 1MB} { "{0:N1} MB" -f ($bytes / 1MB); break }
        {$_ -ge 1KB} { "{0:N1} KB" -f ($bytes / 1KB); break }
        default { "$bytes Bytes" }
    }
}

# Lista risultati
$resultList = @()

# Scansione file
Get-ChildItem -Path $rootPath -Recurse -File | Where-Object {
    $videoExtensions -contains $_.Extension.ToLower()
} | ForEach-Object {
    $sizeReadable = Get-ReadableSize $_.Length
    $formattedLine = "{0,-65} - {1}" -f $_.FullName, $sizeReadable
    $resultList += $formattedLine
}

# Output su file e console
$resultList | Sort-Object | Out-File -FilePath $outputFile -Encoding UTF8
$resultList | Sort-Object
