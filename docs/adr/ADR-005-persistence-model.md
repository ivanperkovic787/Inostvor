# ADR-005 — Model persistencije: projekt je datoteka, SQLite je aplikacijska razina

**Status:** prihvaćeno · **Datum:** 2026-07-12

## Odluka

Tri jasno odvojena spremišta:

| Sloj | Gdje | Sadržaj |
|---|---|---|
| **Projekt** | `.ino` datoteka (ZIP) | korisnikov rad: DXF izvori, tehnologija, profil stroja, postprocesor, simulacijski checkpoint, sekcije budućih modula |
| **Aplikacijske biblioteke i postavke** | SQLite (`%LocalAppData%\Inostvor\inostvor.db`) | profili strojeva, biblioteka tehnologija, key-value postavke, nedavni projekti |
| **Autosave** | `%LocalAppData%\Inostvor\autosave.ino` | obični .ino projekt + sentinel `session.lock` |

**SQLite NIKAD nije jedino mjesto gdje postoji korisnikov projekt.** Baza se smije
obrisati bez gubitka ijednog projekta (izgube se samo biblioteke i postavke, koje
su izvozive/uvozive).

## .ino format

ZIP kontejner:
```
manifest.json   {"formatVersion": 1}
project.json    ProjectDocument (JSON, enumi kao stringovi, uvlačeno)
dxf/<ime>.dxf   ORIGINALNI bajtovi uvezenih DXF-ova
```

**Zašto ZIP+JSON, a ne SQLite ili binarni format:** zahtjev je bio otvaranje
"nakon nekoliko godina bez gubitka podataka". ZIP i JSON čita bilo koji alat i za
20 godina; DXF-ovi ostaju izvorni i izvadivi običnim arhiverom čak i ako Inostvor
ne postoji. Binarni/SQLite projekt tu garanciju ne daje.

**Derivirani podaci se NE spremaju** (konture, validacija, putanja, G-kod).
Regeneriraju se iz izvora — što je moguće upravo zato što su M3–M7 bajt-deterministički
(kerf normalizacija, deterministički redoslijed, golden testovi). Posljedica:
projekt je malen i ne može "istrunuti" u nekonzistentno stanje.

**Atomarno spremanje:** piše se u `.tmp` pa `File.Move(overwrite)` — pad usred
spremanja ne uništava prethodnu verziju projekta.

## Verzioniranje i migracije

`manifest.formatVersion` + lanac `IProjectMigration` (v→v+1). Migracije rade **nad
JSON-om, ne nad C# tipovima** — stari format ne treba živuće tipove da bi se
otvorio, pa se stare verzije mogu čitati zauvijek bez zadržavanja mrtvog koda.
Kompatibilnost se **nikad ne razbija**; lanac samo raste. Noviji format u starijem
programu → jasna poruka, ne pad.

## Forward compatibility (buduci moduli)

`ProjectDocument.Extensions` (mapa naziv → JSON). Nesting, tabovi, višestruki
limovi, ostaci lima, baza materijala, optimizacija proizvodnje pišu vlastite
sekcije. **Verzija koja sekciju ne razumije čuva je netaknutu u round-tripu**
(testirano) — stariji Inostvor ne uništava podatke novijeg.

## Prenosivost

`ISettingsPortService`: profili strojeva + biblioteka tehnologija → jedna JSON
datoteka, uvoz na drugom računalu (upsert po imenu/Id-u). Postprocesorski dijalekti
su već serializabilni recordi (M7) — isti kanal ih prima kad stigne editor.
