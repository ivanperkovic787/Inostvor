# M5 — CAM (izvještaj modula)

## Cilj

Klasificirane konture → **potpuno neutralni interni model putanje** (ToolpathProgram)
kroz modularne, zamjenjive operacije. Nijedan bajt G-koda, nijedna pretpostavka o
kontroleru — G-kod nastaje isključivo u postprocesoru (M7, ADR-004).

## Zahtjevi ↔ implementacija

1. **Neutralni IR:** `CutMove` (LineSeg/ArcSeg + MoveKind + FeedRate), `CutSequence`
   (jedan pierce), `RapidMove` (eksplicitno), `ToolpathProgram` + `ToolpathStatistics`.
   Test `IR_NeSadrziNistaKontrolerSpecifino` + strukturno: IR tipovi u Core.Model.Toolpath
   nemaju referencu ni na jedan postprocesorski pojam.
2. **Modularne operacije:** `IKerfOffsetService`, `IArcFitter`, `ILeadStrategy` (Sdk),
   `IOvercutService`, `ICutOrderStrategy` — svaka DI-zamjenjiva; ToolpathGenerator ovisi
   samo o sučeljima (nesiguran cast na konkretni tip uklonjen tijekom razvoja).
3. **Leadovi:** `ILeadStrategy` u Sdk-u (otvoren skup kao IValidationRule); dispečer
   `LeadGeneratorService` bira po `LeadStyle` enumu (None/Line/Arc/Tangential/Loop/
   Corner/PierceOnScrap). V1 implementira **Line** (45° na stranu otpada) i **Arc**
   (četvrt-kružni, TANGENTAN — testirana tangentnost). Neregistriran stil → bez leada,
   nikad pogrešan lead. Nove strategije = registracija, nula izmjena jezgre.
4. **Arc fitting opcionalan i konzervativan:** `EnableArcFitting` prekidač; luk se
   emitira SAMO ako sve obuhvaćene točke leže unutar tolerancije I kutovi su strogo
   monotoni; inače linije. Testirano na 20 slučajnih lukova (seed 42) + cik-cak koji
   NIKAD ne smije postati luk. Krajevi luka egzaktni (kružnica kroz krajnje točke).
5. **Kerf determinizam:** Clipper2 u µm (ADR-001), izoliran u JEDNU datoteku
   (ClipperAdapter, dokumentirana API površina). Redoslijed prstenova i početna
   točka NISU ugovorna obveza Clippera → stabilna normalizacija: rotacija prstena
   na leksikografski minimum + sortiranje prstenova. Test: 5 pokretanja
   bajt-identično (egzaktna jednakost doubleova).
6. **Simulacija bez parsiranja G-koda:** IR nosi Length/FeedRate/Duration po potezu,
   eksplicitne rapids, pierce brojanje; `ToolpathStatistics` (rez/rapid/pierce
   vremena) izračunata jednom. Aktivni segment u simulaciji (M6) = Geometry.PointAt(t).
7. **Procesna neutralnost:** `CutProcess` enum (Plasma/Laser/OxyFuel/WaterJet) živi u
   `TechnologySettings`; jezgra NIGDJE ne grana po procesu — parametri (kerf, feed,
   pierce) su univerzalni, procesno/strojno specifično (THC, visine, plinovi) pripada
   postprocesoru i budućim tehnološkim profilima.

## Odluke koje tražim da posebno potvrdiš

- **Otvorene konture: središnjica bez kerfa i bez leadova (V1).** Kerf kompenzacija
  otvorene putanje je dvosmislena (koja strana?) — ispravno rješenje je korisnički
  izbor strane po putanji, planirano uz UI u M6+.
- **Redoslijed rezanja V1:** rupe prije vanjske konture istog dijela, dijelovi po
  (MinY, MinX). Optimizacija puta (najbliži sljedeći, toplinsko raspoređivanje) je
  M6 strategija kroz isti `ICutOrderStrategy`.
- **Pierce točka V1 = početak offsetirane putanje** (deterministički nakon
  normalizacije: leksikografski minimum). Pametniji izbor (najduži segment, kut,
  scrap analiza) — buduća strategija.
- **Overcut ponavlja početak reznih poteza** (preskače lead-in) — standardno
  ponašanje za uklanjanje spojne bradavice.

## Testovi i benchmarki

- KerfOffsetServiceTests: 7 · ArcFitterTests: 7 · LeadStrategyTests: 6 ·
  OvercutAndCutOrderTests: 6 · ToolpathGeneratorTests: 9 (ukupno **35**)
- ToolpathBenchmarks: cijeli cjevovod na 50/200 dijelova s rupama

## Rizici za prvi Windows build

Clipper2 1.5.4 API površina (InflatePaths/Area potpisi) — svako odstupanje lomi se
lokalno u ClipperAdapter.cs. Ostatak M5 nema vanjskih ovisnosti.

## Definition of Done

- [x] Neutralni IR; sve operacije kroz sučelja; leadovi kao otvoren skup strategija
- [x] Arc fitting konzervativan s garancijom točnosti; kerf bajt-deterministički
- [x] Statistika i podaci za simulaciju u IR-u; procesna neutralnost
- [x] 35 testova + benchmark; statičke provjere zelene
- [ ] Build + testovi na Windows stroju (Clipper2 API autoritativna provjera)
