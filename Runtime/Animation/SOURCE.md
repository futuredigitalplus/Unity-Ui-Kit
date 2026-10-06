# Animation: source record

- Upstream: instance-id/ElementAnimationToolkit (package `id.instance.elementanimationtoolkit` 0.1.93)
- URL: https://github.com/instance-id/ElementAnimationToolkit
- Commit: `a5d518427e8c7fa7a30e3345d62107356e26e046`
- Licence: MIT, (c) 2020 instance.id (file kept in this folder: `LICENSE.md`; full text also in `../../THIRD_PARTY_NOTICES.md`)
- Upstream source root used: `Assets/instance.id/ElementAnimationToolkit/Runtime/` (upstream path), now `Runtime/Animation/` here.
  Upstream folder structure and type names are unchanged. The namespaces (`instance.id.EATK`, `instance.id.EATK.Extensions`) and the assembly
  name `id.instance.elementanimationtoolkit` were renamed to `FutureDigitalPlus.UIKit.Animation[.Extensions]` (see "Rename record"); to diff a
  future upstream version, push it through `Tools~/rename/rename.py` first (`Documentation~/Upgrading.md`).
- All `.meta` files of the copied files and folders are the upstream ones (GUIDs unchanged), except the two new `Resources`
  folders listed below, whose metas Unity generates on import.
- Usage guide: `../../Documentation~/Animation/README.md`. Sample: `../../Samples~/Animation/`. Tests: `../../Tests/Runtime/Animation/`.

## The unavailable dependency `id.instance.extensions`

Upstream `package.json` declares `"id.instance.extensions": "file:../../id.instance.packagerepo/..."` (a local file path on the author's
machine), and the upstream asmdef has a `versionDefines` entry `USING_IDEXTENSIONS` for it.

Findings:

- **No source file references the package.** `USING_IDEXTENSIONS` is never used in any `#if`; no `using` or type comes from an assembly
  named `id.instance.extensions` (`grep` over all upstream `.cs` files). The only code with the same origin is bundled in the upstream
  repository itself, under `Runtime/Scripts/Extensions` (file headers read "Project: https://github.com/instance-id/Extensions"), in the
  namespace `instance.id.EATK.Extensions`.
- So nothing had to be copied from another source and nothing needed to be re-implemented. The helper methods the toolkit uses
  (`Create`, `ToUSS`, `SetOpacity`, `SetDisplay`, `Zero`, `ForEachR`, `FromHex`, `TryParseFloat`, `CreateStyleData`, ...) are the bundled ones,
  kept at their upstream paths (not in a separate `Vendored/` folder; there is none).
- The asmdef `versionDefines` for `USING_IDEXTENSIONS`, `USING_ADDRESSABLES` and `USING_HYBRID` and the five `references` (GUIDs of
  assemblies that are not in this package) were removed. Nothing uses those defines any more. The assembly compiles without references.
- Other optional hooks of the same kind are removed by exclusion below: `ID_LOGGER`, `ID_INFOPANEL` (in `Timer.cs`, excluded),
  `IS_DEV` (`ImageAsset.cs`, excluded), `ID_EATK` (editor window, excluded).

## Copied (25 C# files, 3 USS files, 1 asmdef)

Upstream path (under `Runtime/`) = path here (under `Runtime/Animation/`), except where noted.

