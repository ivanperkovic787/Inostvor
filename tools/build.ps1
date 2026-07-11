# Inostvor build skripta - restore + build + test, staje na prvoj gresci.
$ErrorActionPreference = "Stop"
$sln = Join-Path $PSScriptRoot "..\Inostvor.sln"

Write-Host "== Restore ==" -ForegroundColor Cyan
dotnet restore $sln
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "== Build (Release, warnings = errors) ==" -ForegroundColor Cyan
dotnet build $sln -c Release --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "== Test ==" -ForegroundColor Cyan
dotnet test $sln -c Release --no-build --logger "console;verbosity=normal"
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "== OK: build i testovi prolaze ==" -ForegroundColor Green
