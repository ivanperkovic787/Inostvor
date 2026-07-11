# M3 — Konture i validacija (izvještaj modula)

## Cilj

Uvezene segmente pretvoriti u konture (lance), klasificirati ih (Outer/Hole s
normaliziranom orijentacijom) i validirati — s dva eksplicitna zahtjeva:
robusnost na mala geometrijska odstupanja UZ potpunu evidenciju spajanja, i
potpuno deterministički ValidationReport.

## Zahtjev 1: robusnost + transparentnost spajanja

- `ContourBuilder` premošćuje razmake krajnjih točaka ≤ tolerancije spajanja
  (default 0.05 mm, `ContourBuildSettings.JoinTolerance`).
- SVAKI premošteni razmak > geometrijske tolerancije bilježi se kao
  `ContourJoin` (kontura, lokacija = sredina razmaka, veličina, je li zatvorio
  konturu). `Contour.ClosedByTolerance` označava konture zatvorene tolerancijom.
- `JoinedGapsRule` (Info, AUTO_JOINED_GAP) prijavljuje svaki spoj korisniku;
  `OpenContourRule` (Warning, OPEN_CONTOUR) prijavljuje što je OSTALO otvoreno,
  s razmakom krajeva i uputom kada je razmak blizu tolerancije (≤ 10×).
- Geometrija se pri spajanju NE mijenja — healing razmaka je posao kerf faze
  (M5, Clipper u µm). Odluka dokumentirana u ContourBuilderu.

## Zahtjev 2: deterministički ValidationReport

Dvije razine obrane:
1. Algoritmi su deterministički: segmenti se obilaze ulaznim redoslijedom,
   kandidat za nastavak lanca bira se strogim uređajem (razmak → indeks → kraj),
   nijedan rezultat ne ovisi o enumeraciji hash struktura; Id-jevi kontura su
   stabilni; layeri se obrađuju redoslijedom prve pojave.
2. `ValidationReport` u konstruktoru STABILNO SORTIRA sve nalaze po
   (Severity, Code, ContourId, X, Y, Message) — redoslijed registracije pravila
   nema utjecaja. Testovi: 5 uzastopnih pokretanja → identičan fingerprint;
   obrnut redoslijed pravila → identičan izvještaj.

## Algoritmi (točnost > performanse)

- **Lančanje:** pohlepno, s `EndpointIndex` (uniform grid, ćelija = tolerancija,
  3×3 upit). Širenje naprijed pa natrag od seeda (seed ne mora biti prvi segment
  konture). Jednosegmentni lanac zatvara se samo za luk (puni krug) — linija se
  nikad ne zatvara na sebe. Ograničenje: kod grananja (3+ kraja u točki) izbor je
  pohlepan (najmanji razmak pa indeks) — deterministički, dokumentirano; globalna
  optimizacija grafa nije potrebna za CAD ulaze u V1.
- **Površina: EGZAKTNA**, ne tessellacijska — shoelace po vrhovima + kružni
  odsječci ½r²(θ − sin θ). Puni krug daje točno πr² (test), slot točno
  1200 + 100π (test).
- **Klasifikacija:** ugnježđivanje po dubini (parna = Outer, neparna = Hole),
  roditelj = najmanja kontura koja sadrži reprezentativnu točku. Sadržavanje
  koristi tesselliranu aproksimaciju lukova (0.01 mm) — dokumentirana
  aproksimacija, dovoljna za ODNOS ugnježđivanja (površina je egzaktna).
- **Orijentacija normalizirana:** Outer → CCW, Hole → CW (konvencija za kerf
  offset u M5); `Contour.Reversed()` po potrebi.

## Pravila validacije (svih 5 kroz Sdk `IValidationRule` — isti kontrakt za buduće pluginove)

| Kod | Severity | Opis |
|---|---|---|
| SELF_INTERSECTION | Error | kontura siječe samu sebe (kerf bi bio nepredvidiv) |
| OPEN_CONTOUR | Warning | otvorena kontura + razmak krajeva + uputa |
| DUPLICATE_GEOMETRY | Warning | isti segment dvaput (i obrnut) — kvantizirani ključ + egzaktna potvrda |
| ZERO_LENGTH_SEGMENT | Warning | segment < 0.01 mm (obrana u dubinu) |
| AUTO_JOINED_GAP | Info | svaki automatski premošten razmak |

## Arhitektonska napomena

`ValidationContext` je u Core (samo Core tipovi); `IValidationRule` u Sdk;
implementacija `ToolpathValidator` u Geometry — jer Sdk → Core smjer referenci
ne dopušta Core → Sdk (kružna ovisnost uhvaćena i razriješena tijekom razvoja).
`IGeometryPipeline` (build → classify → validate) jedini je servis koji
ViewModel vidi.

## Testovi

- ContourBuilderTests: 12 (egzaktni/izmiješani/obrnuti spojevi, razmak 0.02
  spojen+evidentiran, 0.5 otvoren, zatvaranje tolerancijom, puni krug, linija se
  ne zatvara, layeri odvojeni, širenje unatrag, slot, determinizam 5×)
- ContourClassifierTests: 10 (CCW/CW normalizacija, rupa CW, 3 razine
  ugnježđivanja, egzaktne površine, više dijelova, determinizam)
- ValidationRulesTests: 13 (svako pravilo pozitivno + negativno, agregacija)
- ValidationDeterminismTests: 4 (fingerprint 5×, obrnuta pravila, sortiranje)
- MainViewModelTests: ažurirano (pipeline se poziva nakon uspješnog importa,
  NE poziva se nakon neuspješnog)
- Benchmark: ContourPipelineBenchmarks (100/500 dijelova, deterministički shuffle)

## Definition of Done

- [x] Spajanje s tolerancijom + potpuna evidencija (ContourJoin + 2 pravila)
- [x] Deterministički report (sort + deterministički algoritmi + testovi)
- [x] Egzaktna površina s lukovima; normalizirana orijentacija
- [x] 5 pravila kroz Sdk kontrakt; DI registracija; UI sažetak u konzoli
- [x] Statičke provjere zelene; bez TODO markera
- [ ] Build + testovi na Windows stroju (autoritativna verifikacija)
