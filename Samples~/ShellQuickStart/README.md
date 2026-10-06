# Shell quick start (sample)

The plumbing every game with more than one screen needs: a screen router, dialogs, toasts, a loading overlay, safe area and back handling, with localized texts.
Genre: card, puzzle, casual, any game with menus around a board. Modules: Foundation, Controls, Language.

1. Window > Package Manager > Future Digital Plus UI Kit > Samples > Shell Quick Start > Import.
2. Create an empty scene, add an empty GameObject, add the component `Future Digital Plus UI Kit > Shell Quick Start Sample`.
3. Press Play.

## What you see

- A home page with a top bar, a three-item `TabBar` (a badge on "Shop") and three buttons: a toast, a destructive confirmation dialog, and a two-second loading overlay.
- A settings page (button in the top bar) with a Language row that switches English and German while both pages stay alive, and an About row that opens an alert.
- Back works from every device: the top bar arrow, Escape, gamepad B and the Android back button. A dialog or the loading overlay takes the press first, then the page is popped.

## What to read in `ShellQuickStartSample.cs`

| Step | Code |
|---|---|
| Panel and document | `PanelSettings` made in code, a `UIDocument` configured while inactive |
| Styles | `FoundationStyles.InstallDesignSystem` on the panel root, `ControlsStyles.Install`, `LanguageStyles.Install` |
| Texts | a `DictionaryTextSource` behind a `UiLocalization`, a `UiTextSource` for the shell, `UiText.FromKey(key).WithFallback(text)` everywhere |
| Shell | `new UiShell(root, options)`, `Catalog.Register(id, factory)`, `Boot(id)`, `Tick()` from `Update` |
| Pages | classes derived from `UiScreen`: build the tree in `OnBuild`, focus the first control in `OnShow` |
| Services | `Ui.Router.Push/Pop`, `Ui.Dialogs.Confirm`, `Ui.Toasts.Show`, `Ui.Loading.Begin/End` |

The sample makes its own `UiLocalization` instead of using `UiLocalization.Default`, so destroying the component leaves no static state behind.
It does not give the shell a state store, so the navigation stack is not saved; pass `StateStore = new PlayerPrefsUiStateStore()` in `UiShellOptions` to restore the open pages after a restart.

## Notes

- Nothing but this package is needed: no Input System, no Localization, no TextMeshPro, no uGUI.
- Run in Unity 6000.3.25f1 by `Tools~/SamplesSmoke` (PlayMode: it builds without a logged error, shows the widgets named above, survives 30 frames of its own `Update` and leaves no document behind) in the Built-in pipeline, URP 3D, URP 2D and HDRP, with the Input System package only and with all optional packages; see `Documentation~/CompatibilityMatrix.md`. The test does not click or press anything, and nothing was run on a device.
- The Resources folder is namespaced (`FutureDigitalPlus/UIKit/Samples/ShellQuickStart`); it holds the one-line runtime theme every `PanelSettings` needs.
