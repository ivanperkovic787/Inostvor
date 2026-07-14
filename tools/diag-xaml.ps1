# XamlCompiler.exe ispisuje pravi razlog pada SAMO uz detaljni ispis.
# Ova skripta gradi App s -v:detailed i izvlaci XAML greske iz buke.
$ErrorActionPreference = "Continue"
$app = Join-Path $PSScriptRoot "..\src\Inostvor.App"
$log = Join-Path $PSScriptRoot "..\xaml-build.log"

Write-Host "=== Build s detaljnim ispisom (potrajat ce) ===" -ForegroundColor Cyan
dotnet build $app -c Debug -v:detailed 2>&1 | Tee-Object -FilePath $log | Out-Null

Write-Host "`n=== XAML GRESKE ===" -ForegroundColor Yellow
Select-String -Path $log -Pattern "XamlCompiler|XLS\d+|WMC\d+|error|Xaml.*Exception" |
  Where-Object { $_.Line -notmatch "MSB3073|exited with code" } |
  Select-Object -First 40 |
  ForEach-Object { Write-Host $_.Line }

Write-Host "`n=== Puni log: $log ===" -ForegroundColor DarkGray
