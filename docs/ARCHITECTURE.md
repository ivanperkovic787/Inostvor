# Inostvor — Arhitektura sustava (v1.1 — KONAČNA, za obostranu potvrdu)

**Status:** Ažurirano prema odobrenju + 6 izmjena. Čeka finalnu potvrdu → zatim M0.
**Datum:** 2026-07-09
**Promjene u odnosu na v1.0:** zamjenjiv DXF parser, novi Kernel projekt, ToolpathValidator, Undo/Redo (Command pattern), Plugin API (SDK), odluka o koordinatnom sustavu (ADR-001).

---

## 1. Načela arhitekture (nepromijenjeno + dopune)

1. Hexagonalna arhitektura — domena bez vanjskih ovisnosti, adapteri kroz interfejse.
2. CAM pipeline = čista, deterministička, testabilna funkcija.
3. UI tanak; ViewModeli bez matematike.
4. Postprocesor = dijalekt + quirkovi (EC300Post : Mach3Post).
5. **Novo:** Svaka točka proširenja (import, post, toolpath operacija, validacijska pravila) ima interfejs u SDK-u od prvog dana — plugin sustav u V3 je tada samo loader, ne refaktoring.
6. **Novo:** Sve mutacije dokumenta (CutJob) idu kroz Command objekte od prvog dana — Undo/Redo je tada samo stack, ne refaktoring.

---

## 2. ADR-001: Koordinatni sustav — double u domeni, Int64 na Clipper2 granici

**Odluka:** Domena i cijeli pipeline rade u `double` (mm). Cjelobrojne koordinate (Int64, 1 µm = ×1000) koriste se **isključivo unutar Clipper2 adaptera**.

**Obrazloženje:**

1. **Clipper2 je iznutra već Int64.** Njegov `PathsD` API prima double + parametar preciznosti, interno skalira u `Paths64`, radi robusnu integer aritmetiku i vraća double. Numeričku stabilnost integer pristupa dobivamo besplatno, točno tamo gdje je kritična (offset, booleove operacije) — bez da je propagiramo kroz sustav.
2. **Potpuno integer jezgra ubija lukove.** Centri lukova, radijusi, tangente leadova, biarc fitting, rotacijske transformacije — ništa od toga ne leži na integer mreži. Svaka operacija bi zahtijevala konverziju i zaokruživanje, što KUMULIRA grešku umjesto da je smanjuje. Integer jezgra ima smisla za čisto poligonalne CAM-ove (laser/glodanje 2D bez lukova u izlazu) — mi izričito želimo G2/G3.
3. **Preciznost doublea je dovoljna s ogromnom rezervom.** Double ima ~15,9 značajnih znamenki. Za tablu 3000 mm to znači rezoluciju ispod nanometra. DXF je double na ulazu, G-kod je decimalni mm (3-4 decimale) na izlazu — integer međusloj ne bi ništa dodao.
4. **Rizik doublea nije preciznost nego usporedbe** — i to rješavamo centralizirano: `Kernel.Tolerance` klasa je JEDINO mjesto s epsilon konstantama (geometrijska tolerancija 1e-6 mm, tolerancija spajanja kontura podesiva, default 0.05 mm). Zabranjeno je pisanje `a == b` ili lokalnih epsilona po kodu — analyzer pravilo.

**Implementacijska posljedica:** `KerfOffsetService` interno koristi `ClipperAdapter` koji radi konverziju mm↔µm (×1000, Int64) eksplicitno, s testovima rubnih slučajeva (koordinate > 10 m, kerf < 0.5 mm, rupe blizu veličine kerfa).

---

## 3. Konačna struktura solutiona

