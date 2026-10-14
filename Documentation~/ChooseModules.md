# Choosing modules

The kit is one package with several modules, each in its own assembly set. A game references only the assemblies it
uses. This page answers three questions: which modules does my kind of game need, what does each one pull in, and which
optional packages change what works.

**Status column.** "Ships" means the module was in 0.1.0. "New" means the module was added for global use. Every module, old and new, is verified in Unity 6000.3.25f1 as a whole
(EditMode, PlayMode, player script compile) in every variant of [CompatibilityMatrix.md](CompatibilityMatrix.md); what that matrix does not cover (devices, consoles, XR, other Unity versions) is listed there.
The README has a shorter "Which modules do I need?" table, and [Recipes.md](Recipes.md) the code for the common tasks. A module's own guide (`Documentation~/<Module>/README.md`) is the authority for its API; the assembly names, references and defines on this page were read from the asmdefs.

## 1. The modules

| Module | Status | Assemblies (all `FutureDigitalPlus.UIKit.` + ...) | What it gives | References |
|---|---|---|---|---|
| DesignSystem | Ships | `DesignSystem` (+ `.Editor`) | tokens, themes (dark, light, high contrast, run-time brand themes), `ds-*` components, icons, behaviours, fonts tooling | nothing |
| Components | Ships | `Components` (+ `.Testing`, `.Editor`, `.Addressables`) | attribute-driven `UIComponent` base class, generators, test bed | nothing; the four Roslyn DLLs only apply to assemblies that reference it |
| Animation | Ships | `Animation` | chainable `VisualElement` animations, animated elements | nothing |
| Extras | Ships | `Extras` | tooltip, popup panel, colour picker, aspect-ratio padding, blur panel | nothing |
| UIKit facade | Ships | `UIKit` | `UIKitHost`, `UIKitInstaller`, theme + touch class + safe area in one call | DesignSystem, Animation |
| Foundation | New | `Foundation.Core` (pure C#), `Foundation` | `UiShell`: screen router, dialogs, toasts, loading overlay, safe area, panel scale, size-class classes, pause menu, top bar, list rows | `Foundation.Core` only. The views read the design system sheet by Resources path (one warning if it is absent) |
| Interaction | New | `Interaction.Core`, `Interaction`, optional `Interaction.InputSystem` | last-used input device classes, focus ring, back key, `Activate`, accessibility settings | nothing |
| Language | New | `Language`, optional `Language.UnityLocalization` | localized labels and buttons, dictionary text source, locale and RTL classes, RTL sheet | `Foundation.Core` |
| Bootstrap | New | `Bootstrap` | `UIShellHost`: one component that runs the shell, themes, input profile, accessibility, language classes and the back key | DesignSystem, Animation, Foundation, Interaction, Language |
| Hud | New | `Hud.Core`, `Hud` | meters, radial progress, cooldowns, counters, countdown, floating text, world anchors, feed, HUD zones | nothing |
| Controls | New | `Controls.Core`, `Controls` | tab bar and segmented control, context menu, select field, bottom sheet, list and grid helpers, reward popup | Foundation (the `FocusTrap` class only) |
| Gameplay | New | `Gameplay.Core`, `Gameplay` | inventory model, item slots and slot grids with drag, keyboard and gamepad pick-up | nothing |
| ScrollView | New (not yet run through the matrix) | `ScrollView.Core`, `ScrollView` | FancyScrollView for UI Toolkit: carousels and wheels whose cells animate by position (loop, snap, inertia), lists of fixed-height rows and grids that build only the cells in view | nothing |

Rules that hold for every new module (so the table does not change when a module is added):

- No new module references Components, so a game that never uses `UIComponent` never compiles the generator-backed assembly.
- No module references TextMeshPro, uGUI, Cinemachine, URP, HDRP or XR packages.
- Every `*.Core` assembly has `noEngineReferences`, so its logic is testable in plain .NET and cannot call `Screen` or `Application`.
- Every optional adapter has its own assembly with `versionDefines` and `defineConstraints`, so with the optional package missing it does not exist.
- The new modules hardly reference each other: `Language` uses `Foundation.Core` (the pure text types), and `Controls` uses `Foundation` (for `FocusTrap`, so a menu or sheet keeps focus the way a dialog does). Everything else a game joins in its own code; the samples show the few joins
  (for example Interaction's `InputProfile` to the shell's `IUiInputModeSource`, and Interaction's `BackKeySource` to `UiShell.HandleBackPress`); the `Bootstrap` assembly does those joins once, in `UIShellHost`, so the modules stay free of each other.

## 2. Which modules for which game

`x` = recommended, `o` = often useful, blank = usually not needed. "Facade" is the `UIKit` assembly. The last column names the [sample](Samples.md) that shows that kind of game.

| Game type | DesignSystem | Facade | Foundation | Interaction | Language | Hud | Controls | Gameplay | Animation | Extras | Components | Sample |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| Card, puzzle, casual (menus around a board) | x | x | x | o | x | | o | | o | o | | Shell Quick Start |
| Match-3 / idle with shop and settings screens | x | x | x | o | x | o | x | | x | o | | Shell Quick Start |
| RPG, strategy (inventory, menus, tooltips) | x | x | x | x | x | o | x | x | o | x | | RPG Inventory |
| Action, FPS, racing (HUD over a 3D scene) | x | x | o | x | o | x | o | | o | | | Action HUD |
| Roguelike, survival (HUD + inventory + pause) | x | x | x | x | o | x | x | x | o | o | | Action HUD, RPG Inventory |
| Console / handheld, TV (gamepad only) | x | x | x | x | o | o | x | o | | | | Gamepad Menu |
| Desktop, resizable window (mouse + keyboard) | x | x | x | x | o | o | x | o | | x | | Responsive |
| Mobile, tablet, foldable (touch, notch) | x | x | x | o | o | o | o | | o | | | Responsive |
| Any game that lets the player change theme, text size or language | x | x | x | x | x | | o | | | | | Theming and Language |
| Tool or editor-style app inside a game | x | | o | x | o | | x | | | x | o | |
| One shared widget in an existing UI (no facade) | x | | | | | | | | | | | Design System Showcase |

Why:

- **Foundation** is the plumbing every game with more than one screen has: navigation, dialogs, toasts, loading, safe area,
  panel scale. A HUD-only game can skip it, but then it must handle safe area itself.
- **Interaction** matters as soon as a gamepad or keyboard can drive the UI. Without it the design system still works with a
  mouse or touch.
- **Language** is needed for more than one language or any right-to-left language. With no localization package it shows
  the fallback text and never throws.
- **Hud** and **Gameplay** are separate because a menu game should not carry them.
- **ScrollView** is for any game type that shows a carousel (level or mode select, a shop shelf) or a list or grid too long to build at once
  (leaderboard, chat, collection). For a short list, UI Toolkit's own `ScrollView` or Controls' `ListViewBinder` and `VirtualGrid` are enough.
- **Components** is only for teams that want the attribute-driven component pattern; nothing else in the kit needs it.

## 3. Minimum assembly sets

An asmdef in your game lists these by name. A game without asmdefs, which compiles into `Assembly-CSharp`, reaches the auto-referenced runtime assemblies without any list; today
every runtime assembly of the kit except Components and the optional adapters is auto-referenced.

| Goal | References |
|---|---|
| Design system look only | `FutureDigitalPlus.UIKit.DesignSystem` |
| Themed document in one call (today) | `FutureDigitalPlus.UIKit` (references DesignSystem and Animation itself; add them if you call their types) |
| Screens, dialogs, toasts | `FutureDigitalPlus.UIKit.Foundation.Core`, `FutureDigitalPlus.UIKit.Foundation` |
| Localized text | add `FutureDigitalPlus.UIKit.Language` (it needs `Foundation.Core`) |
| A shell, theme, input, accessibility, language and back key joined by one component | `FutureDigitalPlus.UIKit.Bootstrap` (it references DesignSystem, Animation, Foundation(.Core), Interaction(.Core) and Language, so a game that lists it needs those in its asmdef only if it calls their types) |
| Gamepad / keyboard navigation, accessibility | `FutureDigitalPlus.UIKit.Interaction.Core`, `FutureDigitalPlus.UIKit.Interaction` |
| HUD widgets | `FutureDigitalPlus.UIKit.Hud.Core`, `FutureDigitalPlus.UIKit.Hud` |
| Tab bar, context menu, select field, sheets | `FutureDigitalPlus.UIKit.Controls.Core`, `FutureDigitalPlus.UIKit.Controls` (which pulls in `FutureDigitalPlus.UIKit.Foundation` and `Foundation.Core` at run time) |
| Inventory | `FutureDigitalPlus.UIKit.Gameplay.Core`, `FutureDigitalPlus.UIKit.Gameplay` |
| Carousel, long list or grid | `FutureDigitalPlus.UIKit.ScrollView.Core`, `FutureDigitalPlus.UIKit.ScrollView` |
| Game logic tests with no engine | any `*.Core` assembly alone: plain C#, no UnityEngine |

The `*.Core` types (for example `HudZone`, `OffScreenMode` and `FloatingTextKind` in `Hud.Core`) share the namespace of their views assembly, so a file that uses them lists both assemblies.

Components (`FutureDigitalPlus.UIKit.Components`) is not meant to be pulled in by accident: reference it by name only in the
assemblies that define `UIComponent` classes.

## 4. Optional packages and what changes

None is required. Each row says what you gain by installing the package; with the package missing nothing breaks.

| Optional package | Define the kit sets (inside the adapter's own assembly) | Effect |
|---|---|---|
| Input System (`com.unity.inputsystem`) | `FDP_UIKIT_INPUTSYSTEM` | assembly `Interaction.InputSystem`: device detection and the back key through the Input System. Without it the kit uses the legacy Input Manager (axes `Horizontal`, `Vertical`, `Submit`, `Cancel` must exist) |
| Unity Localization (`com.unity.localization`) | `FDP_UIKIT_UNITY_LOCALIZATION` | assembly `Language.UnityLocalization`: `UnityLocalizationBackend` puts Unity string tables behind `LocalizedLabel`. Without it use `DictionaryTextSource` or the fallback text |
| Addressables (`com.unity.addressables`) | `UICOMPONENTS_ADDRESSABLES_INSTALLED` | the Components Addressables asset source. A Foundation Addressables source is not in the package |
| Accessibility module (built in) | `FDP_UIKIT_ACCESSIBILITY` (inside `Interaction`) | reads OS accessibility settings where Unity exposes them |
| uGUI (`com.unity.ugui`) | none | no uGUI bridge is in the package; the kit never references uGUI |
| (host define, off by default) | `FDP_UIKIT_AUTO_ATTACH` | upstream behaviour: attach the design system behaviours to every `UIDocument` automatically |

## 5. Platforms and render pipelines

The kit draws with UI Toolkit only. It has no dependency on the render pipeline, so Built-in, URP and HDRP all work
(the matrix ran Built-in, URP 3D and 2D, and HDRP, with every module; HDRP supports Linear only). Platform
notes, what was verified and what was not (WebGL, tvOS, visionOS, consoles, Windows and Linux players, Unity versions
other than 6000.3.25f1) are in [CompatibilityMatrix.md](CompatibilityMatrix.md), [Platforms.md](Platforms.md) and the README "Verification" section. Treat every target in the
"not verified" list as unverified until a row in CompatibilityMatrix.md says otherwise.

## 6. Rules of thumb

1. Start with DesignSystem plus the facade; add Foundation when you have a second screen.
2. Add Interaction before you ship on a console, handheld or desktop. A touch-only mobile game can leave it out.
3. Add Hud and Gameplay only to the games that draw a HUD or an inventory.
4. Keep game-specific tokens (card colours, brand palette) in the game's own theme; the kit's themes carry roles, not brands.
5. Do not reference a module "just in case": each assembly you leave out is code you do not ship.
