# Tools~/Matrix: the package's tests per variant

`run-matrix.sh <variant>` clones a clean Unity project, points it at (a copy of) this package, sets the variant up (optional packages, render pipeline asset,
colour space, Active Input Handling), runs the whole EditMode and PlayMode suites with the package in `testables`, and prints one table row. It is the
reproducible form of the matrix in `Documentation~/CompatibilityMatrix.md` (results) and `Documentation~/Platforms.md` (notes).

| File | What |
|---|---|
| `run-matrix.sh` | The runner (bash). `--list`, `--table`, `--dry-run`, `--samples`, `--classic`, `--player-compile`, `--export` |
| `variants.json` | The variants and the package versions the verified runs used |
| `matrix.py` | Project preparation and result summary (Python 3.9+, standard library only); `test_matrix.py` tests it without Unity |
| `Editor/` | Scripts copied into the variant project: `PlayerCompileCheck` (iOS, Android, macOS, Windows, Linux, WebGL; a target whose module is not installed is reported as "no module"), `PlayerExport` (IL2CPP, stripping High), `UrpSetup`, `HdrpSetup` (their asmdefs compile only when URP or HDRP is installed) |

## Use

```
M=<package>/Tools~/Matrix
$M/run-matrix.sh --list
$M/run-matrix.sh base   --base-project <clean Unity 6000.3 project that has been opened once> --work <scratch dir>
$M/run-matrix.sh urp2d  --base-project ... --work ... --player-compile --export ios --export android
$M/run-matrix.sh --table --work <scratch dir>        # all variants run so far, one table
```

Variants: `base`, `base-linear`, `legacy`, `input-system`, `both`, `ugui`, `localization`, `addressables`, `all-optional`, `urp3d`, `urp2d`, `urp3d-linear`, `hdrp-linear` (HDRP supports Linear only: in Gamma it logs an error for every camera that renders, which fails the tests that render with a camera).
The table has one row per variant: pipeline / renderer, colour space, input, EditMode and PlayMode pass counts, how many themed-panel paint tests passed, compiler errors and
warnings seen in the logs, player compile result per platform, IL2CPP export result.

`--classic` installs the kit as a classic Assets package instead (no manifest entry, no `testables`; `Tools~/make_classic_kit.py` writes `<project>/Assets/FutureDigitalPlus/UIKit`; the row is labelled `<variant>-classic`). Same variants, same expected numbers.

Rules of the runner:
- It never touches the base project (it clones it with `cp -Rc`) and refuses a path that contains `Color-Solitaire`. Unity must only ever run on a scratch project.
- By default the variant references a **copy** of the package (`<work>/<variant>/pkg`), so Unity cannot write `.meta` files into the live package. Pass `--live` when that is the point
  (an integration run that harvests the metas of new files).
- It deletes `Library/Bee` and `Library/ScriptAssemblies` in the clone first, so every compiler warning is printed again; `--no-recompile` keeps them.
- One Unity process per project, at most three variants at once. A variant takes many minutes (package resolution on the first run, then two test runs): run it in the background and
  poll `<work>/<variant>/status.txt`; the logs are `edit.log`, `play.log`, `setup.log`, `pcc.log`, `export-*.log`, the results `results-edit.xml`, `results-play.xml`, `summary.json`.
- The variants other than `base` need the Package Manager to download the optional packages (registry access). Versions are pinned in `variants.json` to the ones the verified runs used.
- EditMode runs without `-nographics` (eight Components editor tests open an editor window); PlayMode runs with graphics so panels really render.

What the tests print: `FDPMATRIX env ...` (pipeline, renderer, device, colour space), `FDPMATRIX paint ...`, `FDPMATRIX blur ...`, `FDPMATRIX fxshader ...`
(from `Tests/Runtime/Platform`); the runner reads them from `play.log`.

## Tests of the tool

`python3 -m unittest test_matrix` (in this folder) checks the variants file, the project preparation (manifest, `ProjectSettings.asset`, copied scripts) and the summary and table on fixture results.
The runner was used for real on 2026-10-06 (Unity 6000.3.25f1, macOS arm64): every variant of `variants.json` and the `--samples` runs; the results are in `Documentation~/CompatibilityMatrix.md`. It starts Unity directly (`-batchmode -runTests ... -logFile`), so the logs are the editor logs and no Unity CLI is needed.
