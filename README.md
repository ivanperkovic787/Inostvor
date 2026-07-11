# Inostvor

Windows desktop CAM aplikacija za CNC plazma rezanje. Potpuno offline.

**Stack:** C# / .NET 9 / WinUI 3 / MVVM / SQLite / SkiaSharp / xUnit

## Preduvjeti za build

- Windows 10 1809+ / Windows 11
- Visual Studio 2022 17.12+ s workloadovima:
  - **.NET desktop development**
  - **Windows application development** (WinUI 3 / Windows App SDK)
- .NET 9 SDK

## Build i testovi

```powershell
.\tools\build.ps1          # restore + build (Release) + svi testovi
```

ili ručno:

```powershell
dotnet build Inostvor.sln -c Release
dotnet test  Inostvor.sln -c Release
```

Pokretanje aplikacije: `Inostvor.App` kao startup projekt (x64), F5.

## Struktura

- `src/` — produkcijski kod (vidi `docs/ARCHITECTURE.md`, Architecture Baseline v1.1)
- `tests/` — unit testovi + `TestData/` referentni DXF-ovi
- `benchmarks/` — BenchmarkDotNet mjerenja (nije dio aplikacije)
- `docs/` — arhitektura, ADR zapisi, izvještaji modula

## Pravila razvoja

1. Arhitektura je zamrznuta kao **Baseline v1.1** — izmjene isključivo kroz ADR u `docs/adr/`.
2. Verzije NuGet paketa isključivo u `Directory.Packages.props` (CPM).
3. Svaki ispravljeni bug dobiva regression DXF u `tests/TestData/Regression/`.
4. Bez TODO komentara. Modul se dovršava u cijelosti prije sljedećeg.
5. Sve mutacije dokumenta (CutJob) idu kroz `IUndoableCommand` — nikad direktno.
