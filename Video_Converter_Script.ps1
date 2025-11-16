# Video Batch Converter Script
$inputFolder = "E:\Steam Recordings"
$outputFolder = "E:\Steam Recordings\stretched"
$processedFolder = Join-Path $inputFolder "processed"

Write-Host "Starting video conversion..." -ForegroundColor Cyan
Write-Host ""

if (!(Test-Path -Path $outputFolder)) {
    New-Item -ItemType Directory -Path $outputFolder | Out-Null
    Write-Host "Created output folder" -ForegroundColor Green
}

if (!(Test-Path -Path $processedFolder)) {
    New-Item -ItemType Directory -Path $processedFolder | Out-Null
    Write-Host "Created processed folder" -ForegroundColor Green
}

$videoFiles = Get-ChildItem -Path $inputFolder -Filter "*.mp4" -File

if ($videoFiles.Count -eq 0) {
    Write-Host "No video files found in: $inputFolder" -ForegroundColor Yellow
    exit
}

Write-Host "Found $($videoFiles.Count) video file(s) to process" -ForegroundColor Cyan
Write-Host ""

$counter = 0
foreach ($file in $videoFiles) {
    $counter++
    $inputPath = $file.FullName
    $outputPath = Join-Path -Path $outputFolder -ChildPath $file.Name
    $processedPath = Join-Path -Path $processedFolder -ChildPath $file.Name
    
    Write-Host "[$counter/$($videoFiles.Count)] Processing: $($file.Name)" -ForegroundColor Yellow
    
    & ffmpeg -i "$inputPath" -vf "setdar=16/9" -c:v libx265 -crf 18 -b:v 20000k -s 1920x1080 "$outputPath"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Successfully converted: $($file.Name)" -ForegroundColor Green
        Move-Item -Path $inputPath -Destination $processedPath -Force
        Write-Host "  Moved original to processed folder" -ForegroundColor Gray
    } else {
        Write-Host "Error converting: $($file.Name) - original kept in place" -ForegroundColor Red
    }
    Write-Host ""
}

Write-Host "Conversion complete! Processed $counter file(s)." -ForegroundColor Green
Write-Host "Original files moved to: $processedFolder" -ForegroundColor Cyan