```
Inostvor.sln
│
├── src/
│   ├── Inostvor.Kernel/           # ★NOVO — čista matematika. Ovisnosti: NIŠTA.
│   ├── Inostvor.Core/             # Domena + apstrakcije. Ovisi: Kernel.
│   ├── Inostvor.Sdk/              # ★NOVO — javni Plugin API. Ovisi: Kernel, Core.
│   ├── Inostvor.Geometry/         # Konture, offset, leadovi. Ovisi: Kernel, Core, Clipper2.
│   ├── Inostvor.Import.NetDxf/    # ★PREIMENOVANO — prva IDxfImporter implementacija. Ovisi: Core, netDxf.
│   ├── Inostvor.Cam/              # Toolpath pipeline, VALIDATOR, redoslijed, simulacija. Ovisi: Kernel, Core, Geometry.
│   ├── Inostvor.Post/             # G-kod + postprocesori. Ovisi: Core.
│   ├── Inostvor.Data/             # SQLite. Ovisi: Core, Microsoft.Data.Sqlite, Dapper.
│   ├── Inostvor.Rendering/        # SkiaSharp scena. Ovisi: Kernel, Core, SkiaSharp.
│   ├── Inostvor.ViewModels/       # MVVM. Ovisi: Core, Sdk, CommunityToolkit.Mvvm. Bez WinUI!
│   └── Inostvor.App/              # WinUI 3 host + PluginLoader infrastruktura. Ovisi: sve.
│
├── tests/
│   ├── Inostvor.Kernel.Tests/     # ★NOVO
│   ├── Inostvor.Geometry.Tests/
│   ├── Inostvor.Cam.Tests/        # uključuje Validation testove
│   ├── Inostvor.Import.NetDxf.Tests/
│   ├── Inostvor.Post.Tests/
│   └── Inostvor.ViewModels.Tests/ # uključuje Undo/Redo testove
│
├── docs/adr/                       # ADR-001 (koordinate), ADR-002 (plugin izolacija)...
├── tools/
├── Directory.Build.props
├── .editorconfig
└── .gitignore
```

**Graf ovisnosti (smjer strelice = "ovisi o"):**

```
App ──► ViewModels ──► Sdk ──► Core ──► Kernel
 │                              ▲
 └──► { Import.NetDxf, Geometry, Cam, Post, Data, Rendering } ──┘
        (svi implementiraju Core/Sdk apstrakcije; App ih spaja kroz DI)
```

---

## 4. Novi/izmijenjeni moduli u detalje

### 4.1 Inostvor.Kernel (točka 2)

```
Inostvor.Kernel/
├── Primitives/
│   ├── Point2.cs, Vector2.cs          readonly struct, double
│   ├── LineSeg.cs, ArcSeg.cs          segmenti kao primitivi (lukovi first-class!)
│   ├── Polyline2.cs                   niz segmenata + iteracija, duljina, bounds
│   └── Aabb.cs                        axis-aligned bounding box, union/intersect/inflate
├── Transforms/
│   └── Matrix3x2d.cs                  2D afine transformacije (translate/rotate/scale/mirror), kompozicija
├── Intersections/
│   ├── LineLine.cs, LineArc.cs, ArcArc.cs
│   └── SelfIntersection.cs            sweep-line za polylines (koristi ga Validator)
├── Spatial/
│   ├── ISpatialIndex.cs
│   └── AabbTree.cs                    dinamični AABB tree (V1); KD-Tree/R-Tree dodati kad zatreba (nesting u V2)
├── Tolerance.cs                       JEDINO mjesto s epsilonima (ADR-001)
└── MathUtil.cs                        clamp, lerp, normalizacija kutova, signed area
```

**Napomena o prostornim strukturama:** za V1 je dovoljan AABB tree (kolizije leadova, pick/selekcija na canvasu). KD/R-Tree dolaze s nestingom u V2 — interfejs `ISpatialIndex` postoji od početka, pa je zamjena lokalna.

### 4.2 Zamjenjiv DXF parser (točka 1)

