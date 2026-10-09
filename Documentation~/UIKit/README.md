# UIKit (facade)

New code written by Future Digital Plus. It adds no features of its own; it wires the DesignSystem and Animation
modules together so a screen starts with one component. Assembly `FutureDigitalPlus.UIKit`
(references `FutureDigitalPlus.UIKit.DesignSystem` and `FutureDigitalPlus.UIKit.Animation`), namespace `FutureDigitalPlus.UIKit`.
It does not reference Components or Extras, so it does not run the Roslyn generators.

| Type | What it is |
|---|---|
| `UIKitHost` | MonoBehaviour next to a `UIDocument`. Installs the kit on the document root whenever the tree is (re)built; `Theme` property swaps the theme. Add via *Add Component > FDP UI Kit > UI Kit Host*. |
| `UIKitInstaller.Install(root, options)` | The same thing from code, for any `VisualElement`. Returns a `UIKitInstallation` (null for a null root). |
| `UIKitInstaller.CreatePanelSettings(themeStyleSheet, referenceResolution, match)` | A `PanelSettings` that scales with the screen (default 1080 x 1920, match 0.5). The caller owns it. |
| `UIKitInstallation` | Handle: `SetTheme(theme)`, `Refresh()`, `IsMobile`, `AppliedInsets`, `Dispose()` (takes everything off again). |
| `UIKitOptions` | Serializable settings: `Theme`, `InstallBaseStyles`, `Mobile` (`Auto`/`Off`/`On`), `MobileBreakpoint` (768), `ApplySafeArea`, `EnsureBehaviours`. |
| `SafeAreaInsets.FromScreen(safeArea, screenSize, panelSize)` | Pure function: `Screen.safeArea`-style rectangle in pixels to insets in panel units. |
| `UIKitMotion.FadeIn / FadeOut` | Two extension methods over the Animation module (`AnimateOpacity`). A new fade on an element replaces a running one (the older one stops, its `onDone` does not run, the new one starts from the current opacity), so a quick FadeOut then FadeIn ends visible. Everything else in the Animation module is called directly. |

## What `Install` does

1. **Base styles.** Adds `FutureDigitalPlus/UIKit/DesignSystem/Styles/DesignSystem` (master sheet, imports all component sheets and the
   `:root` tokens) to the **panel root**, the parent of the document root. Shared between several documents on one
   panel (reference counted). A sheet the host already added itself is left alone. A root that is not attached yet gets the
   sheet on itself first and moves to the panel root on attach.
2. **Theme.** `ThemeRuntime.Apply(root, options.Theme)` on the document root: the baked theme stylesheet and, for a
   class-scoped theme, its class (`theme-light`). `null` means the base (dark) tokens. A theme wins over the base tokens
   only from a descendant of the base sheet's element, which is why step 1 puts the base on the parent.
