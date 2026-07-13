# M8 — Persistencija (izvještaj modula)

## Zahtjevi ↔ implementacija

1. **Format projekta.** `.ino` = ZIP (manifest + project.json + originalni DXF-ovi).
   Sadrži: DXF izvore, tehnologiju (kerf, leadovi, overcut, strategija redoslijeda),
   ugrađenu KOPIJU profila stroja (projekt se otvara i na računalu bez tog profila),
   id postprocesora (kroz profil), simulacijski checkpoint, korisničke postavke
   projekta, te `Extensions` za buduće module. Detalji i obrazloženje: **ADR-005**.
2. **Verzioniranje.** `formatVersion` u manifestu + lanac `IProjectMigration`
   (migracije nad JSON-om). Testirano: v0 → v1 migracija, jasna greška kad
   migracija nedostaje ili je format noviji od programa.
3. **SQLite ≠ istina projekta.** Tri odvojena sloja (tablica u ADR-005). Baza
   se smije obrisati — nijedan projekt se ne gubi.
4. **Machine Profile Manager.** Profil: naziv, proizvođač, radno područje,
   SafeZ/PierceHeight/CutHeight, ProbeMacro, **HasThc**, proces, zadani
   postprocesor, zadana tehnologija, Extra vreća. ViewModel s New/Save/Delete;
   biblioteka se pri prvom pokretanju puni ugrađenim profilima.
5. **Technology Library.** `TechnologyEntry`: naziv, materijal, debljina, plin,
   amperaža, `TechnologySettings` (kerf/feed/pierce/visine) + Extra. Jedna
   tehnologija primjenjiva na više projekata (živi u SQLiteu, kopira se u projekt).
6. **Auto Save.** Timer 60 s → `autosave.ino` (običan projekt, ne poseban format).
   Detekcija pada: sentinel `session.lock` postoji dok app radi, briše se pri
   urednom izlasku; ako pri startu postoji + postoji autosave → ContentDialog nudi
   oporavak. Greška autosavea nikad ne ruši rad korisnika.
7. **Import/Export postavki.** `ISettingsPortService` — profili + tehnologije u
   jednu JSON datoteku, upsert pri uvozu (testirano: izvoz iz jedne baze, uvoz u
   praznu, identični podaci).
8. **Prostor za budućnost.** `Extensions` (nesting, tabovi, višestruki limovi,
   ostaci, materijali, optimizacija) — nepoznate sekcije se čuvaju u round-tripu,
   testirano. Nijedan budući modul ne traži promjenu formata ni migraciju.
9. **About.** `AboutInfo` (ViewModels): naziv Inostvor, verzija iz assemblyja,
   autor, godina, copyright, opis, pozdravna poruka, **popis licenci otvorenog
   koda** (10 komponenti). Model je gotov; UI dijalog ostaje planiran.

## Odluka koju tražim da potvrdiš

**Derivirani podaci se ne spremaju u projekt** (konture, validacijski nalazi,
putanja, G-kod) — regeneriraju se pri otvaranju iz DXF izvora. To je moguće jer je
cjevovod M3–M7 bajt-determinističan. Dobit: mali projekti, nemoguće nekonzistentno
stanje, format otporan na promjene algoritama. Cijena: otvaranje velikog projekta
troši sekundu-dvije na regeneraciju. Ako želiš, kasnije se može dodati OPCIONALNI
cache putanje u kontejner (kao ubrzanje, ne kao istinu).

## Testovi

ProjectStoreTests: 6 (round-trip s očuvanjem DXF bajtova, Extensions round-trip,
noviji format → greška, migracija v0→v1, nedostajuća migracija, atomarnost) ·
SqliteRepositoryTests: 5 (CRUD profila sa svim poljima, upsert/delete, tehnologije,
postavke, izvoz→uvoz u novu bazu). Ukupno **11**.

## Definition of Done

- [x] .ino format s verzijom i migracijama; SQLite kao aplikacijska razina
- [x] Machine Profile Manager i Technology Library (repozitoriji + ViewModeli)
- [x] Auto Save s oporavkom nakon pada; Import/Export postavki
- [x] Extensions kanal za buduće module (round-trip testiran)
- [x] AboutInfo model s licencama
- [ ] Build + testovi na Windows stroju
