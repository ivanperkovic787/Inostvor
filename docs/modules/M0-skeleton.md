# M0 — Skeleton (izvještaj modula)

## Cilj

Kompletan kostur projekta: solution sa svih 19 projekata prema Baseline v1.1,
DI + Serilog s dvostrukim izlazom (datoteka + Output Console panel), shell s
4 panela (Explorer / Canvas / Properties / Console), Undo/Redo infrastruktura
s punim testovima, Git repozitorij, Benchmarks projekt, TestData struktura.

## Implementirano

- **Root:** `.gitignore`, `.editorconfig` (naming pravila, file-scoped namespaces),
  `Directory.Build.props` (nullable, TreatWarningsAsErrors, analyzers),
  `Directory.Packages.props` (Central Package Management), `tools/build.ps1`.
- **Projekti:** 11 src + 7 test + 1 benchmark, reference točno po grafu ovisnosti
  iz Baseline v1.1 §3. Prazni projekti (Kernel, Geometry, Cam, Post, Data,
  Rendering, Import.NetDxf) sadrže samo csproj — sadržaj dolazi u svom modulu.
- **Core:** `IUndoableCommand`, `IUndoService`, `IDispatcherService`,
  `UndoRedoService` (kapacitet, LinkedList za izbacivanje najstarijeg),
  `CompositeCommand` (rollback pri iznimci usred izvršavanja).
- **Sdk:** `IPlugin`, `IPluginHost` — bazni kontrakti.
- **ViewModels:** `MainViewModel` (Undo/Redo komande, status), `ConsoleViewModel`
  (Messenger → UI thread marshaling, limit 2000 redaka), `LogMessage`, `LogLine`.
- **App:** Generic Host bootstrap, Serilog (File + ConsolePanelSink), 
  `DispatcherService`, shell layout s GridSplitterima, Mica backdrop, status traka.
- **Testovi:** 18 testova (UndoRedoService 12, CompositeCommand 6).
- **Benchmarks:** BenchmarkSwitcher ulazna točka; benchmarki se dodaju po modulima.

## Dizajnerske odluke M0 (unutar Baseline v1.1)

1. **Prazan ToolpathValidator NIJE registriran u DI u M0** (plan ga je spominjao):
   registracija bez ijednog pravila i bez domenskih tipova zahtijevala bi
   placeholder tipove, što krši pravilo "bez TODO/placeholder koda".
   `IValidationRule` + orkestrator dolaze u M3 s prvim geometrijskim pravilima.
2. **Undo/Redo testovi žive u novom `PlasmaCAM.Core.Tests`** (kod je u Core),
   ne u ViewModels.Tests kako je plan naveo.
3. **`Microsoft.Extensions.Logging.Abstractions` umjesto vlastitog `IAppLogger`:**
   standardna apstrakcija, strukturirani logging, nulta cijena vlastitog održavanja.
4. **xUnit 2.9.3 umjesto xUnit v3:** v3 mijenja model test projekta (Exe) i
   runner integraciju koje u ovom okruženju nije bilo moguće verificirati;
   2.9.x je provjeren. Migracija na v3 je izolirana (csproj + paketi) i može se
   napraviti bilo kada uz zeleni build kao dokaz.
5. **CompositeCommand radi rollback pri iznimci** — dokument nikad ne ostaje u
   pola grupne izmjene (nije bilo specificirano; smatram obveznim za komercijalni CAM).

## Definition of Done

- [x] Svi projekti + reference po Baseline grafu
- [x] Undo/Redo s testovima (18)
- [x] Serilog → datoteka + Console panel
- [x] Shell s 4 panela + splitteri + status traka
- [x] Git init + inicijalni commit
- [x] TestData struktura + politika regression DXF-ova
- [ ] `tools\build.ps1` zelen na Windows stroju (verifikacija na ciljnoj platformi)
