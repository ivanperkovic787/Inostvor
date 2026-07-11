# M2 — DXF import (izvještaj modula)

## Cilj

Robusan import DXF geometrije u interne primitive (mm, world koordinate), kroz
parser-neutralne kontrakte koji omogućuju dodavanje novih importera BEZ ijedne
izmjene CAM jezgre.

## Arhitektura zamjenjivosti

- **Kontrakt:** `Core.Abstractions.IDxfImporter` → `ImportResult` (Result pattern,
  bez iznimki prema pozivatelju). `ImportResult`/`ImportedEntity`/`ImportWarning`/
  `ImportSettings` ne sadrže NIJEDAN tip iz netDxf biblioteke.
- **Plugin put:** `Sdk.Import.IImportPlugin` — ugrađeni netDxf importer registrira
  se kroz isti kontrakt kao budući vanjski plugini; DI: `IImportPlugin` →
  `CreateImporter()` → `IDxfImporter`.
- **Izolacija adaptera:** netDxf API dodiruje ISKLJUČIVO `NetDxfImporter.cs`
  (header datoteke navodi pretpostavljenu API površinu za brz popravak pri buildu).

## Pokriveno (zahtjevi iz odobrenja M1)

| Zahtjev | Implementacija | Test |
|---|---|---|
| INSERT entiteti | rekurzivno raspakiravanje, `p' = Pos + Rot·Scale·(p − Base)` | insert_scale2_rot90 |
| Blokovi | 3 razine ugnježđenja, zaštita od ciklusa i dubine (32) | blocks_3_levels |
| Transformacije blokova | konformne analitički (luk ostaje luk, zrcaljenje obrće smjer); neuniformna skala → tessellacija + upozorenje | insert_mirrored, insert_nonuniform_scale |
| Layer nasljeđivanje | entitet bloka na layeru "0" preuzima layer INSERT-a | insert_layer0_inherit |
| DXF jedinice | $INSUNITS → skala u mm (in/ft/cm/m); unitless → mm + upozorenje | units_inches, units_unitless |
| SPLINE tessellacija | adaptivno uzorkovanje s konvergencijom duljine (64→1024) | spline_wave, spline_closed |
| Otvorene/zatvorene polilinije | bulge → ArcSeg.FromBulge; IsClosed → zatvarajući segment | rect_lwpolyline, slot_bulge, open_polyline |
| Degenerirani entiteti | pre-check + preskakanje uz upozorenje, validni preživljavaju | degenerate_entities |
| OCS normale | ±Z podržano (−Z = zrcaljenje po X); ostalo preskočeno uz upozorenje | (jedinično kroz SegmentTransform) |
| Elipse | uzorkovanje korakom iz min. polumjera zakrivljenosti b²/a | ellipse_full, ellipse_arc |

## Poznata ograničenja (dokumentirano, ne skriveno)

1. **R12/R13/R14 nije podržan** — netDxf ograničenje (minimalno AutoCad2000).
   Fail s jasnom porukom. Stari CNC DXF-ovi su često R12 → ako se pokaže čestim,
   dodaje se drugi importer (npr. ACadSharp) kroz isti IImportPlugin kontrakt,
   bez dodira CAM jezgre. To je točno scenarij za koji je arhitektura građena.
2. **Proizvoljne 3D normale** (nagnute ravnine) preskaču se uz upozorenje —
   plazma reže u XY.
3. **HATCH, TEXT, DIMENSION** i ostali ne-konturni entiteti → UNSUPPORTED_ENTITY
   upozorenje (namjerno: nisu rezna geometrija).

## netDxf API rizici (verificirati na prvom Windows buildu)

Pinnan **netDxf 3.0.1**. Bez mogućnosti kompilacije ovdje, sljedeće pretpostavke
mogu zahtijevati lokalni popravak u NetDxfImporter.cs (i samo tamo):
`Polyline2D`/`Polyline3D` imenovanje (starije verzije: LwPolyline/Polyline),
`Spline.PolygonalVertexes(int)` potpis i `Spline.IsClosed`,
`Ellipse.MajorAxis/MinorAxis` = pune duljine osi (test elipse to lovi),
`doc.Entities.All`, `DrawingUnits` članovi, `CheckDxfFileVersion` potpis.

## TestData

31 sintetska DXF datoteka (ezdxf 1.4.4, round-trip validirane), kategorije
Simple/Holes/Nested/OpenContours/Decorative/LargeFiles/Invalid; DXF verzije
R2000–R2018. Native vendor exporti idu u ByVendor/ kad budu dostupni
(vidi tests/TestData/README.md — sintetske datoteke ne reproduciraju vendor quirkove).

## Testovi i benchmarki

- Kernel.Tests: +11 (Tessellation 4, SegmentTransform 7)
- Import.NetDxf.Tests: 24 test metode (5 Theory slučajeva za verzije → 28 izvršavanja)
- ViewModels.Tests: +3 (OpenDxf komanda s NSubstitute mockovima)
- Benchmark: DxfImportBenchmarks (grid_2000_circles, end-to-end)

## Definition of Done

- [x] Parser-neutralni kontrakti; netDxf izoliran u jednu datoteku
- [x] Svi zahtjevi iz odobrenja pokriveni testom
- [x] Upozorenja s stabilnim kodovima + cap (200)
- [x] UI: Otvori DXF (toolbar) → sažetak u Output Console
- [x] Statičke provjere zelene; bez TODO markera
- [ ] Build + testovi na Windows stroju (autoritativna verifikacija)
