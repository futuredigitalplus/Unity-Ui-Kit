#!/usr/bin/env bash
# Runs the package's tests (and optional player checks) in one matrix variant, in a scratch copy of a Unity project, and prints one table row.
#
#   run-matrix.sh <variant> [options]        run a variant (see variants.json: base, urp3d, hdrp-linear, all-optional, ...)
#   run-matrix.sh --list                     list the variants
#   run-matrix.sh --table [--work <dir>]     print the table of all variants that have been run
#
# Options
#   --base-project <dir>   a clean Unity 6000.3 project that has been opened once (default: $FDP_MATRIX_BASE_PROJECT).
#                          It is cloned (cp -Rc, instant on APFS); it is never modified and never the owner's game.
#   --work <dir>           where variant projects, logs and results go (default: $FDP_MATRIX_WORK or ${TMPDIR:-/tmp}/fdp-matrix)
#   --editor-version <v>   default: "unity" in variants.json
#   --unity <path>         the Unity editor executable (default: $FDP_UNITY, else the Unity Hub install of the editor version on macOS or Linux; set it on Windows).
#                          Unity is started directly (-batchmode, -logFile), so no Unity CLI is needed and the log of every run is the editor log
#   --player-compile       also compile the runtime assemblies for iOS, Android and macOS standalone (editor script)
#   --export ios|android   also export an IL2CPP player project with Managed Stripping High (repeatable; needs the platform module)
#   --no-recompile         keep the clone's Library (default deletes Library/Bee and Library/ScriptAssemblies so every warning is printed)
#   --samples              the samples run instead of the test suites: all 11 samples and Tools~/SamplesSmoke are copied into the project and only the
#                          PlayMode smoke tests run (EditMode is skipped: the Components samples have no convention layouts, which the editor tests of the
#                          package would report). The variant's work folder is <variant>-samples
#   --live                 reference the live package folder instead of a copy of it. Unity then writes .meta files for new files into the
#                          live package (what an integration run wants); the default copy keeps the live package untouched
#   --classic              install the kit as a classic Assets package: Tools~/make_classic_kit.py writes <project>/Assets/FutureDigitalPlus/UIKit and the manifest has no
#                          kit entry and no testables (the work folder is <variant>-classic). Same variants, same tests, same expected numbers as the UPM form
#   --keep                 keep the variant project afterwards (default: delete the clone but keep logs, results and summary.json)
#   --filter <name>        pass -testFilter to both test runs (NUnit name filter)
#   --dry-run              print the commands, run nothing
#
# One Unity process per variant project; run at most three variants at the same time. Each unity command can take many minutes:
# start this script in the background and poll <work>/<variant>/status.txt. Never run it against a game project.
set -euo pipefail

