# Dijagnostika: mjeri koliko traje uvoz SVAKE testne DXF datoteke pojedinacno.
# Cilj: izolirati datoteku koja "visi" u netDxf parseru.
$ErrorActionPreference = "Stop"
$root = Join-Path $PSScriptRoot ".."
$proj = Join-Path $root "tools\ImportDiag"

if (-not (Test-Path $proj)) {
  New-Item -ItemType Directory -Path $proj -Force | Out-Null

@'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Inostvor.Import.NetDxf\Inostvor.Import.NetDxf.csproj" />
  </ItemGroup>
</Project>
'@ | Set-Content (Join-Path $proj "ImportDiag.csproj")

@'
using System.Diagnostics;
using Inostvor.Import.NetDxf;

var testData = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "tests", "TestData"));
Console.WriteLine($"TestData: {testData}\n");

var importer = new NetDxfImporter();
foreach (var file in Directory.GetFiles(testData, "*.dxf", SearchOption.AllDirectories).OrderBy(f => f))
{
    var name = Path.GetFileName(file);
    var sw = Stopwatch.StartNew();
    string status;
    try
    {
        var task = Task.Run(() => importer.Import(file));
        if (task.Wait(TimeSpan.FromSeconds(10)))
        {
            var r = task.Result;
            status = r.Success ? $"OK   {r.Entities.Count,5} ent" : $"FAIL {r.Error?[..Math.Min(40, r.Error.Length)]}";
        }
        else
        {
            status = ">>> VISI (>10 s) <<<";
        }
    }
    catch (Exception ex)
    {
        status = $"IZNIMKA: {ex.GetType().Name}";
    }
    sw.Stop();

    var mark = sw.ElapsedMilliseconds > 1000 ? " <<<< SPOR" : "";
    Console.WriteLine($"{sw.ElapsedMilliseconds,7} ms  {name,-32} {status}{mark}");
}
'@ | Set-Content (Join-Path $proj "Program.cs")
}

Write-Host "=== Mjerenje uvoza svake DXF datoteke (timeout 10 s) ===" -ForegroundColor Cyan
dotnet run --project $proj -c Debug
