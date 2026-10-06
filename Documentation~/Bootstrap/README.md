# Bootstrap: one component that runs the kit

`FutureDigitalPlus.UIKit.Bootstrap` holds `UIShellHost`, a MonoBehaviour for a game with more than one screen, and `InputProfileModeSource`, the adapter it needs. The kit's modules do not reference each other, so a
game used to write the joins itself (input device to shell, back key to shell, accessibility to the animation hook, language to root classes). `UIShellHost` is those joins, written once.
It is its own assembly so that the facade (`UIKit`) and the modules stay free of each other; add this assembly only when you want the host.

| Assembly | References | Auto-referenced |
|---|---|---|
| `FutureDigitalPlus.UIKit.Bootstrap` | DesignSystem, Animation, Foundation.Core, Foundation, Interaction.Core, Interaction, Language | yes |

## Use

1. A scene with a `UIDocument` whose Panel Settings has a Theme Style Sheet ([GettingStarted.md](../GettingStarted.md), step 1).
2. Add **Future Digital Plus UI Kit > UI Shell Host** to the same GameObject.
3. Register your screens and boot the first one when the host says it is ready:

```csharp
using FutureDigitalPlus.UIKit.Bootstrap;
using FutureDigitalPlus.UIKit.Foundation;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class Game : MonoBehaviour
{
    sealed class StartScreen : UiScreen
    {
        public override string UxmlName => null;                 // built in code; return "Start" for Resources/Screens/Start.uxml
        protected override void OnBuild() => Root.Add(new Label("Hello"));
    }

    void Awake() => GetComponent<UIShellHost>().Ready += OnReady;

    void OnReady(UIShellHost host)
    {
        host.Shell.Catalog.Register("Start", () => new StartScreen());
        host.Shell.Boot("Start");
    }
}
```

## What the host does

On `Start` (and again whenever the component is disabled and enabled):

1. Installs the design system sheet and the Language sheet on the panel root and the Interaction sheet on the document root (the shell adds the Foundation sheets itself).
2. Registers the built-in `"dark"` and `"light"` themes (unless you registered a theme of that name on `Themes` before `Start`) and wears `Start Theme`.
3. Binds `InputProfile` (the last-used device as `fdp-input-*` classes) and `AccessibilityApplier` (`fdp-reduce-motion`, `fdp-text-scale-*`, `fdp-bold-text`, `fdp-high-contrast`); copies the accessibility result into `UiAccessibility`,
   switches the high contrast theme through `ThemeManager.HighContrast` and points `EatkMotion.Scaler` at the reduced-motion scale, so animations of the Animation module obey it.
4. Binds `LocaleClassApplier` (`fdp-lang-<code>`, `fdp-rtl`).
5. Builds a `UiShell` with the input mode from `InputProfileModeSource`, texts from `UiLocalization.Default` and screens from `Resources/<Screens Folder>/<UxmlName>.uxml`; calls your `ConfigureShell` first.
6. Sends Escape, gamepad B, the Android back button and `NavigationCancelEvent` to `UiShell.HandleBackPress`.
7. Raises `Ready`. Every frame it calls `Shell.Tick()`; it forwards application focus and pause. On disable it undoes all of it (the shell is disposed, the bindings released, `EatkMotion.Scaler` cleared if it is still ours).

| Member | |
|---|---|
| `Ready` (event), `IsReady` | the kit is running; register screens and boot here |
| `Shell`, `Root` | the shell and the document root (null while not ready) |
| `Profile`, `Accessibility` | the input profile and the accessibility applier |
| `Themes` | a `ThemeManager`; register a brand theme before `Start`, switch with `Themes.Set("brand")` |
| `ConfigureShell` | `Action<UiShellOptions>`, set it in `Awake`: state store, catalog, toast placement, minimum safe-area margin for a TV, ... |
| Inspector: `Start Theme`, `Screens Folder` | first theme name; the Resources folder of the screens (empty: screens build their elements in code) |

## What it leaves to you

- The HUD, Controls and Gameplay style sheets: this assembly does not reference those modules. Call `HudStyles.Install(host.Root)`, `ControlsStyles.Install(host.Root)` or `GameplayStyles.Install(host.Root)` from `Ready`.
- Panel scale: use `PanelScaleDriver` from Foundation on your `PanelSettings` ([Foundation/README.md](../Foundation/README.md)).
- Saved settings (theme name, text size, language): [Recipes.md](../Recipes.md) recipes 7 and 8.

## Do not combine with `UIKitHost`

The shell pads the safe area and the input profile writes `fdp-input-touch`. If a `UIKitHost` is on the same document, the facade sees the shell's root class `fdp-shell`, gives its padding back and stops writing the touch class, so nothing is doubled,
but the facade then does nothing useful; leave it out.

## Tests

`Tests/Runtime/Bootstrap/UIShellHostTests.cs` (PlayMode, a RenderTexture panel): the kit is built and `Ready` raised with the game's screens booted, a back press pops a screen, a theme registered under `"dark"` before `Start` is kept, disable
tears everything down and enable builds it again; `InputProfileModeSourceTests` (device kinds, a null profile, detaching). Nothing was run with a real input device.
