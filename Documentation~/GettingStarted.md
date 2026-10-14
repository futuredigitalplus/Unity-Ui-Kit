# Getting started

Install the FDP UI Kit in another Unity game, build a first themed screen, and see which module to
reference for what. The package id (`com.futuredigitalplus.uikit`) and display name are placeholders the owner may
rename; see the README. After this page, [Recipes.md](Recipes.md) has copy-paste solutions for the common tasks (one bootstrap
component, screens, dialogs, toasts, gamepad navigation, HUD, inventory, localization, brand theme, responsive layout).

## 1. Install (2 minutes)

Unity 6000.3 or newer (`package.json` and `VERSION` say `6000.3`). Developed and verified on 6000.3.25f1; other versions are untested. No other packages are required.

**Choose UPM or classic.** The kit comes in two install forms from one source: a **UPM package** (1a, referenced from `Packages/manifest.json`) and a **classic Assets
package** (1b, a plain folder `Assets/FutureDigitalPlus/UIKit` generated from the UPM source). Same code, same `.meta` files and GUIDs, same tests; nothing in a game's code or assets changes
between them. Pick UPM when several games share the kit (one tag or path rolls an update out to all). Pick classic when a game has to build from its own repository
alone (no sibling kit repository, no git host reachable from CI, a vendor that gets only the game), or when you want the kit's files in the game's own history. Never install both in one project.

### 1a. As a UPM package

In the game's `Packages/manifest.json`, pick one:

```json
{
  "dependencies": {
    "com.futuredigitalplus.uikit": "https://github.com/futuredigitalplus/Unity-Ui-Kit.git#v0.2.2"
  }
}
```

- The repository root is the package root, so the git URL needs no `?path=`. Always pin a tag or commit. The SSH form is
  `git@github.com:futuredigitalplus/Unity-Ui-Kit.git#v0.2.2`. See [RELEASING.md](RELEASING.md).
- A local clone works too: `"com.futuredigitalplus.uikit": "file:../../Unity-Ui-Kit"`. A relative `file:` path is resolved from the game's
  `Packages` folder (the example is for a game project that sits next to a clone called `Unity-Ui-Kit`). Do not commit an absolute `file:/...` path.
- To edit the kit from inside a game, copy the folder into the game's `Packages/` (embedded package) and port changes back.

Unity creates nothing in your `Assets` folder. The package brings two built-in modules it needs
(`unitywebrequest`, `vectorgraphics`; the second imports the SVG icons).

### 1b. As a classic Assets package

No `manifest.json` entry, no `testables`. The folder is generated from the UPM source (the folder that holds `package.json`):

```sh
python3 <kit source>/Tools~/make_classic_kit.py --src <kit source> --dst <game>/Assets/FutureDigitalPlus/UIKit
```

`<kit source>` is the root of a clone of the kit repository, checked out at the tag to adopt (see [RELEASING.md](RELEASING.md)), for example
`python3 ../Unity-Ui-Kit/Tools~/make_classic_kit.py --src ../Unity-Ui-Kit --dst Assets/FutureDigitalPlus/UIKit` from a game's root.

What you get (`ls Assets/FutureDigitalPlus/UIKit`):

```
Runtime/  Editor/  Tests/            the code, with its .meta files (same GUIDs as the UPM package)
Samples~/  Documentation~/  Tools~/  hidden from Unity by the trailing ~ (no import, no compile, no .meta)
README.md  CHANGELOG.md  THIRD_PARTY_NOTICES.md  .gitignore  .gitattributes   as in the source (README.md gets a "Classic copy" banner)
VERSION                              first line: the kit version; then package id, display name, Unity version, required modules, the sample list
.fdp-classic-manifest.json           sha256 of every generated file (a dot file, Unity ignores it): how the next run detects local edits
```

