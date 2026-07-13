# Inostvor build skripta — restore + build + test, staje na prvoj gresci.
# Pokretanje iz korijena repozitorija:  .\tools\build.ps1
$ErrorActionPreference = "Stop"
$sln = Join-Path $PSScriptRoot "..\Inostvor.sln"

Write-Host "=== 1/3 RESTORE ===" -ForegroundColor Cyan
dotnet restore $sln
if ($LASTEXITCODE -ne 0) { throw "Restore neuspjesan (najvjerojatnije verzija paketa u Directory.Packages.props)." }

Write-Host "=== 2/3 BUILD ===" -ForegroundColor Cyan
dotnet build $sln -c Debug --no-restore
if ($LASTEXITCODE -ne 0) { throw "Build neuspjesan." }

Write-Host "=== 3/3 TESTOVI ===" -ForegroundColor Cyan
# App (WinUI) se ne testira; testni projekti su net9.0 i rade na svakom Windowsu.
dotnet test $sln -c Debug --no-build --logger "console;verbosity=minimal"
if ($LASTEXITCODE -ne 0) { throw "Testovi nisu prosli." }

Write-Host "`nSVE PROSLO." -ForegroundColor Green
