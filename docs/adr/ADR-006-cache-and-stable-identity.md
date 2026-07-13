# ADR-006 — Opcionalni cache izvedenih podataka i stabilni identitet

**Status:** prihvaćeno · **Datum:** 2026-07-13

## 1. Cache izvedenih podataka

**Odluka:** izvedeni podaci (ToolpathProgram, kasnije preview i drugi artefakti)
**nisu izvor istine**, ali smiju postojati kao **opcionalni cache uz provjeru hasha**.
Ako je cache valjan — koristi se. Ako nije — tiho se odbacuje i regenerira.

### Mehanizam

`cache/toolpath.json` u .ino kontejneru sadrži:
- `InputHash` — SHA-256 nad: SHA-256 svakog DXF izvora (redoslijedom) + kanonska
  serijalizacija `TechnologySettings` (kerf, feed, leadovi, overcut, arc fitting,
  tolerancije, strategija redoslijeda).
- `PipelineVersion` — verzija CAM cjevovoda (`CacheKey.PipelineVersion`).
- `Program` — sam ToolpathProgram.

Pri otvaranju cache se prihvaća **samo ako se podudaraju i hash i verzija cjevovoda**.
Svaka nepodudarnost, oštećen JSON ili nedostajuća datoteka → `Cache = null`,
regeneracija. **Cache nikad ne smije uzrokovati grešku otvaranja projekta.**

### Posljedice

- **Promjena algoritma** (kerf, fitting, redoslijed, IR) → povećati `PipelineVersion`.
  Svi postojeći cachevi se automatski odbacuju; **migracija projekata nije potrebna**,
  jer cache nije podatak nego izvedenica.
- Brisanje `cache/` iz .ino datoteke ne mijenja projekt, samo usporava otvaranje.
- Geometrija (konture, validacija) uvijek se regenerira — potrebna je za prikaz i
  selekciju, a jeftina je; cachira se skupi dio (kerf offset + fitting + leadovi).
- `ISegment` je polimorfan, pa cache koristi diskriminator `$kind` (line/arc).

## 2. Stabilni identitet (UUID od V1)

**Odluka:** svi trajni objekti implementiraju `IIdentifiable` sa stabilnim `Guid Id`
dodijeljenim pri stvaranju i **nikad promijenjenim**.

| Objekt | Id |
|---|---|
| `ProjectDocument` | preživljava preimenovanje datoteke |
| `ProjectDxfSource` | referencira izvor neovisno o imenu |
| `MachineProfile` | primarni ključ u SQLiteu (bio: ime) |
| `TechnologyEntry` | već je imao Id, sada formalno kroz sučelje |

**Trajne reference idu preko Id-a, nikad preko imena.** Preimenovanje profila ili
tehnologije ne razbija ništa; prijenos biblioteke između računala (`ISettingsPortService`)
radi upsert po Id-u, pa se isti objekt ne duplicira.

Projekt i dalje nosi **ugrađenu kopiju** profila i tehnologije (samodostatnost —
otvara se na računalu bez tih zapisa u biblioteci), a `TechnologyId`/`Machine.Id`
služe povezivanju s bibliotekom kad ona postoji.

### Uvedeno PRIJE prvog izdanja

Namjerno: uvođenje identiteta nakon V1 značilo bi migraciju svih postojećih
projekata i biblioteka korisnika. Sada je trošak nula.
