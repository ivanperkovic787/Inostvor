# XamlCompiler.exe ispisuje pravi razlog pada SAMO uz detaljni ispis.
# Ova skripta CISTI stare artefakte pa gradi s -v:detailed i izvlaci bitno.
$ErrorActionPreference = "Continue"
$root = Join-Path $PSScriptRoot ".."
$app  = Join-Path $root "src\Inostvor.App"
$log  = Join-Path $root "xaml-build.log"

Write-Host "=== Ciscenje obj/bin (stare reference mogu ostati zapamcene) ===" -ForegroundColor Cyan
Remove-Item (Join-Path $app "obj") -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $app "bin") -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "=== Build s detaljnim ispisom ===" -ForegroundColor Cyan
dotnet build $app -c Debug -v:detailed 2>&1 | Tee-Object -FilePath $log | Out-Null

Write-Host "`n=== 1) Neuspjele reference (najcesci uzrok) ===" -ForegroundColor Yellow
Select-String -Path $log -Pattern "Could not locate the assembly|Could not resolve this reference" |
  ForEach-Object { Write-Host $_.Line.Trim() -ForegroundColor Red }

Write-Host "`n=== 2) Ima li jos Uno paketa? ===" -ForegroundColor Yellow
$uno = Select-String -Path $log -Pattern "uno\.winui|Uno\.UI"
if ($uno) { Write-Host "DA - Uno je jos prisutan:" -ForegroundColor Red; $uno | Select-Object -First 3 | ForEach-Object { Write-Host $_.Line.Trim() } }
else { Write-Host "NE - Uno je uklonjen" -ForegroundColor Green }

Write-Host "`n=== 3) XAML greske (XLS/WMC kodovi) ===" -ForegroundColor Yellow
Select-String -Path $log -Pattern "XLS\d+|WMC\d+|Xaml Internal Error|XamlCompiler.*error" |
  Select-Object -First 20 | ForEach-Object { Write-Host $_.Line.Trim() -ForegroundColor Red }

Write-Host "`n=== 4) Koja SkiaSharp verzija se koristi? ===" -ForegroundColor Yellow
Select-String -Path $log -Pattern "skiasharp[^\\]*\\[0-9.]+" | Select-Object -First 3 |
  ForEach-Object { if ($_.Line -match "skiasharp[^\\]*\\([0-9.]+)") { Write-Host "  SkiaSharp $($matches[1])" } }

Write-Host "`n=== Puni log: $log ===" -ForegroundColor DarkGray