- `Core.Abstractions.IDxfImporter` ostaje jedina ovisnost ostatka sustava.
- `Inostvor.Import.NetDxf.NetDxfImporter` = prva implementacija. netDxf NuGet referenca postoji SAMO u tom projektu.
- Budući `AcadSharpImporter` = novi projekt `Inostvor.Import.AcadSharp`, registracija u DI (ili kroz `IImportPlugin`), nula izmjena drugdje.
- `ImportResult` je parser-neutralan: `Segment[]` + upozorenja + metapodaci (INSUNITS, layeri) — nijedan netDxf tip ne izlazi iz Import projekta.

### 4.3 ToolpathValidator (točka 3)

```
Inostvor.Cam/Validation/
├── ToolpathValidator.cs               orkestrator: izvršava sva registrirana pravila
├── ValidationReport.cs                nalazi s Severity (Error | Warning | Info) + referencom na entitet + pozicijom
└── Rules/
    ├── OpenContourRule.cs             otvorene konture (gap > tolerancija spajanja)
    ├── SelfIntersectionRule.cs        samopresijecanja (Kernel sweep-line)
    ├── MinRadiusRule.cs               radijus < min. izvediv za zadani kerf/stroj
    ├── MinHoleRule.cs                 rupa premala nakon kerf offseta (degenerira ili < prag, tipično < debljina lima)
    ├── LeadCollisionRule.cs           lead siječe vlastitu ili susjednu konturu
    ├── OvercutRule.cs                 overcut dulji od preostale geometrije / izlazi iz konture
    ├── DuplicateSegmentRule.cs        dupli/preklapajući segmenti (čest DXF artefakt)
    └── ZeroLengthSegmentRule.cs       degenerirani segmenti
```

- Pravila implementiraju `IValidationRule` (u Sdk-u → validacijska pravila su i plugin točka).
- **Policy:** `Error` blokira generiranje G-koda; `Warning` se ispisuje u Output Console i označava na canvasu (crveni/žuti marker na poziciji problema), ali ne blokira.
- Validator se izvršava u dvije faze: nakon importa (geometrijska pravila) i nakon generiranja putanja (lead/overcut pravila).

### 4.4 Undo/Redo — Command pattern (točka 4)

```
Inostvor.Core/
├── Abstractions/IUndoableCommand.cs   Execute() / Undo() / string Description
├── Abstractions/IUndoService.cs      Do(cmd), Undo(), Redo(), CanUndo/CanRedo, events
└── Services/UndoRedoService.cs        dva stacka, limit dubine, transakcije (CompositeCommand)
```

- **Pravilo od M0:** ViewModeli NIKAD ne mutiraju `CutJob` direktno — svaka izmjena (promjena kerf-a, redoslijeda, lead pozicije, brisanje konture) je `IUndoableCommand` kroz `IUndoService.Do()`.
- V1 implementira infrastrukturu + komande koje ionako nastaju u M5-M8 (izmjene ToolSettings, per-contour override, redoslijed). Puna pokrivenost svih operacija dolazi prirodno jer drugog puta za mutaciju nema.
- `CompositeCommand` za grupirane operacije (npr. "Generate toolpaths" = jedna undo jedinica).

### 4.5 Plugin API — Inostvor.Sdk (točka 5)

```
Inostvor.Sdk/
├── IPlugin.cs                         Id, Name, Version, Initialize(IPluginHost)
├── IPluginHost.cs                     ono što aplikacija nudi pluginu: logger, settings, registracije
├── Import/IImportPlugin.cs            ekstenzije datoteka + factory za IDxfImporter/IGeometryImporter
├── Post/IPostProcessorPlugin.cs       factory za IPostProcessor + opis dijalekta
├── Toolpath/IToolpathPlugin.cs        custom toolpath operacije (npr. marking, dodatne strategije)
└── Validation/IValidationRule.cs      (koristi ga i Cam interno — ugrađena pravila su "first-party plugini")
```

