# M1 — Kernel (izvještaj modula)

## Cilj

Matematička jezgra PlasmaCAM-a: primitivi, transformacije, intersekcije, AABB,
prostorni indeks i Tolerance sustav (ADR-001). Sve ostalo u sustavu gradi se na
ovom modulu — naglasak na numeričkoj stabilnosti i pokrivenosti testovima.

## Implementirano

**Primitives:** `Point2`, `Vector2` (record structs; egzaktna sintetizirana jednakost,
geometrijska kroz `AlmostEquals`/Tolerance), `Aabb` (uvijek validan, bez "empty" sentinela),
`ISegment`, `LineSeg`, `ArcSeg` (lukovi first-class: predznačeni sweep, puni krug,
`FromStartEndCenter`, `FromBulge`), `Polyline2` (validirana povezanost, zatvorenost, Reversed).

**Transforms:** `Matrix3x2d` — double verzija System.Numerics konvencije (row-vector),
translacija/rotacija/skala (i oko točke), kompozicija, `TryInvert`.

**Intersections:** line-line (+ zasebna detekcija kolinearnog preklapanja), line-arc
(sekanta/tangenta/promašaj + sweep filter), arc-arc (uklj. tangente; koincidentne
kružnice dokumentirano vraćaju 0), `SegmentIntersection` dispatcher,
`PolylineSelfIntersection` sweep-and-prune.

**Spatial:** `ISpatialIndex<T>` (build-and-query kontrakt), `AabbTree<T>` — BVH s
inkrementalnim umetanjem po perimetar-heuristici.

**Tolerance / MathUtil:** jedina epsilon točka u sustavu; normalizacija kutova,
signed area (shoelace), lerp, deg↔rad.

## Ključne dizajnerske odluke

1. **Segmenti su sealed klase, točke/vektori record structi.** ISegment polimorfizam
   nad structovima boksira pri svakom pristupu; klase daju čist polimorfizam bez
   iznenađenja. Hot-path tipovi (Point2/Vector2) ostaju structovi. Benchmarki čuvaju prag.
2. **Sweep-and-prune umjesto pune Bentley–Ottmann.** Ista točnost rezultata,
   red veličine manje koda i nula degeneracijskih rubnih slučajeva; O(n log n + k·m)
   praktički identičan za CAM geometrije. Benchmark do 5000 segmenata.
3. **AabbTree bez rebalansirajućih rotacija.** Korektnost upita ne ovisi o balansu;
   randomizirani test uspoređuje rezultate s brute-force referencom, benchmark
   QueryTree vs QueryLinear čuva prag performansi. Rotacije se dodaju tek ako
   mjerenja pokažu potrebu (YAGNI uz mjerni instrument, ne slijepo).
4. **Kutna tolerancija lukova izvedena iz geometrijske na trenutnom polumjeru**
   (`Geometric/R + Angular`) — tolerancije su konzistentno u milimetrima, ne u
   radijanima, pa se veliki i mali lukovi ponašaju identično u mm prostoru.
5. **DXF bulge konvencija eksplicitno testirana.** Pozitivan bulge = CCW = luk
   ISPOD tetive koja putuje u +X (centar lijevo od smjera putovanja). Klasičan
   izvor zrcaljenih lukova pri importu — round-trip testovi za 6 bulge vrijednosti
   uključujući velike lukove (|b| > 1).
6. **Degenerirani primitivi odbijaju se na izvoru** (nul-segment, nul-radijus,
   nul-sweep bacaju u konstruktoru) — nizvodni kod ne mora braniti od smeća.

## Testovi

14 test klasa, ~100 test metoda (Theory slučajevi dodatno množe):
svi primitivi, sve grane intersekcija (sekante/tangente/promašaji/sweep filteri/
tolerancijski rubovi), samopresjeci (bowtie, kvadrat, tangencijalni luk, luk-luk,
500-segmentni smoke), AabbTree randomizirano protiv brute-force reference,
matrice s round-trip inverzijom.

## Benchmarki

`IntersectionBenchmarks` (line-line/line-arc/arc-arc), `SelfIntersectionBenchmarks`
(100/1000/5000 segmenata), `AabbTreeBenchmarks` (build + query vs linearni baseline,
1k/10k kutija). Pokretanje: `dotnet run -c Release --project benchmarks/PlasmaCAM.Benchmarks`.

## Definition of Done

- [x] Svi javni API-ji s XML dokumentacijom
- [x] Testovi pokrivaju sve javne članove i rubne slučajeve
- [x] Benchmarki za intersekcije, samopresjeke i prostorni indeks
- [x] Bez TODO markera; statičke provjere zelene
- [ ] `tools\build.ps1` + testovi zeleni na Windows stroju (autoritativna verifikacija)