| Folder | Files |
|---|---|
| `Animations/` | `AnimationExtensions.cs`, `AnimationSequences.cs`, `ContinuousAnimations.cs`, `Easy.cs`, `HoverAnimations.cs`, `VisualElementBaseAnimation.cs` |
| `Elements/AnimatedFoldout/` | `AnimatedFoldout.cs`, `UIElementExpander.cs`, `Resources/FutureDigitalPlus/UIKit/Animation/Styles/AnimatedFoldoutStyle.uss` (upstream `Resources/Style/`) |
| `Elements/AnimatedLabel/` | `AnimatedLabel.cs`, `Resources/FutureDigitalPlus/UIKit/Animation/Styles/AnimatedLabel.uss` (upstream: `Style/AnimatedLabel.uss`, moved into a `Resources` folder) |
| `Elements/BetterTextField/` | `MaskedInputField.cs`, `Resources/FutureDigitalPlus/UIKit/Animation/Styles/MaskedInputField.uss` (upstream: `Style/MaskedInputField.uss`, moved into a `Resources` folder) |
| `Enhancements/Tasks/` | `ITaskSequence.cs`, `SingleTask.cs`, `WaitUntil.cs` |
| `Util/` | `ColorUtil.cs` |
| `Scripts/Extensions/` | `CollectionExtensions.cs`, `ParseElements.cs`, `Elements/VisualElementExtension.cs`, `VisualElementEvents/VisualElementEvents.cs` |
| `Scripts/Extensions/DataTypes/` | `AnimColorStore.cs`, `AnimTime.cs`, `AnimValueStore.cs`, `AnimateElement.cs`, `AnimatedItems.cs`, `GetEasing.cs`, `VisualElementStyleStore.cs` |
| root | `FutureDigitalPlus.UIKit.Animation.asmdef` (upstream `id.instance.elementanimationtoolkit.asmdef`; rewritten, see below), `LICENSE.md` |

Files written for this package, not from upstream: this `SOURCE.md`, `../../Documentation~/Animation/README.md`,
`../../Samples~/Animation/*`, `../../Tests/Runtime/Animation/*`, and `Animations/EatkMotion.cs` (no upstream header): `EatkMotion.Scaler`
(a `Func<int,int>`, default null, reset on subsystem registration) and `EatkMotion.Scale(int ms)`, the one hook through which the module honours
reduced motion or an animation speed setting. The module still references no other module; the UIKit facade (or a game) assigns the scaler,
for example from `AccessibilityApplier.ScaleMotion`.

## Excluded (upstream paths under `Assets/instance.id/ElementAnimationToolkit/`)