here="$(cd "$(dirname "$0")" && pwd)"
package="$(cd "$here/../.." && pwd)"
work="${FDP_MATRIX_WORK:-${TMPDIR:-/tmp}/fdp-matrix}"
base="${FDP_MATRIX_BASE_PROJECT:-}"
version=""
unity_bin="${FDP_UNITY:-}"
player_compile=0
exports=()
recompile=1
keep=0
live=0
classic=0
samples=0
filter=""""
dry=0
variant=""
mode="run"

while [ $# -gt 0 ]; do
  case "$1" in
    --list) mode="list"; shift ;;
    --table) mode="table"; shift ;;
    --base-project) base="$2"; shift 2 ;;
    --work) work="$2"; shift 2 ;;
    --editor-version) version="$2"; shift 2 ;;
    --unity) unity_bin="$2"; shift 2 ;;
    --player-compile) player_compile=1; shift ;;
    --export) exports+=("$2"); shift 2 ;;
    --no-recompile) recompile=0; shift ;;
    --keep) keep=1; shift ;;
    --live) live=1; shift ;;
    --samples) samples=1; shift ;;
    --classic) classic=1; shift ;;
    --filter) filter="$2"; shift 2 ;;
    --dry-run) dry=1; shift ;;
    -h|--help) sed -n '2,24p' "$0"; exit 0 ;;
    -*) echo "unknown option $1" >&2; exit 2 ;;
    *) variant="$1"; shift ;;
  esac
done

variants="$here/variants.json"

if [ "$mode" = "list" ]; then
  python3 - "$variants" <<'PY'
import json, sys
data = json.load(open(sys.argv[1]))
for name, v in data["variants"].items():
    print(f"{name:14} {v['description']}")
PY
  exit 0
fi

if [ "$mode" = "table" ]; then
  python3 "$here/matrix.py" table --work "$work"
  exit 0
fi

[ -n "$variant" ] || { echo "usage: run-matrix.sh <variant> [options]  (run-matrix.sh --list shows the variants)" >&2; exit 2; }
[ -n "$base" ] || { echo "No base project: pass --base-project <dir> or set FDP_MATRIX_BASE_PROJECT (a clean Unity project that has been opened once)." >&2; exit 2; }
[ -d "$base/ProjectSettings" ] || { echo "'$base' is not a Unity project (no ProjectSettings)." >&2; exit 2; }
case "$base" in
  *FCSolitaireUnity*) echo "Refusing to use a game project as the base project." >&2; exit 2 ;;
esac

[ -n "$version" ] || version="$(python3 -c "import json,sys; print(json.load(open('$variants'))['unity'])")"
if [ -z "$unity_bin" ]; then
  case "$(uname -s)" in
    Darwin) unity_bin="/Applications/Unity/Hub/Editor/$version/Unity.app/Contents/MacOS/Unity" ;;
    Linux) unity_bin="$HOME/Unity/Hub/Editor/$version/Editor/Unity" ;;
    *) echo "Pass --unity <path to Unity.exe> or set FDP_UNITY." >&2; exit 2 ;;
  esac
fi
[ "$dry" = 1 ] || [ -x "$unity_bin" ] || { echo "No Unity editor at '$unity_bin' (pass --unity or set FDP_UNITY)." >&2; exit 2; }
label="$variant"
[ "$samples" = 0 ] || label="$variant-samples"
[ "$classic" = 0 ] || label="$label-classic"
dir="$work/$label"
project="$dir/project"
method="$(python3 "$here/matrix.py" pipeline-method --variant "$variant")"

status() { [ "$dry" = 1 ] || echo "$(date +%H:%M:%S) $*" >> "$dir/status.txt"; }

run() {  # run <logfile> <command...>: prints the command, then runs it unless --dry-run; a failing command does not stop the matrix (the exit code goes to status.txt)
  local log="$1"; shift
  echo "+ $*  > $log"
  if [ "$dry" = 1 ]; then return 0; fi
  set +e
  "$@" > "$log.stdout" 2>&1   # Unity writes its own log to $log (-logFile); stdout only holds what the shell sees
  local code=$?
  set -e
  status "  exit $code ($(basename "$log"))"
}

if [ "$dry" = 0 ]; then
  rm -rf "$dir"
  mkdir -p "$dir"
fi

pkg="$package"
if [ "$live" = 0 ]; then pkg="$dir/pkg"; fi

status "clone $base"
echo "+ cp -Rc $base $project"
[ "$live" = 1 ] || [ "$classic" = 1 ] || echo "+ rsync -a $package/ $pkg/"
[ "$classic" = 0 ] || echo "+ make_classic_kit.py --src $package --dst $project/Assets/FutureDigitalPlus/UIKit"
if [ "$dry" = 0 ]; then
  cp -Rc "$base" "$project" 2>/dev/null || cp -R "$base" "$project"
  if [ "$live" = 0 ] && [ "$classic" = 0 ]; then rsync -a --exclude .git --exclude bin --exclude obj --exclude out "$package/" "$pkg/"; fi
  if [ "$classic" = 1 ]; then
    python3 "$here/matrix.py" prepare --project "$project" --package "$package" --variant "$variant" --classic
    python3 "$here/../make_classic_kit.py" --src "$package" --dst "$project/Assets/FutureDigitalPlus/UIKit" --quiet
  else
    python3 "$here/matrix.py" prepare --project "$project" --package "$pkg" --variant "$variant"
  fi
  if [ "$samples" = 1 ]; then "$here/../SamplesSmoke/run-samples-smoke.sh" "$project"; fi
  if [ "$recompile" = 1 ]; then rm -rf "$project/Library/Bee" "$project/Library/ScriptAssemblies"; fi
fi

if [ -n "$method" ]; then
  status "pipeline setup $method"
  run "$dir/setup.log" "$unity_bin" -batchmode -nographics -projectPath "$project" -logFile "$dir/setup.log" -quit -executeMethod "$method"
fi

extra=()
[ -n "$filter" ] && extra=(-testFilter "$filter")

if [ "$samples" = 1 ]; then
  filter="FutureDigitalPlus.UIKit.Samples.Smoke"
  extra=(-testFilter "$filter")
fi

if [ "$samples" = 0 ]; then
status "EditMode"
run "$dir/edit.log" "$unity_bin" -batchmode -projectPath "$project" -runTests -testPlatform EditMode -testResults "$dir/results-edit.xml" -logFile "$dir/edit.log" ${extra[@]+"${extra[@]}"}
fi
status "PlayMode"
run "$dir/play.log" "$unity_bin" -batchmode -projectPath "$project" -runTests -testPlatform PlayMode -testResults "$dir/results-play.xml" -logFile "$dir/play.log" ${extra[@]+"${extra[@]}"}

if [ "$player_compile" = 1 ]; then
  status "player compile"
  run "$dir/pcc.log" "$unity_bin" -batchmode -nographics -projectPath "$project" -logFile "$dir/pcc.log" -executeMethod FdpMatrix.PlayerCompileCheck.Run
fi

for target in ${exports[@]+"${exports[@]}"}; do
  case "$target" in
    ios) m="FdpMatrix.PlayerExport.Ios"; bt="iOS" ;;
    android) m="FdpMatrix.PlayerExport.Android"; bt="Android" ;;
    *) echo "unknown export target $target (ios|android)" >&2; exit 2 ;;
  esac
  status "export $target"
  run "$dir/export-$target.log" "$unity_bin" -batchmode -nographics -projectPath "$project" -buildTarget "$bt" -logFile "$dir/export-$target.log" -executeMethod "$m"
done

status "summarize"
if [ "$dry" = 0 ]; then
  python3 "$here/matrix.py" summarize --dir "$dir" --variant "$variant" --label "$label"
  if [ "$keep" = 0 ]; then rm -rf "$project"; fi
fi
status "done"
