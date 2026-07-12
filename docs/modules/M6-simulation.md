# M6 — Redoslijed rezanja i simulacija (izvještaj modula)

## Zahtjevi ↔ implementacija

1. **CutOrder kao otvoren skup strategija.** `ICutOrderStrategy` dobio `Id`;
   `ICutOrderStrategyProvider` rezolvira po `TechnologySettings.CutOrderStrategyId`.
   Registrirane: `bottom-to-top` (dosadašnji default) i `nearest-neighbor`
   (pohlepni najbliži dio, deterministički tie-breakovi). Left-to-Right,
   Grid, Minimal-Rapids (TSP), Custom-User-Order — dodaju se registracijom u DI,
   **nula izmjena ToolpathGeneratora** (dokazano testom: ista instanca generatora,
   različit Id u tehnologiji → različit redoslijed). Nepoznat Id → konzervativni
   fallback, nikad iznimka. Nepromjenjivo pravilo svih strategija: rupe prije
   vanjske konture dijela.

2. **Simulacija neovisna o rendereru.** `SimulationEngine` i `SimulationState`
   žive u Inostvor.Cam — nula referenci na SkiaSharp/WinUI (statički provjereno).
   Izlaz je čisti model (pozicija, faza, torch, aktivni potez, vremena); renderer
   ga isključivo crta (torch marker + overlay putanje iz IR-a: rapids crtkano,
   leadovi zeleno, rez crveno).

3. **Razdvojena vremena.** `TimeBreakdown` (Cut/Rapid/Pierce/Total) dostupan u
   SVAKOM trenutku simulacije (`SimulationState.Elapsed`), ne samo na kraju.
   Križna provjera testom: finalni breakdown == `ToolpathStatistics` iz M5
   (dva neovisna izračuna istog IR-a).

4. **Pauza/nastavak.** Ključna dizajnerska odluka: simulacija je ČISTA FUNKCIJA
   vremena — `StateAt(t)` nad jednom izgrađenom vremenskom crtom (binarno
   traženje). Stanje sesije je zato JEDAN broj: `SimulationCheckpoint(Time,
   Speed)` sprema se i vraća bez ponovnog izračuna ToolpathPrograma (testirano:
   restore daje identičan record stanja).

5. **Digitalni blizanac.** UI ovisi o sučelju `IMachineStateSource { Current,
   StateChanged }` — `SimulationSession` (planirana reprodukcija) je PRVA
   implementacija; budući Live Monitor hranjen telemetrijom stroja (pozicija,
   stanje luka, M-kodovi) je DRUGA implementacija istog sučelja, bez ijedne
   promjene renderera ili ViewModela. `SimulationState` polja su namjerno
   oblikovana da opisuju i stvarni stroj (Phase, TorchOn, Position).

6. **Procesna neutralnost.** Simulacija čita isključivo IR (Length/FeedRate/
   Duration/PointAt) — ista vremenska crta vrijedi za plazmu, laser, oxy-fuel i
   vodeni mlaz; procesne razlike ulaze kroz tehnologiju, ne kroz engine.

## UI (V1 transport)

Traka na dnu viewporta: Play/Pause, Stop, slider (seek), brzina (0.5×–10×),
status (vrijeme, faza, razdvojena vremena). DispatcherTimer tick 33 ms aktivan
ISKLJUČIVO dok reprodukcija igra — poštuje change-driven invalidaciju iz M4.

## Zabilježeno za M7 (tvoja ideja)

**Grafički editor postprocesora** upisan u plan razvoja (docs/ARCHITECTURE.md).
Posljedica za dizajn M7: postprocesori će se, gdje god je moguće, opisivati
DEKLARATIVNO (predlošci dijalekta kao podaci: formati riječi, zaglavlja,
sekvence pierce/kraja) umjesto čistim kodom — upravo to kasnije omogućuje
grafičko uređivanje s live pregledom G-koda, a plugin kod ostaje za slučajeve
koje predložak ne pokriva.

## Testovi

SimulationEngineTests: 9 · CutOrderStrategyTests: 4 (ukupno **13** novih).

## Definition of Done

- [x] CutOrder strategije po Id-u, bez izmjene generatora; NN + bottom-to-top
- [x] SimulationEngine čist (bez UI ovisnosti), razdvojena vremena u svakom t
- [x] Checkpoint spremanje/nastavak bez ponovnog izračuna
- [x] IMachineStateSource — put prema Live Monitoru/Digital Twinu
- [x] Toolpath overlay + torch u vieweru; transport traka
- [ ] Build + testovi + vizualna provjera na Windows stroju