| Excluded | Reason |
|---|---|
| `Runtime/Scripts/UI/EATKEditor.cs`, `Runtime/Scripts/UI/Windows/EATKEditor/EATKEditor.cs` (+ `Style/EATKEditor.uss`), `Runtime/Scripts/UI/Windows/BaseEditorWindow/**` (`idEditorWindow.cs`, sass, uss), `Runtime/Scripts/UI/ElementData/ObjectElementData.cs` | The upstream feature-tour window (*Tools > instance.id > Element Animation Toolkit*) and its base window. Editor-only, about 2 600 lines, and built on the excluded editor helpers below. `Windows/EATKEditor/EATKEditor.cs` is a second copy compiled only with `ID_EATK`. `Samples~/Animation` replaces it with a small runtime demo. |
| `Runtime/Scripts/UI/Style/Resources/Fonts/SourceCodePro-*.otf` and `Runtime/Scripts/UI/Windows/EATKEditor/Style/Resources/Fonts/SourceCodePro-*.otf` (14 files each) | Adobe *Source Code Pro* font files. They carry their own licence (SIL Open Font License 1.1, with a Reserved Font Name; stated inside the font files), and upstream ships no licence text for them. Not redistributed here. `MaskedInputField.MonoFont` lets the game supply a monospaced font. |
| `Runtime/Scripts/ScriptableObjects/**` (`AssetInstance.cs`, `StyleAssets/EATKStyleAsset.cs`) | Editor-oriented asset helpers for the excluded window (`AssetDatabase`, `ID_LOGGER`). |
| `Runtime/Scripts/Extensions/ElementGeneration/**` (`ElementGeneration.cs`, `IMGuiObject.cs`, `UIElementsEditorHelper.cs`, `Container*.cs`) | Editor UI generator (IMGUI bridge, `UnityEditor` APIs, reflection over serialized fields). Not animation. |
| `Runtime/Scripts/Extensions/AssetFileExtensions.cs` | `AssetDatabase` helpers (style sheet, font and script lookup, "jump to code" menu). Editor only. The three elements that used it now load stylesheets with `Resources.Load`. |
| `Runtime/Scripts/Extensions/JumpTarget/**` | Support for the "jump to code" menu of the excluded window. |
| `Runtime/Scripts/Extensions/DataTypes/{ClassData,FieldData,ImageAsset,ListPool,SerializedDictionary}.cs`, `DataTypes/Editor/SerializedDictionaryEditor.cs`, `DataTypes/VersionElement/**` | Used only by the excluded editor code (`ImageAsset` needs `AssetInstance` and `IS_DEV`; `SerializedDictionary` produced compiler warnings and needs an editor drawer; `VersionElement` is a version-number field, not animation). |
| `Runtime/Scripts/Extensions/{LinqExtensions,ReflectionExten,Timer,PropertyExtensions}.cs` | `LinqExtensions` re-declares `Append`, `Prepend`, `ToHashSet`, `Concat` and `Join` extension methods that clash with `System.Linq`, and nothing needs it. `ReflectionExten` served the excluded `ClassData`/`FieldData`. `Timer` is an internal performance logger bound to the unavailable `ID_LOGGER` / `ID_INFOPANEL` packages. `PropertyExtensions` needs `SerializedProperty` (editor). |
| `Runtime/Scripts/Extensions/Color/**` (`Colors.cs`, `ColorTool.cs`, `ColorExtensions.cs`, `MathEx.cs`, `GetColor.cs`) | `Colors.cs` is a colour-name list whose header says it was taken from a public gist (`gist.github.com/louisvalet/d37f3bc259a43bfbfecec7dd5e376354`) with no licence stated; `ColorTool`, `ColorExtensions` (random colours, buggy `DistinctColors`) and `MathEx` only support it. `GetColor.cs` is entirely commented out upstream. The single use in the kept code (`Colors.DolphinGray`, in `CreateWithHeader`) is replaced by the same RGB value inline. `ColorUtil` (kept) provides `FromHex`. |
| `Runtime/Scripts/Extensions/VisualElementExtension.cs` | Upstream has two copies of this class; this one is fully commented out. The active copy is `Extensions/Elements/VisualElementExtension.cs` (kept). |
| `Runtime/Elements/Attributes/EditorFieldAttribute.cs` | Attribute for the excluded editor generator (also would not compile: `using System.Runtime.InteropServices.WindowsRuntime`). |
| `Runtime/Elements/AnimatedFoldout/Resources/Style/*.sass`, `elements/*.sass` | Sass sources of the `.uss` file, which is kept. They need an external Sass compiler and are not Unity assets. |
| `Samples/**` (`Example_Scene.unity`, `Objects/*.prefab/.asset`, `Scripts/**`) | Example scene and inspector example built on the excluded editor generator. Replaced by `Samples~/Animation`. |
| `package.json`, `README.md`, `media/**`, `.github/`, `Packages/`, `ProjectSettings/`, `SelectionGroups/`, `UserSettings/`, `removed-from.manifest.json`, `.gitignore` | Upstream project files. The README content is reflected in `Documentation~/Animation/README.md`; the `media/` GIFs and video are not needed. |

No editor code was kept, so `Editor/Animation/` contains nothing.

## Local modifications

Every change is marked in the source with a comment that starts with `FutureDigitalPlus:`.

