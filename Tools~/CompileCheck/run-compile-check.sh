#!/usr/bin/env bash
# Compiles every assembly of the package with Roslyn (no Unity), checks the XML docs listed in doc-scope.txt and the name hazards.
#   run-compile-check.sh [--tests] [--samples] [--work <dir>] [--unity <Unity.app>] [--test-project <dir>] [--dump-generated <dir>] [--define SYMBOL ...]
# --define: scripting symbols added to every assembly, for example UNITY_WEBGL or UNITY_STANDALONE_WIN, to compile the code a platform enables (an approximation: Unity's own player compile needs the platform module).
# --work: where build output and plan.json go (default: a temp folder, never inside the package).
# --tests needs --test-project (a Unity project that has run the tests once: its Library holds the TestRunner and NUnit assemblies).
set -euo pipefail
here="$(cd "$(dirname "$0")" && pwd)"
root="$(cd "$here/../.." && pwd)"
work="${TMPDIR:-/tmp}/fdp-compile-check"
plan_args=()
run_args=()
while [ $# -gt 0 ]; do
  case "$1" in
    --work) work="$2"; shift 2 ;;
    --unity) plan_args+=(--unity "$2"); shift 2 ;;
    --test-project) plan_args+=(--test-project "$2"); shift 2 ;;
    --define) plan_args+=(--define "$2"); shift 2 ;;
    --tests|--samples) plan_args+=("$1"); shift ;;
    --dump-generated) run_args+=(--dump-generated "$2"); shift 2 ;;
    *) echo "unknown option $1" >&2; exit 2 ;;
  esac
done
mkdir -p "$work"
python3 "$here/plan.py" "$root" --out "$work/plan.json" ${plan_args[@]+"${plan_args[@]}"}
dotnet build "$here/CompileCheck.csproj" --nologo -v q --artifacts-path "$work/artifacts" >/dev/null
dotnet "$work/artifacts/bin/CompileCheck/debug/CompileCheck.dll" "$work/plan.json" --docs "$here/doc-scope.txt" --hazards ${run_args[@]+"${run_args[@]}"}
