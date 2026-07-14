# Prvi build — što očekivati i kako raspetljati

Cijeli kod (M0–M8.1) napisan je bez ijedne kompilacije: okruženje u kojem je
nastao nema .NET SDK ni pristup nuget.org. Statičke provjere su zelene (balans
zagrada, XML, CPM konzistentnost, izolacija vanjskih API-ja, bez TODO markera),
ali **prvi build sigurno neće proći iz prve**. Ovo je popis poznatih rizika,
poredan po vjerojatnosti, sa spremnim rješenjima.

## Redoslijed

```powershell
.\tools\build-core-only.ps1   # 1) jezgra bez WinUI — rasplesti ovdje
.\tools\build.ps1             # 2) cijeli solution s aplikacijom
```

Jezgru rasplesti prvu: 8 od 11 projekata i 296 testova ne ovisi o Windows App SDK-u.

## Rizik 1 — verzije NuGet paketa (najvjerojatniji)

Sve verzije u `Directory.Packages.props` odabrane su iz sjećanja, ne s nuget.org.
Ako `restore` padne, ispravi verziju u TOJ jednoj datoteci:

| Paket | Pinnano | Ako padne |
|---|---|---|
| netDxf | 3.0.1 | provjeri stvarnu zadnju 3.x |
| Clipper2 | 1.5.4 | paket se možda zove `Clipper2` ili `Clipper2Lib` |
| SkiaSharp + SkiaSharp.Views.WinUI | **3.119.0** | 2.88.x POVLACI UNO PLATFORM i puca uz WinAppSDK 1.7 (vidi nize) |
| Dapper | 2.1.66 | bilo koja 2.1.x |
| Microsoft.Data.Sqlite | 9.0.0 | mora pratiti .NET 9 |
| CommunityToolkit.Mvvm | (iz M0) | 8.x |

## Rizik 2 — netDxf API površina

Sav netDxf kod je u **jednoj** datoteci: `src/Inostvor.Import.NetDxf/NetDxfImporter.cs`
(header datoteke navodi pretpostavljenu API površinu). Očekivana odstupanja:
- `Polyline2D` / `Polyline3D` vs. starije `LwPolyline` / `Polyline`
- `Spline.PolygonalVertexes(int)` potpis, `Spline.IsClosed`
- `Ellipse.MajorAxis` / `MinorAxis` — je li PUNA duljina osi ili poluos
  (test `ellipse_full` to lovi: bounds moraju biti 20,15 → 80,45)
- `doc.Entities.All`, `DrawingUnits` članovi, `CheckDxfFileVersion` potpis

## Rizik 3 — Clipper2 API površina

Sav Clipper kod je u **jednoj** datoteci: `src/Inostvor.Cam/Offset/ClipperAdapter.cs`.
Provjeri potpise: `Clipper.InflatePaths(Paths64, double, JoinType, EndType)`,
`Clipper.Area(Path64)`, `Point64(long, long)`.

## POTVRDJENO: SkiaSharp 2.88.x NE RADI s Windows App SDK 1.7

`SkiaSharp.Views.WinUI` **2.88.9** povlaci **Uno Platform** kao tranzitivnu ovisnost.
Njegov `SkiaSharp.Views.WinUI.Native.Projection` se ne moze razrijesiti uz WinAppSDK 1.7,
pa XAML kompajler pada s beskorisnom porukom `MSB3073: XamlCompiler.exe exited with code 1`.
Pravi razlog vidljiv je samo uz `-v:detailed`:

```
Could not locate the assembly "SkiaSharp.Views.WinUI.Native.Projection"
/reference:...uno.winui\5.2.132\lib\...\Uno.UI.Toolkit.dll
```

**Rjesenje:** SkiaSharp **3.119.0** — ima ispravnu WinUI podrsku bez Uno ovisnosti.
API koji koristimo (SKPaint, SKPathEffect, SKXamlCanvas, SKPaintSurfaceEventArgs) je
nepromijenjen.

## Rizik 4 — WinUI 3 / XAML

`x:Bind` na duboke putanje i `SKXamlCanvas` mogu tražiti sitne korekcije. Ako
XAML kompajler prigovara, `App` je jedini projekt koji to dodiruje — jezgra ostaje
zelena.

## Rizik 5 — golden datoteke G-koda

`tests/Inostvor.Post.Tests/GoldenFiles/*.tap` ručno su izračunate. Ako testovi
padnu, **prvo pregledaj stvarni izlaz** (test ispisuje razliku): ako je izlaz
ispravan G-kod, jednom "blagoslovi" datoteku (prepiši je stvarnim izlazom). Od
tada svaki promijenjeni znak ruši test — što je i svrha.

## Rizik 6 — nullable / analyzer upozorenja kao greške

Ako `TreatWarningsAsErrors` blokira build zbog analyzer pravila (CA/IDE), privremeno
ga isključi u `Directory.Build.props`, dovrši funkcionalni build, pa vrati i sredi
upozorenja jedno po jedno.

## Što NE dirati pri raspetljavanju

- Determinističke garancije (normalizacija kerf prstenova, sortiranje
  ValidationReporta, `CacheKey.PipelineVersion`) — to su ugovorne obveze, ne detalji.
- Ako **moraš** promijeniti bilo što u CAM cjevovodu što mijenja izlaz → povećaj
  `CacheKey.PipelineVersion` (ADR-006).
