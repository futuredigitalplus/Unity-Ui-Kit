# Animation module

Chainable UI Toolkit animation extension methods and a few ready-made animated elements, for runtime UI and for editor UI.
It is the ElementAnimationToolkit by instance.id (MIT), version 0.1.93, imported unchanged in structure (namespaces and the assembly name renamed). What was
changed for Unity 6 and for runtime use is listed in [`Runtime/Animation/SOURCE.md`](../../Runtime/Animation/SOURCE.md).

| | |
|---|---|
| Assembly | `FutureDigitalPlus.UIKit.Animation` (upstream: `id.instance.elementanimationtoolkit`; auto-referenced, no references, no editor code) |
| Namespaces | `FutureDigitalPlus.UIKit.Animation` (animations, elements, tasks) and `FutureDigitalPlus.UIKit.Animation.Extensions` (helper extensions) |
| Package folder | `Runtime/Animation` |
| Needs | UI Toolkit only. It does not use the Design System module, UIComponents, or any game code |
| Sample | `Samples~/Animation` (import from Package Manager: *Animation Toolkit Demo*) |
| Tests | `Tests/Runtime/Animation` (assembly `FutureDigitalPlus.UIKit.Tests.Animation.Runtime`) |

## Contents

1. [Quick start](#quick-start)
2. [Things to know before you use it](#things-to-know-before-you-use-it)
3. [Base animations](#base-animations)
4. [Sequences](#sequences)
5. [Continuous animation](#continuous-animation)
6. [Hover animations](#hover-animations)
7. [Event helpers](#event-helpers)
8. [Scheduling helper](#scheduling-helper)
9. [Easing](#easing)
10. [Data stores](#data-stores)
11. [Elements](#elements)
12. [Task helpers](#task-helpers)
13. [Helper extensions](#helper-extensions)
14. [Resources paths used by this module](#resources-paths-used-by-this-module)
15. [What is not included](#what-is-not-included)

## Quick start

```csharp
using FutureDigitalPlus.UIKit.Animation;              // animations, elements, Easy, SingleTask ...
using FutureDigitalPlus.UIKit.Animation.Extensions;   // helper extensions, ColorUtil.FromHex ...
using UnityEngine;
using UnityEngine.UIElements;

public class Example : MonoBehaviour
{
    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        var bar = new VisualElement();
        bar.style.height = 8;
        bar.style.backgroundColor = Color.gray;
        root.Add(bar);                                   // add it to the tree first, most helpers need element.parent

        bar.AnimateWidth(0f, 300f, 600);                 // width 0 -> 300 px over 600 ms
        bar.AnimateBackgroundColor("#2F569C", "#D2A00C", 800);
        bar.AnimateOpacity(0f, 1f, 400, () => Debug.Log("faded in"), Easy.OutQuad);
    }
}
```

All animations return the `ValueAnimation<StyleValues>` (or `ValueAnimation<float>`) object of Unity's own
`experimental.animation` API, so you can `Stop()`, `Start()`, `KeepAlive()` or `Recycle()` it yourself.
Durations and delays are in milliseconds. The default easing of the base animations is `Easy.EaseInOutQuint`.

## Things to know before you use it

These come from the upstream code and were kept on purpose (behaviour is unchanged). Details are in the doc comments of each method.

* **Add the element to a parent first.** Every `...Delayed` animation, `Animate(valueStore)`, `ExecuteIn` and the
  hover helpers schedule work on `element.parent`. They throw a `NullReferenceException` for an element that has no parent yet.
* **Opacity 0 is replaced by 0.0001.** `Zero()`, `SetOpacity(0)`, the opacity animations and `AnimValueStore.initial/final`
  turn a value of exactly `0` into `0.0001f`. This is the toolkit's workaround for opacity "bugging out" at 0.
  The element is never exactly 0 after such a call.
* **`AnimateOpacity` can return `null`.** When the element already has the end opacity inline and `reanimate` is `false`,
  nothing is started and `null` is returned.
* **Animations start when they are created.** `HoverWidth`, `HoverHeight` and `HoverBackground(animate: true)` create their
  `ValueAnimation` objects when you register the hover, and Unity starts an animation as soon as it is created.
  Expect one animation pass at registration.
* **Hover helpers call `StopPropagation()`** on the mouse events unless a `stopPropagation` parameter says otherwise.
* **Unregistering.** The `unregister` parameter of `HoverColor` and `HoverBorderPulseUnregister` build new lambdas, which cannot
  remove the old ones. To remove a hover, use the callbacks that `HoverBorderPulse`, `HoverWidth` and `HoverHeight` return
  in `AnimatedItems.EventCallbacks` with `element.UnregisterCallback(callback, true)`.
* **`ShowForDuration` ignores its `callback` argument.** The parameter exists but is never invoked.
* **`AnimFadeInSequence` remembers its times.** Non-zero `fadeIn`, `display`, `fadeOut` values are stored in static fields and
  become the defaults for later calls that pass `0`.
* **`ApplyPrivacyMask(prefix, suffix)` masks the characters from `prefix` up to and including index `suffix`.**
  `"0123456789".ApplyPrivacyMask(0, 4)` returns `("0123456789", "*****56789")`. The upstream summary text says otherwise.
* **Colour helpers.** The old `GetColor.FromHex` is gone upstream. Use `"#RRGGBB".FromHex()` or `ColorUtil.FromHex("#RRGGBB")`
  (namespace `FutureDigitalPlus.UIKit.Animation.Extensions`) and `EnsureHex()`.
* **Name clashes.** `FutureDigitalPlus.UIKit.Animation.Extensions` adds many extension methods to `VisualElement` (`SetDisplay`, `SetOpacity`,
  `Create`, `ToUSS`, ...). Add `using FutureDigitalPlus.UIKit.Animation.Extensions;` only in files that use them.

## Base animations

Class `VisualElementBaseAnimation`, namespace `FutureDigitalPlus.UIKit.Animation`. Each method has overloads for `Color`/`float` values, for
hex strings (`"A4B5C6"` or `"#A4B5C6"`) where it takes colours, and for the data stores described below.

| Method | Animates | Main arguments |
|---|---|---|
| `AnimateBackgroundColor(start, end, durationMs, callback, easing)` | `backgroundColor` | `Color`, hex `string`, or `AnimValueStore<Color>` with `easing` and `reverse` |
| `AnimateBackgroundColorDelayed(start, end, durationMs, delayMs, callback, easing)` | `backgroundColor` after a delay | |
| `AnimateBorderColor(...)` | all four border colours | `Color`, hex `string`, `AnimColorStore`, or `AnimValueStore<Color>` |
| `AnimateColor(...)` | text `color` | `Color`, hex `string`, `AnimColorStore`, or `AnimValueStore<Color>` |
| `AnimateImageTintColor(...)` | `unityBackgroundImageTintColor` | `Color` or hex `string` |
| `AnimateOpacity(start, end, durationMs, callback, easing, reanimate)` | `opacity` | also `AnimValueStore` with `reanimate`, `reverse` |
| `AnimateOpacityDelayed(start, end, durationMs, delayMs, callback, easing)` | `opacity` after a delay | also `AnimValueStore` |
| `AnimateDisplay(setDisplay, durationMs, callback, easing)` | shows an element and fades it in, or fades it out and then sets `display: none` | |
| `AnimateWidth(start, end, durationMs, callback, easing)` | `width` in pixels | also `AnimValueStore` |
| `AnimateWidthDelayed(...)` | `width` after a delay | also `AnimValueStore` |
| `AnimateHeight(start, end, durationMs, callback, easing)` | `height` in pixels | also `AnimValueStore` with `durationOverride`, `delayOverride` |
| `AnimateHeightDelayed(...)` | `height` after a delay | also `AnimValueStore` |
| `AnimateValue(start, end, durationMs, valueChange, callback, easing)` | any float, you apply it in `valueChange(element, value)` | also `AnimValueStore` |
| `AnimateSlider(slider, start, end, durationMs, valueChange, callback, easing)` | a float for a `Slider` | |
| `Animate<T>(AnimValueStore<T>, easing, reverse, durationOverride, delayOverride, callback)` | the property named by `valueStore.element` (`AnimateElement`): height, width, opacity, colours | needs a parent |

```csharp
label.AnimateColor("#BABABA", "#2F569C", 250);
panel.AnimateOpacityDelayed(0f, 1f, 300, 100);                     // start after 100 ms
slider.AnimateSlider(0f, 1f, 500, (el, v) => ((Slider)el).SetValueWithoutNotify(v));
```

## Sequences

Class `AnimationSequences`.

| Method | What it does |
|---|---|
| `AnimFadeInSequence(Label, newText, textColor, textDefaultColor, fadeIn, display, fadeOut, easing)` | Fades the label out, swaps in `newText` and `textColor`, fades in, holds for `display` ms, fades out, restores the original text and colour and fades back in. Also available for any `VisualElement` without text. |
| `AnimCharacterSequence(AnimatedLabel, color1, color2, cascadeMs, durationMs, reverse, callback, flexBasis)` | Splits the text of an `AnimatedLabel` into one label per character and fades each character from `color1` to `color2` and back, each one starting `cascadeMs` after the previous. Returns the list of animations. |
| `ShowForDuration(element, fadeDurationMs, displayDurationMs, callback, easing)` | Fades a (possibly hidden) element in, keeps it for `displayDurationMs`, fades it out over 500 ms and restores its original `display` state. Returns the animations of each phase. The `callback` is not invoked (see above). |

```csharp
toast.ShowForDuration(250, 1500);
status.AnimFadeInSequence("Saved", ColorUtil.FromHex("#607FAE"), ColorUtil.FromHex("#BABABA"), 500f, 2000f, 500f);
```

## Continuous animation

Class `ContinuousAnimations`.

`AnimBorderPulse(element, color1, color2, original, color1DurationMs, color2DurationMs, addBorder, borderStartEndWidth, callback, borderSelection, repeatedAnim, delayBetween)`
pulses the border colour between two colours until you stop it. It returns an `IVisualElementScheduledItem`; call `Pause()` on it
to stop; the border is reset and `callback` is invoked when the pulse that is currently running finishes. `borderSelection` is a `Vector4` that picks the sides (x left, y top, z right, w bottom).
There is also an overload that takes an `AnimValueStore<Color>`.

```csharp
var pulse = box.AnimBorderPulse(Color.gray, Color.cyan, original: Color.gray, color1DurationMs: 500, color2DurationMs: 500);
// later
pulse.Pause();
```

## Hover animations

Class `HoverAnimations`. These work like CSS `:hover` but keep working when you set colours from code.

| Method | Effect |
|---|---|
| `HoverColor(original, hoverColor, condition, conditionElement, animate, unregister)` | Text colour on hover. |
| `HoverBackground(original, hoverColor, condition, conditionElement, animate, animDuration)` | Background colour on hover. |
| `HoverBorder(original, hoverColor, addBorder, borderStartEndWidth)` | Border colour on hover. |
| `HoverBorderPulse(pulseStartColor, pulseEndColor, original, addBorder, borderStartEndWidth, colorDuration, includeChildren, stopPropagation, useTrickleDown, animatedItems, params animRunCheck)` | Pulsing border while hovered. Returns `AnimatedItems<MouseOverEvent, MouseOutEvent>`. |
| `HoverBorderPulseUnregister(...)` | See the note on unregistering above. |
| `HoverOpacity(startValue, endValue, animDuration)` | Opacity animation on enter and leave. |
| `HoverWidth(initialWidth, desiredWidth, duration, hoverCallback, leaveCallback, afterAnimation, includeChildren, stopPropagation, useTrickleDown)` | Width animation on enter and leave. Returns `AnimatedItems`. |
| `HoverHeight(initialHeight, desiredHeight, duration, hoverCallback, leaveCallback, afterAnimation)` | Height animation on enter and leave. Returns `AnimatedItems`. |
| `HoverToolTip(callback)` | Calls `callback` when the pointer enters the element (to show a tooltip in an external label). |
| `HoverAction(mouseInAction, mouseOutAction, extendedTargets)` | Runs your actions when the pointer enters and leaves the element. The elements in `extendedTargets` trigger the same actions, and leaving the main element towards one of them does not call `mouseOutAction`. |

`condition` is a `Func<T, bool>`; when it returns `true` the hover effect is skipped (for example for a selected row).

```csharp
row.HoverBackground(new StyleColor(Color.clear), new Color(0.2f, 0.35f, 0.6f), animate: true);
var events = box.HoverBorderPulse(Color.gray, Color.cyan, original: Color.gray, includeChildren: true);
box.UnregisterCallback(events.EventCallbacks.mouseOverEvent, true);
box.UnregisterCallback(events.EventCallbacks.mouseOutEvent, true);
```

## Event helpers

Class `VisualElementEvents` (namespace `FutureDigitalPlus.UIKit.Animation`).

| Method | Effect |
|---|---|
| `RegisterCallback<TEvent>(callback, includeChildren, useTrickleDown)` | Registers on the element and all descendants. Returns the element. |
| `UnregisterCallback<TEvent>(callback, includeChildren, useTrickleDown)` | Removes it again. |
| `MouseDownBackgroundColor(downColor)` | Background colour while the mouse button is down (works for `Button`, too). |
| `MouseDownImageColor(downColor)` | Same for the image tint. |

## Scheduling helper

Class `AnimationExtensions`: `element.ExecuteIn(action, delayMs)` and `action.ExecuteIn(element, delayMs)` run an action after a delay
using the scheduler of `element.parent`.

## Easing

`Easy` holds the easing functions (`float f(float t)`, `t` from 0 to 1): `Step`, `Linear`, `InSine`, `OutSine`, `InOutSine`, `InQuad`,
`OutQuad`, `InOutQuad`, `InCubic`, `OutCubic`, `InOutCubic`, `InPower`/`OutPower`/`InOutPower(t, power)`, `OutExpo`, `InBounce`,
`OutBounce`, `InOutBounce`, `InElastic`, `OutElastic`, `InOutElastic`, `InBack`, `OutBack`, `InOutBack` (also with an overshoot
argument `s`), `InCirc`, `OutCirc`, `InOutCirc` and `EaseInOutQuint` (the default of the base animations).
The `Easing` enum names the same curves for serialized fields, and `GetEasing.Get(Easing)` converts an enum value to the function.

## Data stores

Serializable bundles of start value, end value, duration, delay, easing and callbacks, so an animation can be defined once and
played forwards or in `reverse`.

| Type | Holds |
|---|---|
| `AnimValueStore` | floats `initial`/`final` (0 becomes 0.0001), `duration`, `delay`, `easing`, `callback`, `reverseCallback`, `userData` |
| `AnimValueStore<T>` | the same for a struct value such as `Color`, plus `AnimateElement element` for `Animate<T>` |
| `AnimValueStore<TValue, TStyle>` | initial and final of different types |
| `AnimColorStore` | initial and final `Color` (or hex strings), `duration`, `delay`, callbacks |
| `AnimTime` | `duration`, `delay` and an `Easing`; `Easings` returns the function |
| `AnimateElement` | enum of style properties that `Animate<T>` can drive |
| `AnimatedItems`, `AnimatedItems<TEvent>`, `AnimatedItems<TOver, TOut>` | what the hover helpers return: the element, the `ValueAnimation` list, the registered callbacks and an `AllowRun` switch to pause the effect |
| `VisualElementStyleStore` | a snapshot of an element's style; `element.CreateStyleData()` and `store.FromStyleData(target)` copy styles between elements |

## Elements

### AnimatedFoldout

A foldout whose content opens and closes with an animated height (it uses the `Expander` class, which you can also use on its own).

```csharp
var foldout = new AnimatedFoldout { text = "Details" };            // collapsed
foldout.Add(new Label("content"));                                   // goes into the content container
foldout.Expand(true);                                                // expand from code, Expand(true, animate: false) for no animation
foldout.RegisterValueChangedCallback(evt => Debug.Log(evt.newValue));
```

* Constructors: `AnimatedFoldout()`, `AnimatedFoldout(bool startExpanded)`, `AnimatedFoldout(VisualElement headerElement, FlexDirection, Align)` and
  the overload with `replaceHeader`.
* Properties: `text`, `value`, `toggleAsHeader` (header line under the toggle), `startExpand`, `disableAnimation`,
  `OnComplete` (called when the animation ends), `stateChange`, `expander`.
* UXML: `<FutureDigitalPlus.UIKit.Animation.AnimatedFoldout text="Details" value="true"/>`. A missing `value` attribute means collapsed
  (the old UxmlTraits version defaulted to expanded).
* Styling: stylesheet `FutureDigitalPlus/UIKit/Animation/Styles/AnimatedFoldoutStyle` is added automatically. USS classes: `unity-foldout`, `unity-foldout__toggle`,
  `unity-foldout__content`, `expandToggle`, `expandContainerItem`, `headerBorderLine`, `headerBorderLineExpanded`, `animatedFoldoutLabel`.
  The header line width comes from the custom USS property `--fdp-headerBorderWidth`.

### AnimatedLabel

A container with one inner `Label`, used as the target of `AnimCharacterSequence`. It forwards `text`, class-list calls and
`RegisterCallback` to the inner label. UXML: `<FutureDigitalPlus.UIKit.Animation.AnimatedLabel/>`. Stylesheet `FutureDigitalPlus/UIKit/Animation/Styles/AnimatedLabel`.

### MaskedInputField

A `TextField` that shows a privacy-masked copy of its value (`prefixNum`, `suffixNum`, `usePrivacyMask`) and a placeholder.
Properties: `Placeholder` (UXML attribute `placeholder`), `maskedText`, `OnValueChangedHandler`, `ApplyPrivacyMask(string)`.
Stylesheet `FutureDigitalPlus/UIKit/Animation/Styles/MaskedInputField`.

The upstream project styled this field with the font *Source Code Pro*, which is not part of this package. Assign any monospaced font
before you create fields to get that look, otherwise the default font is used:

```csharp
MaskedInputField.MonoFont = myMonoFont;     // optional
```

## Task helpers

* `SingleTask` (implements `ITaskSequence`): runs a list of `Action<Action>` steps one after another; each step calls the action it
  receives when it is done. `Add(step)`, `Run(onCompleted)`.
* `Wait.While(condition, frequency, timeout)` and `Wait.Until(condition, frequency, timeout)`: awaitable polling helpers that throw
  `TimeoutException` when the timeout (ms, `-1` = none) is hit.

```csharp
var steps = new SingleTask();
steps.Add(next => element.AnimateOpacity(0f, 1f, 300, next));
steps.Add(next => element.AnimateWidth(0f, 200f, 300, next));
steps.Run(() => Debug.Log("done"));
```

## Helper extensions

Static class `VisualElementExtension` (namespace `FutureDigitalPlus.UIKit.Animation.Extensions`). The ones used by the animations and most useful on their own:

| Group | Methods |
|---|---|
| Building | `Create(out var, name)`, `Create(name, className)`, `CreateRow`, `CreateColumn`, `CreateWithLabel`, `CreateWithHeader`, `HR`, `Root`, `BuildHeader`, `ToUSS(params classNames)`, `NameAsUSS`, `AddAll`, `AddRange`, `AssignTo`, `SetParent` |
| Style setters | `SetColor`, `SetBackgroundColor`, `SetBackgroundImageColor`, `SetBorderWidth`, `SetBorderRadius`, `SetBorderColor`, `SetMargin`, `SetPadding`, `SetAlignItems`, `SetJustify`, `SetText`, `SetDisplay`, `GetDisplay`, `IfDisplay`, `SetOpacity`, `Set(...)` |
| Hierarchy | `GetFirstAncestorWithClass`, `GetFirstAncestorOfType`, `GetFirstParentOfType`, `GetParentsOfType`, `GetSiblingIndex` |
| Behaviour | `OpenURL(url)`, `AutoToggleAfter(toggle, ms, ...)`, `AutoToggleAfterChange(...)`, `RegisterAnyChangeEvent`, `RegisterValue(...)`, `SelectRangeDelayed` |
| Values | `Zero()` (returns 0.0001), `ApplyPrivacyMask(prefix, suffix)`, `CreateStyleData()`, `FromStyleData()`, `CopyElement()` |

`ColorUtil` (`FromHex`, `EnsureHex`), `CollectionExtension` (`ForEachR`, `forEach`, `TryAdd`, `TryGetVal`, ...) and `UIParser` (a small
USS and UXML string parser; `TryParseFloat`, `ToStyleInt`, `ToStyleColor`, ...) are in the same namespace.

## Resources paths used by this module

The elements load their stylesheets with `Resources.Load<StyleSheet>`. Do not create files with these paths in your own `Resources` folders.

| Path | Used by |
|---|---|
| `FutureDigitalPlus/UIKit/Animation/Styles/AnimatedFoldoutStyle` | `AnimatedFoldout` |
| `FutureDigitalPlus/UIKit/Animation/Styles/AnimatedLabel` | `AnimatedLabel` |
| `FutureDigitalPlus/UIKit/Animation/Styles/MaskedInputField` | `MaskedInputField` |

## What is not included

Upstream also contains an editor window with a feature tour (*Tools > instance.id > Element Animation Toolkit*), editor-only tooling and an
example scene. They depend on editor APIs, on assets that are not redistributable here, or on code that was dropped, and are not part of this package.
The exact list, with reasons, is in `Runtime/Animation/SOURCE.md`. `Samples~/Animation` is a small replacement example written for this package.
