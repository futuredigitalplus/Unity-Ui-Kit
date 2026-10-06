#!/usr/bin/env bash
# Copies the samples and the smoke tests into a scratch Unity project so a test run can compile and run them.
#   run-samples-smoke.sh <unity project>
set -euo pipefail
if [ $# -ne 1 ] || [ ! -d "$1/Assets" ]; then
  echo "usage: $0 <unity project (a folder with Assets/)>" >&2
  exit 2
fi
here="$(cd "$(dirname "$0")" && pwd)"
root="$(cd "$here/../.." && pwd)"
target="$1/Assets/UIKitSamplesSmoke"
rm -rf "$target"
mkdir -p "$target/Samples" "$target/Tests"
for sample in UIKitQuickStart DesignSystemShowcase Components Animation Extras ShellQuickStart RpgInventory ActionHud GamepadMenu Responsive ThemingLanguage; do
  cp -R "$root/Samples~/$sample" "$target/Samples/$sample"
done
cp "$here/FutureDigitalPlus.UIKit.Samples.Smoke.Tests.asmdef" "$here/SamplesSmokeTests.cs" "$target/Tests/"
echo "copied into $target; remove it again with: rm -rf \"$target\" \"$target.meta\""