3. **Mobile class.** `fdp-input-touch` on the root (the design system's touch-size tier, `Mobile.uss`): in `Auto` mode when
   `Application.isMobilePlatform` or the panel is narrower than `MobileBreakpoint`, re-evaluated on every geometry change.
4. **Safe area.** Root padding from `Screen.safeArea`, converted to panel units with the panel size (so it is right with
   scale-with-screen panels), re-checked on geometry changes and every 500 ms (rotation, fold). `Dispose` restores the root's previous inline padding.
   While installed, the safe area overwrites root padding of your own: put that padding on an inner element, or turn this off.
   When a `UiShell` (Foundation) is added under the root, the facade gives its padding back and leaves the safe area to the shell, whose top bars, dialogs and toasts pad themselves and whose scrims should reach
   the screen edges; the only link between the two modules is the shell's root class `fdp-shell`.
5. **Behaviours.** The design system's runtime behaviours (`EnsureAll`: toggle knobs, tabs, drag and drop, dropdown placement) run once at install and then every 250 ms
   (`EnsureBehaviours`), so a `ds-toggle` in a screen that a shell builds after start gets its knob without a call. Each helper does nothing once its work is done. The scripting define
   `FDP_UIKIT_AUTO_ATTACH` (upstream's auto-attach to every `UIDocument`, off by default) is a different route and is not needed. Without the host, call
   `DesignSystemBehaviourBase<UIDocument>.EnsureAll(root)` yourself after you add elements.

## Example

```csharp
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Data;   // ThemeData
using FutureDigitalPlus.UIKit;           // UIKitInstaller, UIKitOptions, MobileMode, UIKitMotion.FadeIn
// (an asmdef that uses this needs references to "FutureDigitalPlus.UIKit" and "FutureDigitalPlus.UIKit.DesignSystem")

var settings = UIKitInstaller.CreatePanelSettings(myRuntimeTheme);   // a ThemeStyleSheet (.tss) that imports unity-theme://default; lightTheme below is a ThemeData
var kit = UIKitInstaller.Install(document.rootVisualElement, new UIKitOptions
{
    Theme = lightTheme,
    Mobile = MobileMode.Auto,
});
kit.SetTheme(null);           // back to dark
screenElement.FadeIn(250);    // Animation module
kit.Dispose();                // take it all off
```

### Without a scene setup: host from code

A `UIKitHost` installs when its `UIDocument` has a root, so the order of `AddComponent` does not matter, and it re-installs
if the document rebuilds its tree. To get the options in place before the first install, configure an inactive object:

```csharp
var go = new GameObject("UI");
go.SetActive(false);
var document = go.AddComponent<UIDocument>();
document.panelSettings = UIKitInstaller.CreatePanelSettings(myRuntimeTheme);
var host = go.AddComponent<UIKitHost>();
host.Options.Theme = lightTheme;      // or host.Theme = ... later, at runtime
go.SetActive(true);
```

### Things to know

- The facade is for a document that has a themed screen and no shell. With a `UiShell` (Foundation) use `UIShellHost` ([../Bootstrap/README.md](../Bootstrap/README.md)) instead of `UIKitHost`. If both are on one document anyway, the facade steps aside as soon as an element with the shell's root class `fdp-shell` shows up under its root: it gives its safe-area padding back (the shell pads its top bars, dialogs and toasts itself, so the notch is not applied twice) and stops writing `fdp-input-touch` (the input profile owns it). The two modules share nothing but that class name.
- The device safe area belongs to a screen-space panel on the main display. For a panel that renders into a `RenderTexture` (an in-game monitor, a minimap), a world-space panel (6000.5 and later) or a panel on another display, the default `SafeAreaSource` reports the whole screen, so nothing is padded. A `SafeAreaSource` you assign yourself is applied to every panel.
- The theme is applied to the document root, the base sheet to its parent (the panel root). Put `ds-root` on your top
  element and build the screen inside it; elements you add to the document root directly (for example a tooltip) do not
  get the `ds-root` background, but they do inherit the tokens.
- `UIKitInstaller.Install` on the root of a `UIDocument` owned by another script is fine; call `Dispose()` when you destroy it.
  A root has at most one installation: installing again (a `UIKitHost` plus a manual `Install`, or a second call after a scene change) disposes the previous installation first, so the two never fight over the theme, the padding and the base sheet. After `Dispose`, `SetTheme` only updates `Options.Theme`; it no longer dresses the root.
  `UIKitHost` does that in `OnDisable`.
- `ApplySafeArea` overwrites the root's inline padding while installed (`Dispose` restores what was there). Padding you want on top of the safe area belongs on an inner element.
- `UIKitMotion.FadeIn` sets `display: flex` first, `FadeOut` hides with `display: none` unless `hide: false`.

## Tests

`Tests/Runtime/UIKit` (PlayMode, on a RenderTexture panel so results do not depend on the Game view):
installer, host, safe area, motion, and `CrossModuleTests`, one screen that uses all four modules (a `UIComponent` with
`[Layout]` and `[Query]`, the facade theme and mobile tier, an Animation fade and an Extras tooltip).
`Tests/Editor/UIKit` has three EditMode tests that need no panel.
