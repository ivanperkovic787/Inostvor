# ADR-001: Koordinatni sustav — double u domeni, Int64 na Clipper2 granici

**Status:** Prihvaćeno (Baseline v1.1, 2026-07-09)

## Kontekst

Razmatrana je cjelobrojna CAM jezgra (Int64, 1 µm) radi numeričke stabilnosti,
posebno oko Clipper2 operacija (kerf offset, booleove operacije).

## Odluka

Domena i cijeli pipeline rade u `double` (milimetri). Cjelobrojne koordinate
(Int64, skala ×1000 = 1 µm) koriste se isključivo unutar Clipper2 adaptera
(`PlasmaCAM.Geometry.Offset.ClipperAdapter`, dolazi u M5).

## Obrazloženje

1. Clipper2 je iznutra već Int64 — robusnost integer aritmetike dobivamo točno
   tamo gdje je kritična, bez propagiranja kroz sustav.
2. Potpuno integer jezgra degradira lukove: centri, radijusi, tangente leadova i
   biarc fitting ne leže na integer mreži; svaka konverzija kumulira grešku.
3. Double (~15,9 značajnih znamenki) daje sub-nanometarsku rezoluciju na tabli od 3 m.
4. Stvarni rizik doublea su usporedbe — rješava se centralizirano: `Kernel.Tolerance`
   je JEDINO mjesto s epsilonima. Lokalni epsiloni i `a == b` nad doubleovima su zabranjeni.

## Posljedice

- `ClipperAdapter` radi eksplicitnu mm↔µm konverziju s testovima rubnih slučajeva
  (koordinate > 10 m, kerf < 0.5 mm, rupe usporedive s kerfom).
- `Kernel.Tolerance` (M1) definira: geometrijski epsilon 1e-6 mm (fiksan),
  toleranciju spajanja kontura (podesiva, default 0.05 mm).
