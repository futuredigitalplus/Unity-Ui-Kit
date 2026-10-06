# Tools~/SamplesSmoke: does every sample run?

`SamplesSmokeTests.cs` is a PlayMode test per sample (ten tests for the eleven samples): it adds the sample component to an empty GameObject, waits a few frames, checks that the widgets the sample's README names exist,
lets 30 more frames of the sample's own `Update` run, destroys it and checks that no `UIDocument` is left. The five Components samples that have a layout are built as elements of one document
(`ComponentsSamples_EachComponentBuilds`); the Extras sample is style sheets only and the Addressables sample needs an Addressables group and a player content build (see its README), so both are covered by
compiling and importing them, not by a run. The test framework fails a test on any logged error or exception, so a sample that throws
while it builds or updates fails here. It does not click or press anything.

Samples are not compiled inside the package (`Samples~` is hidden from Unity), so the tests live here and run in a project that has the samples imported:

```
Tools~/SamplesSmoke/run-samples-smoke.sh <unity project>      # copies the samples and these tests into <project>/Assets/UIKitSamplesSmoke
unity test <project> --mode PlayMode --filter "FutureDigitalPlus.UIKit.Samples.Smoke" ...   # see Documentation~/Verification.md for the full command
rm -rf <project>/Assets/UIKitSamplesSmoke <project>/Assets/UIKitSamplesSmoke.meta           # afterwards
```

Use a scratch project (never a game). Package Manager imports a sample into `Assets/Samples/...`; the script does the same copy without the editor. The project needs the package with the modules the
samples use (all of them), and `com.unity.test-framework`. Run it in each variant of `Tools~/Matrix` that applies (Built-in, URP, HDRP, Input System only).

Run it for every variant with `Tools~/Matrix/run-matrix.sh <variant> --samples` (it copies the samples and these tests into the variant project and runs only them).

Not covered: input (clicks, keys, gamepad), the look of a sample (no screenshot comparison), devices.