`package.json` is not copied; its metadata is in `VERSION` and the README. Also written, when missing: `Assets/FutureDigitalPlus.meta` and `Assets/FutureDigitalPlus/UIKit.meta`.
Commit the whole folder, `.meta` files and the manifest included (the folder's own `.gitignore` re-includes `*~/` folders and `*.dll`, which a typical Unity `.gitignore` would drop).

- **Modules.** Add `com.unity.modules.unitywebrequest` and `com.unity.modules.vectorgraphics` to the game's `Packages/manifest.json` if they are not there (new projects have them). A package install brings them by itself.
- **Samples.** Copy the sample you want from `Samples~/<Name>` to `Assets/Samples/<Name>`. Do not rename the whole `Samples~` folder in the copy: all eleven samples would compile
  into the game, their components have no convention layouts (the Components editor tests then fail), and the next update sees a deleted and an added folder and asks for `--force`.
- **Tests.** The kit's tests are in `Tests/` and show up in the Test Runner without `testables`. In a game that has PlayMode tests of its own, run the kit's PlayMode tests (`--filter FutureDigitalPlus`) and the game's in separate runs: in one run the two interfere (see `Migration.md` 10.7); EditMode can run together. Their `Resources` assets (about 44 KB) are packed into player builds in both layouts.
  `--exclude Tests` leaves the folder out of the copy if the game does not want the kit's tests (the lint tests then cannot run either).
- **Update.** Run the script again from the (updated) source. It rewrites only the files that differ, deletes files that left the kit, and refuses (exit 2) when the copy was edited by hand: look at the listed
  files, port the change to the UPM source if it is worth keeping, then `--force`. `--check` (exit 3 on a difference) is for CI. More in [Upgrading.md](Upgrading.md) "Updating a classic copy".
- **Where is the kit?** Never hard-code `Packages/com.futuredigitalplus.uikit` or `Assets/FutureDigitalPlus/UIKit` in editor code or tests: `FutureDigitalPlus.UIKit.Location.KitLocation` (assembly `FutureDigitalPlus.UIKit.Location.Editor`)
  returns the asset path and the absolute path in either layout, found by the GUID of the `Runtime` folder. Stylesheet and UXML references should use the GUID form (`project://database/?fileID=...&guid=...&type=3`) or the real path of the layout in use; a path of the other layout still resolves by GUID but logs a "was moved" message on import.
- **Moving between layouts.** Remove the manifest line, run the script, let Unity import. GUIDs are identical, so scene, prefab and asset references survive; only text paths (`project://database/Packages/...`, `Assets/...` strings in tools) change.

Optional: `com.unity.addressables` 1.18.19 or newer switches on `FutureDigitalPlus.UIKit.Components.Addressables` automatically.
Running the kit's own tests (section 6) also needs `com.unity.test-framework`, which new projects already have.

## 2. First screen in the editor (10 minutes)

1. **Panel settings.** Assets > Create > UI Toolkit > Panel Settings Asset. In *Theme Style Sheet* assign a runtime
   theme (Unity's default one is created by Assets > Create > UI Toolkit > Default Runtime Theme File).
   Do not leave it empty: without a theme Unity logs "No Theme Style Sheet set to PanelSettings", the document root gets no height
   and text gets no font, so a screen looks empty or collapsed.
   Set *Scale Mode* to *Scale With Screen Size* and a reference resolution for your game (for example 1080 x 1920).
2. **UXML.** Create `MainMenu.uxml`. Do not add a `<Style>` for the design system (see the note below). Use the
   design system classes:

   ```xml
   <ui:UXML xmlns:ui="UnityEngine.UIElements">
       <ui:VisualElement class="ds-root" style="justify-content: center; align-items: center;">
           <ui:VisualElement class="ds-card" style="width: 320px;">
               <ui:Label text="Hello" class="ds-h2" />
               <ui:Button name="play" text="Play" class="ds-btn ds-btn--primary" />
           </ui:VisualElement>
       </ui:VisualElement>
   </ui:UXML>
   ```

   A `ds-btn` has no colours of its own: always add a variant (`ds-btn--primary`, `--secondary`, `--tertiary`, `--ghost` or `--danger`).
   A bare `ds-btn` falls back to Unity's default button colour, and on a dark theme its light text is hard to read on it.
3. **Scene.** Add an empty GameObject, add a `UIDocument` (Panel Settings = step 1, Source Asset = `MainMenu`), then
   *Add Component > FDP UI Kit > UI Kit Host*. (The menu path uses the package display name.)
4. **Theme.** In the host, set *Theme* to `Light` (click the circle next to the field and pick it; it is listed under
   *Assets* and *Packages*, and lives in `Packages/FDP UI Kit/Runtime/DesignSystem/Resources/FutureDigitalPlus/UIKit/DesignSystem/Themes`, or below `Assets/FutureDigitalPlus/UIKit` in the classic layout),
   or leave it empty for the dark base tokens. Press Play: you see the card on the dark or light background.

The host puts the design system stylesheet on the panel root, the theme on the document root, the `fdp-input-touch` class
(touch-size tier, automatic by platform and panel width) and the device safe area padding. Assign `host.Theme` from code
to switch themes at runtime. Everything it does is described in [UIKit/README.md](UIKit/README.md).

Import **Samples > UI Kit Quick Start** from the Package Manager (classic layout: copy `Samples~/UIKitQuickStart` to `Assets/Samples/UIKitQuickStart`) for the same thing built in code (about 100 lines).

If you go on to section 4b and add a `UiShell`, clear the document's *Source Asset*: the shell's Home screen replaces `MainMenu`, and screens built in
code have no background of their own, so a leftover `MainMenu` would show through every screen.

> **Why no `<Style>` for the design system in your UXML?** On Unity 6000.3 the base stylesheet declares its tokens under
> `:root`, and that outranks a class-scoped theme (`.theme-light`) sitting on the same element or on an ancestor of the
> element holding the base sheet, whatever the sheet order. A theme only wins from a descendant of the element that holds
> the base sheet. The facade therefore puts the base sheet on the panel root and the theme on the document root.
> If you wire things by hand, do the same (`root.parent.styleSheets.Add(baseSheet)`, then `ThemeRuntime.Apply(root, theme)`;
> `ThemeRuntime` is in the namespace `FutureDigitalPlus.UIKit.DesignSystem.Theme.Applier`, the base sheet is
> `Resources.Load<StyleSheet>("FutureDigitalPlus/UIKit/DesignSystem/Styles/DesignSystem")`).
> This differs from what the upstream guides say; details in `Runtime/DesignSystem/SOURCE.md`.

## 3. Add behaviour with the other modules (the rest of the 15 minutes)

| Want | Do |
|---|---|
| A component class with its own UXML/USS, queried fields and dependency injection | Create an asmdef for your UI code and add `FutureDigitalPlus.UIKit.Components` to its `references` (by name). Put `MainMenu.uxml` at `Resources/UI/MainMenu.uxml`. Write `using FutureDigitalPlus.UIKit.Components; [Layout("UI/MainMenu")] public partial class MainMenu : UIComponent { [Query("play")] Button play; }` (the classes must be `partial`; the bundled generators write the loading and query code), then `root.Add(new MainMenu());`. The component loads its assets when it is attached to a panel. See [Components/README.md](Components/README.md). |
| Fade, slide, hover animations | `using FutureDigitalPlus.UIKit.Animation;` then `element.AnimateOpacity(0f, 1f, 250)`, `element.AnimateWidth(0f, 300f, 600)`, `element.HoverBackground(...)`. From the facade: `using FutureDigitalPlus.UIKit;` then `element.FadeIn(250)` / `element.FadeOut()`. The element must be in the tree first. See [Animation/README.md](Animation/README.md). |
| Tooltip, popup, colour picker | `using FutureDigitalPlus.UIKit.Extras;` then `var tooltip = new Tooltip(); root.Add(tooltip); button.AddManipulator(new TooltipManipulator(tooltip));` (add the tooltip last so it paints over the rest; the text comes from the element's `tooltip` property). With the design system's base sheet on the panel (the facade and the shell put it there) the Extras controls take their colours from the active theme; without it they use the upstream demo palette. To restyle them, see [Extras/README.md](Extras/README.md), "Using the module". |
| Fonts | The kit bundles no font. Use Tools > FDP UI Kit > Design System > Google Fonts to import what the game wants. |

## 4. Module matrix

| Module | Assemblies | Reference it from your asmdef | Needs | Auto-referenced | Resources paths it owns |
|---|---|---|---|---|---|
| DesignSystem | `FutureDigitalPlus.UIKit.DesignSystem`, `FutureDigitalPlus.UIKit.DesignSystem.Editor` (Editor only) | `FutureDigitalPlus.UIKit.DesignSystem` | nothing | yes (Assembly-CSharp sees it) | `FutureDigitalPlus/UIKit/DesignSystem/Styles/*`, `FutureDigitalPlus/UIKit/DesignSystem/Themes/*`, `FutureDigitalPlus/UIKit/DesignSystem/Icons/*`, `FutureDigitalPlus/UIKit/DesignSystem/Shaders/*` |
| Components | `FutureDigitalPlus.UIKit.Components`, `FutureDigitalPlus.UIKit.Components.Testing`, `FutureDigitalPlus.UIKit.Components.Addressables` (optional), `FutureDigitalPlus.UIKit.Components.Editor` | `FutureDigitalPlus.UIKit.Components` (plus `FutureDigitalPlus.UIKit.Components.Testing` in test assemblies) | nothing; Addressables optional | **no**, always reference by name | none |
| Animation | `FutureDigitalPlus.UIKit.Animation` | same name | nothing | yes | `FutureDigitalPlus/UIKit/Animation/Styles/AnimatedFoldoutStyle`, `FutureDigitalPlus/UIKit/Animation/Styles/AnimatedLabel`, `FutureDigitalPlus/UIKit/Animation/Styles/MaskedInputField` |
| Extras | `FutureDigitalPlus.UIKit.Extras` | same name | nothing | yes | `FutureDigitalPlus/UIKit/Extras/Styles/*` |
| UIKit (facade) | `FutureDigitalPlus.UIKit` | same name | DesignSystem + Animation | yes | none |
| Foundation | `FutureDigitalPlus.UIKit.Foundation.Core` (pure C#), `FutureDigitalPlus.UIKit.Foundation` | both | nothing | yes | `FutureDigitalPlus/UIKit/Foundation/Styles/{Foundation,Foundation.Tokens}` |
| Interaction | `FutureDigitalPlus.UIKit.Interaction.Core`, `...Interaction`, optional `...Interaction.InputSystem` | the first two | nothing; Input System optional | yes (adapter: no) | `FutureDigitalPlus/UIKit/Interaction/Styles/{Focus,A11y}` |
| Language | `FutureDigitalPlus.UIKit.Language`, optional `...Language.UnityLocalization` | `FutureDigitalPlus.UIKit.Language` (and `Foundation.Core` for `UiText`) | `Foundation.Core`; Unity Localization optional | yes (adapter: no) | `FutureDigitalPlus/UIKit/Language/Styles/Rtl` |
| Hud | `FutureDigitalPlus.UIKit.Hud.Core`, `...Hud` | both | nothing | yes | `FutureDigitalPlus/UIKit/Hud/Styles/Hud` |
| Controls | `FutureDigitalPlus.UIKit.Controls.Core`, `...Controls` | both | nothing | yes | `FutureDigitalPlus/UIKit/Controls/Styles/Controls` |
| Gameplay | `FutureDigitalPlus.UIKit.Gameplay.Core`, `...Gameplay` | both | nothing | yes | `FutureDigitalPlus/UIKit/Gameplay/Styles/Gameplay` |
| ScrollView | `FutureDigitalPlus.UIKit.ScrollView.Core` (pure C#), `...ScrollView` | both | nothing | yes | none |

"Auto-referenced" means only that the predefined `Assembly-CSharp` sees the assembly. Code in your own asmdef must
list every module it uses in its `references`, by name (for example `FutureDigitalPlus.UIKit.DesignSystem` and `FutureDigitalPlus.UIKit`).

Rules that hold for every module:

- No module references game code, and no game code is referenced by the kit.
- Using one module does not drag the others into your build more than their assemblies being compiled; the Roslyn
  generators of Components only run on assemblies that reference `FutureDigitalPlus.UIKit.Components`.
- The `Resources` paths in the table are global in Unity (all `Resources` folders merge). Do not create your own assets at
  those paths; the paths are documented per module in the `SOURCE.md` files.
- Tests ship with the kit. As a UPM package they are only compiled when the package is listed in `testables`; in the classic layout they are part of the project and always compiled:

  ```json
  "testables": [ "com.futuredigitalplus.uikit" ]
  ```

## 4b. A game with more than one screen, a HUD, an inventory or a gamepad

The first screen above needs only the design system and the facade. The modules added for global use are plain code you call yourself; each has a [sample](Samples.md) that is one file to read and copy.
**Which module for which kind of game** is in [ChooseModules.md](ChooseModules.md); the short version:

| You are building | Start from | Then |
|---|---|---|
| menus around a board (card, puzzle, casual) | [Shell Quick Start](../Samples~/ShellQuickStart/README.md): a `UiShell` with a router, dialogs, toasts, loading | add `Language` for texts, `Controls` for tabs |
| an RPG or strategy game | [RPG Inventory](../Samples~/RpgInventory/README.md): `Gameplay` slot grids, `Controls` context menu | add the shell for the rest of the screens |
| an action game or shooter | [Action HUD](../Samples~/ActionHud/README.md): `Hud` widgets in the shell's HUD layer, `PauseMenu` on the router | `HudZones` inside a `SafeAreaPanel` |
| a console, handheld or desktop game | [Gamepad Menu](../Samples~/GamepadMenu/README.md): `Interaction` focus ring and back key, `FocusTrap` in dialogs | a title-safe margin on TVs |
| a game for phones, tablets and windows | [Responsive](../Samples~/Responsive/README.md): `PanelScaleDriver`, size-class classes, `SafeAreaPanel` | `UserScale` as the player's "UI size" |
| a game with theme, text size or language options | [Theming and Language](../Samples~/ThemingLanguage/README.md): `ThemeManager`, `AccessibilityApplier`, `LocaleClassApplier` | right-to-left languages mirror automatically |

The smallest shell, without a facade (the full version with localization is the Shell Quick Start sample):

```csharp
using FutureDigitalPlus.UIKit.Foundation;
using UnityEngine;
using UnityEngine.UIElements;

public sealed class Boot : MonoBehaviour
{
    UiShell _shell;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        FoundationStyles.InstallDesignSystem(root.parent);     // the ds-* look, on the panel root
        _shell = new UiShell(root);
        _shell.Catalog.Register("Home", () => new HomeScreen());   // a class derived from UiScreen
        _shell.Boot("Home");
    }

    void Update() => _shell.Tick();                             // safe area and back key
    void OnDisable() => _shell?.Dispose();
}
```

The modules do not reference each other, so a game joins them in its own code (the samples show each join: the input device to the shell, the back key to the shell, localization to the shell's text source).
`UIShellHost` ([Bootstrap/README.md](Bootstrap/README.md)) is that code as a component of the kit: put it on the `UIDocument`'s GameObject, register your screens in its `Ready` event and call `Shell.Boot`. It does the styles, the Light/Dark themes, the input profile, accessibility, language classes, the shell and the back key.
[Recipes.md, recipe 0](Recipes.md#0-one-bootstrap-for-the-whole-kit) is the same code spelled out (about 100 lines) for a game that wants other joins.
A `UIKitHost` and a shell can share a document (sections 2 and 4b of this page were run that way in three host projects: 2D URP with the Input System, 3D Built-in with the Input Manager, HDRP with a gamepad only), but there is nothing for the host to do then: as soon as a shell lives under its root the host gives its safe-area padding back (the shell pads its own top bars, dialogs and toasts) and stops writing `fdp-input-touch` (the input profile owns the class). The Foundation guide has the exact API.

What the three host projects ran into when they followed this page, so you do not have to:

- **Your own screens are not padded.** Put a HUD or any full-screen content that must clear a notch inside a `SafeAreaPanel`; a `TopBar` already is one.
- **Saved navigation.** `StateStore = new PlayerPrefsUiStateStore(...)` brings the player back to the screen they left. While you develop, that is the screen your last Play session ended on: clear the stored
  `fdp/navigation` key (or run once without a state store) when a scene "starts on the wrong screen".
- **The in-game screen is the router's root.** Back (Escape, gamepad B, the Android back button) pops the router. If the game screen was pushed on top of Home, Back leaves the game; make it the root with
  `Router.ResetTo("Game")` and open `Pause` on Back when nothing is left to pop (the Action HUD sample does exactly this).
- **A HUD zone is a column that hugs its content.** Several widgets added to one zone stack vertically; wrap them in a row `VisualElement` for a skill bar. A `MeterBar` has a minimum width of 160 units; give it
  `style.width` or a wider container for another size.
- **Test a menu without a device.** A batch-mode editor has no focus, so a gamepad cannot be driven from a test. Send the events UI Toolkit raises for it instead: `NavigationMoveEvent`, `NavigationSubmitEvent` and
  `NavigationCancelEvent` to `panel.focusController.focusedElement`, and a pointer press and release for a touch. Everything in this kit reacts to those events. Unity's own bridge from a device to those events
  (Input Manager axes, or the Input System package's UI actions) was not exercised by this test: check a menu with a real gamepad once.
- **Elements added later.** The host re-scans the tree for design system behaviours (toggle knobs, tabs, drop-downs) a few times a second, so a `ds-toggle` in a screen built after start gets its knob without a call.

## 5. Swap the theme or restyle

- Switch at runtime: `host.Theme = otherThemeData;` (or `null` for the base tokens).
- Make your own theme: duplicate `Light.asset` or `Dark.asset` into your `Assets` folder (the package is read-only when
  installed from git), open it (Theme Configurator window), edit colours, spacing, radii, and press Save. Assign it to the host.
- Different look for a different game: the design tokens are plain USS custom properties (`--fdp-color-primary`, `--fdp-space-4`,
  ...), so a theme is a short generated stylesheet, and no module code has to change.
- A brand theme at run time, with no editor step: `ThemeData.FromBrand(primary, background, dark)` derives a whole theme from two colours; `ThemeManager` applies it to every attached root. High contrast is `ThemeManager.HighContrast`.
  The Theming and Language sample shows both; the details are in [DesignSystem/THEMING.md](DesignSystem/THEMING.md).

## 6. Check it works in your project

Run the package's own tests in your project once (Window > General > Test Runner, with the `testables` entry above; no entry is needed in the classic layout).
They need no scene and no network. In the kit's own test project this takes about a minute.