| File | Change | Reason |
|---|---|---|
| `FutureDigitalPlus.UIKit.Animation.asmdef` | Rewritten: `references` emptied, `versionDefines` emptied; name and `rootNamespace` renamed by the script, rest as upstream. | The referenced GUIDs and the three defines belong to packages that are not part of this kit (see above). |
| `Animations/AnimationExtensions.cs`, `AnimationSequences.cs`, `ContinuousAnimations.cs`, `HoverAnimations.cs`, `VisualElementBaseAnimation.cs`, `Elements/AnimatedFoldout/UIElementExpander.cs`, `Elements/AnimatedLabel/AnimatedLabel.cs`, `Elements/BetterTextField/MaskedInputField.cs`, `Scripts/Extensions/ParseElements.cs`, `Scripts/Extensions/VisualElementEvents/VisualElementEvents.cs` | Whole-file `#if UNITY_EDITOR` guard (and its `#endif`) removed. | Upstream compiled the toolkit for the editor only. With the guard the animations do not exist in a player build. Their code only uses runtime UI Toolkit APIs (checked by compiling for a player target without `UnityEditor`). |
| `Animations/VisualElementBaseAnimation.cs` | Removed a stray `Debug.Log` of start and end width in `Animate<TValue,TStyle>`. | It logged on every call. |
| `Elements/AnimatedFoldout/UIElementExpander.cs` | Removed `using JetBrains.Annotations;` and the `[CanBeNull]` attribute. | IDE-only annotation, not available in every Unity install. No behaviour change. |
| `Elements/AnimatedFoldout/AnimatedFoldout.cs` | `using UnityEditor` removed; class made `partial` with `[UxmlElement]`, `text` and `value` marked `[UxmlAttribute]`, the `UxmlFactory` / `UxmlTraits` nested classes removed; `Expand(bool, bool)` no longer defers through `EditorApplication.delayCall`; the unused field `isAnimating` removed. | `UxmlFactory` and `UxmlTraits` are obsolete in Unity 6 (compiler warning CS0618). `delayCall` exists only in the editor. The field produced warning CS0169. **Behaviour difference:** a `<AnimatedFoldout>` in UXML without a `value` attribute is now collapsed; the old traits defaulted it to expanded. `Expand()` now acts immediately instead of on the next editor tick. |
| `Elements/AnimatedLabel/AnimatedLabel.cs` | Made `partial` with `[UxmlElement]`, removed the `UxmlFactory` / `UxmlTraits` classes; stylesheet is loaded with `Resources.Load<StyleSheet>("FutureDigitalPlus/UIKit/Animation/Styles/AnimatedLabel")` instead of `AssetFileExtensions.GetStyleSheet`. | Obsolete UXML API; `AssetDatabase` is editor only. `AnimatedLabel.uss` moved to `Elements/AnimatedLabel/Resources/FutureDigitalPlus/UIKit/Animation/Styles/`. |
| `Elements/BetterTextField/MaskedInputField.cs` | Made `partial` with `[UxmlElement]`, `Placeholder` marked `[UxmlAttribute("placeholder")]`, `UxmlFactory` / `UxmlTraits` and `[UsedImplicitly]` removed; stylesheet loaded with `Resources.Load<StyleSheet>("FutureDigitalPlus/UIKit/Animation/Styles/MaskedInputField")`; font comes from the new `public static Font MonoFont` (null: default font) instead of `AssetFileExtensions.GetFont("SourceCodePro-Medium")`. | Obsolete UXML API; `AssetDatabase` is editor only; the font files are not redistributable here. `MaskedInputField.uss` moved to `Elements/BetterTextField/Resources/Style/`. |
| `Scripts/Extensions/ParseElements.cs` | Removed `using JetBrains.Annotations;`, `using UnityEditor;` and `[UsedImplicitly]`; `ToStyleBackground` no longer calls `AssetDatabase.LoadAssetAtPath<Sprite>` (it always takes the "asset not found" branch). | `AssetDatabase` is editor only. Only the USS-string parser's `url(...)` background support is affected; `TryParseFloat` and `ToStyleInt`, which the animations use, are unchanged. |
| `Scripts/Extensions/Elements/VisualElementExtension.cs` | `using UnityEditor` / `UnityEditor.UIElements` and the `USING_HYBRID` / `USING_ADDRESSABLES` usings removed. The upstream `#if UNITY_EDITOR` block that covered `SetStyleValue` through the end of the class (including `CreateStyleData`, `FromStyleData`, `CopyElement`, `Set`, `AssignTo`, `AddRange`) is unguarded. `CreateEnumField` unguarded. **Removed:** `Create(this Image, UnityObject, out Image, name)`, `RegisterValue` for `ObjectField`, `ColorField`, `LayerMaskField` and `AssetReference`, `TryGetPreview` (2 overloads) and `GetAssetPreview`. In `Set<T>(...)` the `background_image` branch (`AssetDatabase`) is removed and the parameter has no effect. `Colors.DolphinGray` replaced by an inline `Color32(130,142,132)`. | `CreateStyleData` / `FromStyleData` are needed by `AnimCharacterSequence`. The removed members use `AssetPreview`, `AssetDatabase`, `UnityEditor.UIElements` types or `AssetReference.editorAsset`, which exist only in the editor. `Colors` is excluded (see above). |
| `Animations/Easy.cs` | Rewritten (header comment says so). The 29 standard curves (`Step`, `Linear`, `InSine` ... `InOutCirc`, `InPower`/`OutPower`/`InOutPower`, the two-argument `InBack`/`OutBack`/`InOutBack`) are now one-line delegates to Unity's public `UnityEngine.UIElements.Experimental.Easing` (same names, same signatures, same values). `OutExpo` and `EaseInOutQuint`, the upstream author's own curves, are kept as written; XML docs added. | Upstream's file was marked "From com.unity.ui" and held decompiled code of Unity's own `Easing` class (see "Provenance notes"), which the upstream MIT notice cannot license. Checked: the public `Easing` members compile against the 6000.3.25f1 assemblies. |
| `Animations/VisualElementBaseAnimation.cs`, `Animations/AnimationSequences.cs` | Every duration handed to `experimental.animation.Start` (28 call sites in the first file, 3 in the second) goes through `EatkMotion.Scale(...)`. A positive request never becomes 0 (it becomes 1 ms, which finishes on the next tick) because a zero-length value animation divides by its duration. | Reduced motion for the animations the module starts. Not covered: `Elements/AnimatedFoldout/UIElementExpander.cs` calls `experimental.animation` itself. |
| `Elements/BetterTextField/MaskedInputField.cs` | The caret is placed on `PointerUpEvent` instead of `MouseUpEvent`. | Pointer events are raised for finger and pen as well; mouse events need not be. |
| `Enhancements/Tasks/WaitUntil.cs` | `Wait.While` and `Wait.Until` rewritten: one private main-thread loop (`Time.realtimeSinceStartupAsDouble`, `await Awaitable.NextFrameAsync()`), `TimeoutException` after a real-time timeout, `ArgumentNullException` for a null condition, XML docs. Return type `Task` and the parameters are unchanged; `frequency` is the minimum gap between checks. | Upstream ran the condition on a thread-pool thread (`Task.Run`) and delayed with `Task.Delay`: UnityEngine access in the condition is illegal there, and on WebGL (no threads, no timers) the task never completes. |
| `Scripts/Extensions/ParseElements.cs` (second change) | `float.Parse`, `int.Parse`, `float.TryParse` and `ToInt32` use `CultureInfo.InvariantCulture` (private helper `ParseFloat`); XML summary on `UIParser` says it is editor-time tooling. | The current culture read `"0.5"` as 5 in de-DE. `CreateVisualElement` reflects on the non-public `VisualElement.CreateUnityType`, so it is not a player feature. |
| `Elements/**/Resources/FutureDigitalPlus/UIKit/Animation/Styles/*.uss` | Moved (see Copied). Contents unchanged. | Runtime stylesheet lookup needs a `Resources` folder. |
| `Animations/VisualElementBaseAnimation.cs`, `Animations/AnimationExtensions.cs` | The 13 calls of `x.parent.schedule.Execute(...)` (the `*Delayed` animations, `Animate` with a value store, both `ExecuteIn` overloads) are `x.schedule.Execute(...)`. | Upstream used the parent's scheduler, so a delayed animation or `ExecuteIn` on an element that has no parent yet (just created, or removed by an earlier step) threw a `NullReferenceException`. Found by the adversarial API review. |

