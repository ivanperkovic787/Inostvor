# Generira golden datoteke G-koda iz STVARNOG izlaza postprocesora.
#
# PAŽNJA: ovo je "blagoslov" — pokrenuti SAMO nakon što si PREGLEDAO izlaz i
# potvrdio da je G-kod ispravan. Od tog trenutka svaka promjena i jednog znaka
# ruši golden test, što je i svrha.
$ErrorActionPreference = "Stop"
$root  = Join-Path $PSScriptRoot ".."
$proj  = Join-Path $root "tools\GoldenBless"

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
    <ProjectReference Include="..\..\src\Inostvor.Post\Inostvor.Post.csproj" />
  </ItemGroup>
</Project>
'@ | Set-Content (Join-Path $proj "GoldenBless.csproj")
}

# Program.cs se uvijek prepisuje (mora pratiti GoldenTestProgram)
@'
using Inostvor.Core.Model.Machines;
using Inostvor.Core.Model.Toolpath;
using Inostvor.Kernel.Primitives;
using Inostvor.Post;
using Inostvor.Post.Plugins;
using Inostvor.Sdk.Post;

// ISTI program kao GoldenTestProgram u testovima.
const double feed = 3000.0;
var tech = TechnologySettings.Default with { FeedRate = feed, RapidRate = 6000.0, PierceTime = 0.6 };

var seq1 = new CutSequence(7, new Point2(10, 15),
[
    new CutMove(new LineSeg(new Point2(10, 15), new Point2(10, 20)), MoveKind.LeadIn, feed),
    new CutMove(new LineSeg(new Point2(10, 20), new Point2(60, 20)), MoveKind.Cut, feed),
    new CutMove(new LineSeg(new Point2(60, 20), new Point2(60, 70)), MoveKind.Cut, feed),
    new CutMove(new LineSeg(new Point2(60, 70), new Point2(65, 70)), MoveKind.LeadOut, feed),
]);
var seq2 = new CutSequence(9, new Point2(100, 0),
[
    new CutMove(new ArcSeg(new Point2(100, 10), 10, -Math.PI / 2.0, Math.PI), MoveKind.Cut, feed),
]);
var rapids = new List<RapidMove>
{
    new(new Point2(0, 0), new Point2(10, 15)),
    new(new Point2(65, 70), new Point2(100, 0)),
};
var cutLength = seq1.CutLength + seq2.CutLength;
var rapidLength = rapids.Sum(r => r.Length);
var stats = new ToolpathStatistics(cutLength, rapidLength, cutLength / feed * 60.0, rapidLength / 6000.0 * 60.0, 2 * 0.6, 2);
var program = new ToolpathProgram([seq1, seq2], rapids, tech, stats);

var outDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "tests", "Inostvor.Post.Tests", "GoldenFiles"));
Directory.CreateDirectory(outDir);

void Bless(IPostProcessorPlugin plugin, MachineProfile profile, string fileName)
{
    var gcode = plugin.Create(plugin.DefaultDialect, profile).Generate(program).GCode;
    var path = Path.Combine(outDir, fileName);
    File.WriteAllText(path, gcode);

    Console.WriteLine($"=== {fileName} ===");
    Console.WriteLine(gcode);
    Console.WriteLine($"--- zapisano: {path}\n");
}

Bless(new Mach3PostPlugin(), BuiltInMachineProfiles.Mach3Plasma, "mach3_basic.tap");
Bless(new Ec300PostPlugin(), BuiltInMachineProfiles.Ec300Plasma, "ec300_basic.tap");

Console.WriteLine("PREGLEDAJ izlaz gore. Ako je G-kod ispravan, golden datoteke su blagoslovljene.");
'@ | Set-Content (Join-Path $proj "Program.cs")

Write-Host "=== Generiranje golden datoteka iz stvarnog izlaza ===" -ForegroundColor Cyan
dotnet run --project $proj -c Debug
