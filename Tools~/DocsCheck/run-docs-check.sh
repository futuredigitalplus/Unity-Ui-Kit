#!/usr/bin/env bash
# Compiles and runs the code of the documentation in a scratch Unity project.
#   run-docs-check.sh <unity project (a folder with Assets/)> [--run]
# Without --run it only checks the Markdown and copies the files; with --run it also starts Unity (PlayMode, only these tests) and removes the copy afterwards.
# The project must reference the package; the optional packages do not matter. Never use a game.
set -euo pipefail
if [ $# -lt 1 ] || [ ! -d "$1/Assets" ]; then
  echo "usage: $0 <unity project (a folder with Assets/)> [--run]" >&2
  exit 2
fi
project="$1"
run="${2:-}"
here="$(cd "$(dirname "$0")" && pwd)"
root="$(cd "$here/../.." && pwd)"
unity="${UNITY:-/Applications/Unity/Hub/Editor/6000.3.25f1/Unity.app/Contents/MacOS/Unity}"
target="$project/Assets/UIKitDocsCheck"

python3 "$here/check-docs.py" "$root"

rm -rf "$target" "$target.meta"
mkdir -p "$target"
cp -R "$here/Unity/." "$target/"
python3 "$here/extract-doc-code.py" "$root" "$target"
echo "copied into $target"

if [ "$run" != "--run" ]; then
  echo "run it with: $0 $project --run   (or run the assembly FutureDigitalPlus.UIKit.DocsCheck.Tests in the Test Runner); remove it with: rm -rf \"$target\" \"$target.meta\""
  exit 0
fi

log="$project/docs-check.log"
results="$project/docs-check.xml"
status=0
"$unity" -batchmode -projectPath "$project" -runTests -testPlatform PlayMode -assemblyNames FutureDigitalPlus.UIKit.DocsCheck.Tests \
  -testResults "$results" -logFile "$log" || status=$?
grep -E 'error CS|warning CS' "$log" | sort -u || true
rm -rf "$target" "$target.meta"
echo "unity exit code $status (0 = every recipe and getting-started step ran); log $log, results $results"
exit $status
