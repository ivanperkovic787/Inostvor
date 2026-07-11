# ADR-004 — Postprocesori su isključivo pluginovi

**Status:** prihvaćeno (odluka vlasnika projekta)
**Datum:** 2026-07-11

## Odluka

Mach3 i EC300 NISU posebni slučajevi. Svi postprocesori — ugrađeni i budući —
implementiraju isti javni kontrakt `Inostvor.Sdk.IPostProcessorPlugin` (M7) i
registriraju se istim mehanizmom. CAM jezgra ne zna ni za jedan konkretan
kontroler.

## Ciljana lista kontrolera (dodavanje BEZ izmjene jezgre)

Mach3, Mach4, LinuxCNC, GRBL, UC300ETH, ESS, Fanuc, Haas, PlanetCNC,
FireControl i bilo koji drugi.

## Pravila

1. Jezgra (Core/Cam) izlaže neutralan model putanje (toolpath IR); postprocesor
   ga prevodi u dijalekt G-koda. Smjer ovisnosti: plugin → Sdk → Core, nikad obrnuto.
2. Ugrađeni postprocesori (Mach3, EC300 kao specijalizacija Mach3 dijalekta)
   registriraju se kroz isti kontrakt kao vanjski — bez privilegiranih puteva.
3. Distribucija kao zasebni DLL: PluginHost dobiva discovery/učitavanje iz
   plugin mape (planirano nakon V1 jezgre; kontrakt se projektira za to od M7).
4. Stabilnost API-ja: Inostvor.Sdk verzionira se SemVer-om; breaking change
   kontrakta samo uz major verziju — vanjski DLL pluginovi to zahtijevaju.

## Posljedice

- M7 implementira Mach3Post i EC300Post (EC300 nasljeđuje Mach3 dijalekt s
  vlastitim probe/pierce sekvencama) isključivo kroz plugin API.
- Testovi postprocesora pišu se protiv kontrakta, ne implementacije.
