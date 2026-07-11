# ADR-003 — Preimenovanje projekta: PlasmaCAM → Inostvor

**Status:** prihvaćeno (odluka vlasnika projekta)
**Datum:** 2026-07-11

## Odluka

Projekt se službeno preimenuje iz **PlasmaCAM** u **Inostvor**. Ovo je posljednje
veliko preimenovanje prije izlaska V1.

## Opseg (sve provedeno u jednom commitu)

Solution (Inostvor.sln), svih 19 projekata (Inostvor.*), Root Namespace,
Assembly Name (izvršna: Inostvor.exe), naziv SQLite baze (M8: inostvor.db),
log datoteke (inostvor-YYYYMMDD.log), LocalAppData mapa (%LocalAppData%\Inostvor),
plugin identifikatori (inostvor.import.netdxf), build skripte (tools/build.ps1),
dokumentacija, ADR dokumenti, TestData lokator, benchmarki — sve interne reference.

## Posljedice

- Funkcionalnost nepromijenjena; git povijest očuvana (git mv).
- Vanjski dokument "PlasmaCAM Arhitektura v1.1" (zamrznuti baseline) NE prepisuje
  se retroaktivno — ovaj ADR je službeni zapis promjene imena; baseline sadržajno
  vrijedi dalje pod novim imenom.
- Ime autora nije dio naziva programa. About dijalog (naziv, verzija, godina,
  autor, prava, opis, pozdravna poruka) planiran je kao budući modul (vidi
  docs/ARCHITECTURE.md, plan razvoja).