Unity 6 API breakages found: only the obsolete `UxmlFactory` / `UxmlTraits` pair (fixed above). `UnityEngine.UIElements.Experimental`
(`ValueAnimation<StyleValues>`, `StyleValues`, `experimental.animation.Start`) and the other UI Toolkit APIs the code uses are
unchanged in Unity 6000.3 and compile without warnings.

## Provenance notes (licence review)

Everything kept is covered by the upstream MIT licence except as follows.

- `Animations/Easy.cs` (resolved in the licence review of 2026-10-06): the upstream header says "From com.unity.ui", and the body was
  decompiler-style output (`(double)` casts, constants such as `0.363636374473572`) of Unity's `UnityEngine.UIElements.Experimental.Easing`.
  The equations are the standard Penner curves, but the file text derived from Unity's code, which an MIT notice from instance.id cannot
  license. The file now delegates to Unity's public `Easing` class (see the modifications table); only `EaseInOutQuint` and `OutExpo`,
  the upstream author's own, remain as code. Nothing of the decompiled text is left.
- `Elements/AnimatedFoldout/AnimatedFoldout.cs`: modelled on Unity's `Foldout` (same USS class names, same structure). The
  upstream `UxmlFactory`/`UxmlTraits` block that was decompiler output of Foldout's (it carried `// ISSUE: explicit constructor call`)
  is removed here (see the modifications table).
