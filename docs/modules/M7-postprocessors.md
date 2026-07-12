# M7 — G-kod i postprocesori (izvještaj modula)

## Zahtjevi ↔ implementacija

1. **Potpuni plugin sustav (ADR-004).** `IPostProcessorPlugin` u Sdk-u; Mach3 i
   EC300 su OBIČNI pluginovi registrirani u DI kao i svi budući (Mach4, LinuxCNC,
   GRBL, EdingCNC, Fanuc, Haas, Siemens, ESS, Masso, PlanetCNC…). Katalog
   (`IPostProcessorCatalog`) rezolvira po id-u. **UC300ETH namjerno NIJE
   postprocesor** — to je Machine Profile koji govori čisti Mach3 dijalekt
   (testom fiksirano).
2. **Post ne odlučuje CAM logiku.** `IPostProcessor.Generate(ToolpathProgram)` —
   program ulazi 100% gotov (immutable record); emitter ga prolazi TOČNO danim
   redoslijedom. Kontrakt granice dokumentiran u XML docu sučelja; test
   `Postprocesor_NeMijenjaProgram` fiksira redoslijed emisije == redoslijed programa.
3. **Deklarativni dijalekt.** `GCodeDialect` (Sdk) je ČISTI record podataka:
   decimale, G20/G21, G90, M03/M05 (zamjenjivi kodovi — test s M62/M63),
   komentari (zagrade ili ';'), linijski brojevi (prefiks/start/korak), modalnost,
   F-samo-na-promjenu, zaglavlje/završetak s placeholderima ({POST}, {MACHINE},
   {SEQUENCES}, {CUTLENGTH}, {TOTALTIME}, {UNITS}, {DATE}), ekstenzija. Sedam
   testova mijenja izlaz isključivo podacima. Kontrolerske SEKVENCE su virtualni
   hookovi u kodu (`EmitPierceSequence`, `EmitCutEnd`…) — EC300 nadjačava pierce
   (dodatni G04 P0.3 stabilizacije, ProbeMacro iz profila jer firmware nema
   pouzdan G31).
4. **Golden testovi.** Ručno konstruiran deterministički program (bez CAM
   cjevovoda — golden ovisi isključivo o emitteru); `mach3_basic.tap` i
   `ec300_basic.tap` uspoređuju se BAJT-IDENTIČNO; + test 5 uzastopnih
   generiranja. VAŽNO (iskreno): golden datoteke su ručno izračunate prema
   pravilima emittera jer ovdje nema kompilacije — na prvom Windows buildu
   verificirati i po potrebi jednom potvrditi pregledani izlaz; od tada svaki
   promijenjeni znak ruši test. Ugrađeni dijalekti namjerno NE koriste {DATE}
   (determinizam izlaza).
5. **Editor postprocesora.** Preduvjet ugrađen: dijalekt je serializabilan record
   bez ponašanja — budući grafički editor uređuje PODATKE i zove isti
   `Create(dialect, profile)` API; nikakva promjena API-ja neće trebati.
6. **Dijalekt ≠ stroj.** `MachineProfile` (Core.Model.Machines): ime, id
   postprocesora, proces, tehnologija, stol, Z visine (SafeZ/Pierce/Cut),
   ProbeMacro. Ugrađena 4 profila: Mach3 Plasma, **Mach3 Router** (isti plugin,
   drugi stroj — testom dokazano da daju različit G-kod), EC300 Plasma (900×1200),
   UC300ETH Plasma (Mach3 dijalekt). M8 donosi persistenciju i UI uređivanje.
7. **Procesna neutralnost.** `CutProcess` proširen (Router, Mill). Emitter ne
   grana po procesu — TorchOn/Off kodovi i Z visine dolaze iz dijalekta/profila
   (plazma M03=luk, router M03=vreteno, laser M62/M63 kroz dijalekt — testirano).

## UI (V1)

Gumb "G-kod" u toolbaru: aktivni stroj (V1: EC300 Plasma profil) → plugin iz
kataloga → FileSavePicker → .tap. Izbor stroja/profila dolazi s M8 persistencijom.

## Testovi

GoldenFileTests: 3 · DialectTests: 7 · PostArchitectureTests: 5 (ukupno **15**).

## Definition of Done

- [x] Svi postovi kao ravnopravni pluginovi; UC300ETH kao profil, ne post
- [x] Deklarativni dijalekt (serializabilan — spreman za editor); sekvence kao hookovi
- [x] Golden testovi bajt-identični + determinizam
- [x] MachineProfile odvojen od dijalekta; 1 post → N strojeva dokazano testom
- [x] Statičke provjere zelene
- [ ] Build + verifikacija golden datoteka na Windows stroju
