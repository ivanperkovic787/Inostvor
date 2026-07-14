# Gradi i pokrece WinUI aplikaciju.
# Koristi ovo umjesto build.ps1 dok rasplecemo XAML/WinUI greske —
# jezgra je vec zelena, nema smisla je graditi svaki put.
$ErrorActionPreference = "Stop"
$app = Join-Path $PSScriptRoot "..\src\Inostvor.App"

Write-Host "=== BUILD Inostvor.App (WinUI) ===" -ForegroundColor Cyan
dotnet build $app -c Debug
if ($LASTEXITCODE -ne 0) { throw "Build aplikacije neuspjesan." }

Write-Host "`n=== POKRETANJE ===" -ForegroundColor Green
Write-Host "Log datoteka: $env:LOCALAPPDATA\Inostvor\logs\" -ForegroundColor DarkGray
dotnet run --project $app -c Debug --no-build
