# TestData — referentne DXF datoteke

Datoteke u ovoj mapi su ulazi za automatske testove (importer, detekcija kontura,
CAM pipeline, postprocesori) i za benchmarke. Testovi ih učitavaju relativno
(`tests/TestData/...`) — NE kopirati u output, referencirati kao Content.

## Kategorije

| Mapa | Sadržaj | Prvi konzument |
|---|---|---|
| `Simple/` | Osnovni oblici: pravokutnik, krug, L-profil — po jedna vanjska kontura | M2 (import) |
| `Holes/` | Ploče s rupama: prirubnice, montažne ploče — vanjska + unutarnje konture | M3 (klasifikacija) |
| `Nested/` | Dio u dijelu (višestruka ugniježđenost) — test stabla hijerarhije | M3 |
| `OpenContours/` | Namjerno otvorene/prekinute konture s različitim gapovima | M3 (validator) |
| `Decorative/` | Splineovi, elipse, tekst pretvoren u krivulje — test tessellacije | M2/M5 (arc-fit) |
| `LargeFiles/` | 1000+ entiteta — benchmarki i performance regresije | Benchmarks |
| `Invalid/` | Korumpirani/nestandardni DXF-ovi — importer mora vratiti grešku, ne exception | M2 |
| `Regression/` | **Jedan DXF po ispravljenom bugu.** | trajno |

## Pravila

1. **Svaki ispravljeni bug MORA dobiti DXF u `Regression/`** + test koji reproducira
   originalni problem. Isti bug se ne smije nikada vratiti.
2. Imenovanje: `kratki-opis.dxf`; u `Regression/`: `BUG-NNN-kratki-opis.dxf`
   (NNN = broj issuea), uz komentar u pripadnom testu što je bug bio.
3. Datoteke se dodaju u modulu koji ih prvi konzumira (M2+), generirane poznatim
   alatom (AutoCAD/QCAD/LibreCAD) ili programski — nikad ručno editirane bez verifikacije.
4. DXF-ovi su binarno nepromjenjivi nakon dodavanja — promjena ulaza mijenja smisao testa.

## Provenijencija datoteka (M2)

Sve `.dxf` datoteke u ovom trenutku su **sintetske**, generirane bibliotekom
**ezdxf 1.4.4** (spec-compliant DXF writer) i validirane round-trip učitavanjem.
Pokrivaju DXF verzije R2000–R2018 (+ namjerni R12 u `Invalid/`), sve podržane
entitete, INSERT transformacije (skala/rotacija/zrcaljenje/neuniformna skala,
3 razine ugnježđenja), layer nasljeđivanje, jedinice i degenerirane ulaze.

**Native exporti iz komercijalnih CAD alata (AutoCAD, SolidWorks, Fusion 360,
LibreCAD, QCAD…) dodaju se u `ByVendor/<alat>/` kad budu dostupni** — sintetske
datoteke ne mogu reproducirati vendor-specifične quirkove (redoslijed sekcija,
proxy entiteti, nestandardna zaglavlja). Svaka nova vendor datoteka dobiva
regresijski test u `Inostvor.Import.NetDxf.Tests`.
