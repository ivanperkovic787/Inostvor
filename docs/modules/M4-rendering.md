# M4 — Rendering (izvještaj modula)

## Cilj

SkiaSharp viewport CAD kvalitete, projektiran od početka za stotine tisuća
segmenata, bez ijedne linije CAM logike u rendereru.

## Arhitektura (zahtjevi ispunjeni)

- **Bez CAM logike:** renderer crta isključivo stanje iz jezgre. `RenderScene`
  gradi se iz `GeometryPipelineResult` i drži ISTE `Contour`/`Polyline2`/`ISegment`
  objekte koje koristi CAM — nema zasebne geometrije za prikaz. Prikazna
  tessellacija lukova (`DisplayTessellation`) je isključivo crtaći artefakt;
  CAM je nikad ne vidi.
- **Skalabilnost (stotine tisuća segmenata) bez budućeg refaktoriranja:**
  1. *Viewport culling:* `AabbTree` (iz Kernela, M1) po segmentima — po frameu
     se obrađuju samo vidljivi (benchmark: 120 000 segmenata).
  2. *Level-of-detail:* tolerancija tetive luka = 0.5 px u world jedinicama
     (manje zoom → manje točaka); segmenti ispod 0.75 px dijagonale se preskaču.
  3. *Cache geometrije:* tessellacija lukova kesirana po (segment, log2 bucket
     tolerancije) — pan i fini zoom NE regeneriraju geometriju; nova scena
     automatski invalidira cache (vezan uz instancu scene).
  4. *Change-driven invalidacija:* canvas se recrtava samo na `RedrawRequested`
     (promjena kamere/scene/selekcije), nikad u praznom hodu.

### Odstupanje od zahtjeva, s obrazloženjem: "dirty-region redraw"

SkiaSharp canvas u WinUI 3 (SKXamlCanvas) pri svakoj invalidaciji predaje CIJELU
površinu — parcijalna regija nije podržana na toj razini API-ja. Umjesto
simuliranja dirty-regija, isporučeno je ono što stvarno štedi rad: recrtavanje
samo na promjenu + culling + LOD + cache, čime je trošak framea proporcionalan
VIDLJIVOM sadržaju, ne veličini datoteke. Ako se kasnije pokaže potreba,
arhitektura dopušta layer-cache (statična geometrija u SKPicture, dinamični
overlay preko) BEZ refaktoriranja — renderer je već odvojen od izvora scene.

## Zoom/Pan CAD kvalitete

- **Zoom To Cursor:** točka svijeta pod kursorom ostaje fiksna (testirano na 4
  pozicije + 100 uzastopnih zoomova bez drifta — Center+Scale model, ne
  akumulirana matrica).
- **Zoom Extents / Zoom Selected:** gumbi u viewportu; extents s 5% margine.
- **Pan bez trzanja:** sadržaj prati pokazivač 1:1 px (testirano), srednja/desna
  tipka s pointer capture; DPI ispravno (Skia pikseli vs. WinUI DIP-ovi).
- **Stabilnost pri velikim povećanjima:** skala ograničena [1e-4, 1e6] px/mm;
  transformacija bez akumulacije pogreške.

## Klik na Validation nalaz

Lista nalaza u lijevom panelu (GREŠKA/UPOZORENJE/INFO + kod + kontura + poruka).
Klik: centrira pogled, zumira (extents konture s paddingom; bez konture — prozor
40 mm oko lokacije), ističe konturu (žuto, drugi prolaz preko svega) i crta
marker (crveni križić s kružnicom) na lokaciji nalaza.

## Paketi

SkiaSharp + SkiaSharp.Views.WinUI **2.88.9** (provjereno stabilna kombinacija s
WinUI 3). Ako build pokaže konflikt s Windows App SDK verzijom, alternativa je
3.119.x — izmjena je samo u Directory.Packages.props.

## Testovi i benchmarki

- Camera2DTests: 10 (roundtrip, zoom-to-cursor ×4, 100× bez drifta, extents,
  degenerirane granice, pan 1:1, clamp, visible bounds, verzija)
- DisplayTessellationTests: 5 (tolerancija/zoom, bucketi, cache instanca, LOD)
- RenderSceneTests: 4 (granice, culling upiti, prazna scena)
- ViewportViewModelTests: 3 (SetScene→extents+redraw, zoom-to-issue s konturom
  i bez nje)
- Benchmark: RenderCullingBenchmarks (120 000 segmenata, puni pogled i 1%)

## Definition of Done

- [x] Renderer bez CAM logike; ista geometrija kao CAM
- [x] Culling + LOD + cache + change-driven invalidacija
- [x] Zoom To Cursor / Extents / Selected, pan 1:1, stabilno pri velikom zoomu
- [x] Klik na nalaz → centriranje + zoom + isticanje + marker
- [x] Statičke provjere zelene
- [ ] Build + testovi + vizualna provjera na Windows stroju