- Not bundled (licence not stated or different): the *Source Code Pro* font files and `Color/Colors.cs` (see Excluded).

## Resources paths (document, do not collide)

| Path | File |
|---|---|
| `FutureDigitalPlus/UIKit/Animation/Styles/AnimatedFoldoutStyle` | `Elements/AnimatedFoldout/Resources/FutureDigitalPlus/UIKit/Animation/Styles/AnimatedFoldoutStyle.uss` (upstream: `Style/AnimatedFoldoutStyle`) |
| `FutureDigitalPlus/UIKit/Animation/Styles/AnimatedLabel` | `Elements/AnimatedLabel/Resources/FutureDigitalPlus/UIKit/Animation/Styles/AnimatedLabel.uss` (upstream: `Style/AnimatedLabel`) |
| `FutureDigitalPlus/UIKit/Animation/Styles/MaskedInputField` | `Elements/BetterTextField/Resources/FutureDigitalPlus/UIKit/Animation/Styles/MaskedInputField.uss` (upstream: `Style/MaskedInputField`) |

## How this was checked

Static check at import: the sources compiled with Roslyn against the Unity 6000.3.25f1 managed assemblies, with and
without `UNITY_EDITOR`, 0 errors and 0 warnings.

Integration run (Unity 6000.3.25f1, clean scratch project, package in `testables`): full recompile with no warnings;
the 52 PlayMode tests in `Tests/Runtime/Animation` pass; the runtime assembly compiles for iOS, Android and macOS standalone
players; the sample runs. Test fixes made during integration: `AnimatedFoldout_DefaultsToCollapsed_AndHostsChildrenInContent`
asserted `child.parent`, which UI Toolkit reports as the logical parent (the foldout), and now asserts `child.hierarchy.parent`
(the content container). The later changes (input, platform fixes and the integration run) changed the module's sources only as listed in the modifications table (reduced-motion hook, `WaitUntil`, `ParseElements`, `MaskedInputField`, `Easy`).

<!-- rename-record:begin -->
## Rename record

Generated by `Tools~/rename/rename.py` (collision-proofing). Module: Animation. The upstream names survive only in this record.

| Assembly (old) | Assembly (new) |
|---|---|
| `FutureDigitalPlus.UIKit.Tests.Animation.Runtime` | `FutureDigitalPlus.UIKit.Animation.Tests` |
| `id.instance.elementanimationtoolkit` | `FutureDigitalPlus.UIKit.Animation` |

| Namespace (old) | Namespace (new) |
|---|---|
| `id.instance.elementanimationtoolkit` | `FutureDigitalPlus.UIKit.Animation` |
| `instance.id.EATK` | `FutureDigitalPlus.UIKit.Animation` |

| Resources path (old) | Resources path (new) |
|---|---|
| `Style/` | `FutureDigitalPlus/UIKit/Animation/Styles/` |
<!-- rename-record:end -->