- **Ključni princip:** ugrađene implementacije (NetDxfImporter, Mach3Post, EC300Post, validacijska pravila) registriraju se kroz ISTE interfejse kao budući plugini. Time je Plugin API testiran od prvog dana vlastitim kodom — kad u V3 dođe `PluginLoader` (AssemblyLoadContext, izolacija, manifest), kontrakti su već dokazani.
- ADR-002 (piše se u V3): izolacija plugina, verzioniranje SDK-a, potpisivanje.

---

## 5. Ažurirani plan modula V1

| # | Modul | Dopune u odnosu na v1.0 |
|---|---|---|
| M0 | Skeleton | + Sdk i Kernel projekti, + UndoRedoService (infrastruktura + testovi), + prazan ToolpathValidator pipeline registriran u DI |
| M1 | Kernel | preimenovan iz "Geometry primitives": primitivi, transformacije, intersekcije, Aabb, AabbTree, Tolerance — 100% test coverage, ovo je temelj svega |
| M2 | DXF import | NetDxfImporter kroz IImportPlugin registraciju |
| M3 | Konture | + geometrijska validacijska pravila (OpenContour, SelfIntersection, Duplicate, ZeroLength) |
| M4 | Rendering | + prikaz validacijskih markera na canvasu |
| M5 | CAM jezgra | ClipperAdapter (ADR-001), kerf, arc-fit, leadovi, overcut — sve izmjene parametara kao Commands |
| M6 | Redoslijed + simulacija | + toolpath validacijska pravila (MinRadius, MinHole, LeadCollision, Overcut) |
| M7 | G-kod + postovi | Validator gate prije emita (Error blokira); postovi kroz IPostProcessorPlugin registraciju |
| M8 | Persistencija + polish | nepromijenjeno |

---

## 6. Sve ostalo iz v1.0 ostaje na snazi

Tehnološke odluke (§2 v1.0), NuGet paketi, klasne odgovornosti, MVVM struktura, postprocesorska hijerarhija, testna strategija — bez izmjena, uz gore navedene dopune. Odobrene odluke iz §9 v1.0: SkiaSharp ✔, Dapper ✔, Arc-fitting u V1 ✔, EC300Post : Mach3Post ✔, mm interno ✔.

**Otvoreno prije M0:** konačni naziv aplikacije (namespace lock) — radni naziv ostaje **Inostvor** ako ne kažeš drukčije.

---

## 7. Dopuna Baseline v1.1 (odobreno 2026-07-09)

**Arhitektura je ZAMRZNUTA kao Architecture Baseline v1.1.** Sve buduće izmjene
isključivo kroz ADR zapise u `docs/adr/`.

### 7.1 Inostvor.Benchmarks

Zaseban projekt (`benchmarks/`), BenchmarkDotNet, NIJE dio aplikacije. Mjeri:
DXF import, detekciju kontura, kerf offset, arc fitting, ToolpathGenerator,
ToolpathValidator, renderiranje velikih DXF datoteka, generiranje G-koda.
Svaki modul (M1+) dodaje svoje benchmarke zajedno s implementacijom.

### 7.2 tests/TestData/

Referentne DXF datoteke za automatske testove, kategorije:
Simple, Holes, Nested, OpenContours, Decorative, LargeFiles, Invalid, Regression.
**Politika:** svaki ispravljeni bug dobiva regression DXF + test (vidi tests/TestData/README.md).

## Dopuna plana razvoja (2026-07-11)

- **About dijalog** (budući modul, nakon M8): naziv aplikacije, verzija, godina,
  autor, autorska prava, kratki opis, pozdravna poruka korisnicima. Ime autora
  nije dio naziva programa (Inostvor).
- **Plugin DLL distribucija** postprocesora: vidi ADR-004.
- **Grafički editor postprocesora** (nakon V1): vizualno uređivanje dijalekta s
  live pregledom generiranog G-koda. Preduvjet ugrađen u dizajn M7: deklarativni
  opis dijalekta (predlošci kao podaci) umjesto isključivo koda.
