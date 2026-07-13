# Build i testovi BEZ WinUI aplikacije — korisno za brzo raspetljavanje
# gresaka u jezgri prije nego se dira Windows App SDK.
$ErrorActionPreference = "Stop"
$root = Join-Path $PSScriptRoot ".."

$projects = @(
  "src\Inostvor.Kernel", "src\Inostvor.Core", "src\Inostvor.Sdk",
  "src\Inostvor.Geometry", "src\Inostvor.Import.NetDxf", "src\Inostvor.Cam",
  "src\Inostvor.Post", "src\Inostvor.Data", "src\Inostvor.Rendering",
  "tests\Inostvor.Kernel.Tests", "tests\Inostvor.Core.Tests",
  "tests\Inostvor.Geometry.Tests", "tests\Inostvor.Import.NetDxf.Tests",
  "tests\Inostvor.Cam.Tests", "tests\Inostvor.Post.Tests",
  "tests\Inostvor.Data.Tests", "tests\Inostvor.Rendering.Tests"
)

foreach ($p in $projects) {
  $path = Join-Path $root $p
  Write-Host "--- BUILD $p" -ForegroundColor Cyan
  dotnet build $path -c Debug
  if ($LASTEXITCODE -ne 0) { throw "Build neuspjesan: $p" }
}

foreach ($p in ($projects | Where-Object { $_ -like "tests\*" })) {
  $path = Join-Path $root $p
  Write-Host "--- TEST $p" -ForegroundColor Cyan
  dotnet test $path -c Debug --no-build --logger "console;verbosity=minimal"
  if ($LASTEXITCODE -ne 0) { throw "Testovi nisu prosli: $p" }
}

Write-Host "`nJEZGRA PROSLA (bez WinUI aplikacije)." -ForegroundColor Green
