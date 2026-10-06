# Future Digital Plus UI Kit: guides

| Guide | About |
|---|---|
| [GettingStarted.md](GettingStarted.md) | Install in another game, first themed screen, module matrix |
| [Recipes.md](Recipes.md) | Copy-paste solutions: one bootstrap component, the ready-made shell host, themed screen, dialogs, toasts, gamepad navigation, HUD bar, inventory grid, localization hook, brand theme, one UI on every screen size |
| [Samples.md](Samples.md) | The eleven samples: kind of game, what each shows, modules used, what was verified |
| [ChooseModules.md](ChooseModules.md) | Which modules and assemblies a game needs, by game type, platform and optional package |
| [Migration.md](Migration.md) | Moving the card game's UI foundation (`FCS.UI`, `--fcs-*`, `fcs-*`) onto the kit: name, token and class tables |
| [CompatibilityMatrix.md](CompatibilityMatrix.md) | What was run: tests per render pipeline, colour space, input backend and optional package, player compile per target, the samples, and what is not verified |
| [Platforms.md](Platforms.md) | Render pipelines, platforms and Unity versions: what is verified, what is not, per platform notes, what ships in a build, how to reproduce |
| [Upgrading.md](Upgrading.md) | Re-sync a module from upstream, diff, test, bump the version |
| [CONTRIBUTING.md](CONTRIBUTING.md) | Change the kit: the rules, the checks to run, the version bump, regenerating the classic copy |
| [RELEASING.md](RELEASING.md) | Open items (licence), tags, install lines (HTTPS and SSH), the classic copy, how a game updates, the kit repository inside another repository |
| [Verification.md](Verification.md) | What was verified for 0.1.0 and for the modules added for global use, with the exact commands and results |
| [UpstreamFeatures.md](UpstreamFeatures.md) | Exactly what is included, excluded and modified per upstream project |
| [UIKit/README.md](UIKit/README.md) | The facade: `UIKitHost`, `UIKitInstaller`, safe area, mobile class |
| [Bootstrap/README.md](Bootstrap/README.md) | `UIShellHost`: one component that runs the shell, theme, input, accessibility, language and back key |
| [Foundation/README.md](Foundation/README.md) | Shell, screen router, dialogs, toasts, loading, safe area, panel scale, profile classes; the card game's `FCS.UI` names mapped to the kit |
| [Interaction/README.md](Interaction/README.md) | Input mode classes, focus ring, activation, back key, accessibility (reduced motion, text size, bold) |
| [Language/README.md](Language/README.md) | Localized text (`UiLocalization`, `LocalizedLabel`, `BindText`), language classes, right-to-left layout, Unity Localization adapter, system-font fallbacks |
| [Hud/README.md](Hud/README.md) | In-game overlay widgets: meter bar, radial progress, cooldown icon, rolling counters, countdown, floating text, world anchors, feed, zones |
| [Controls/README.md](Controls/README.md) | Tab bar and segmented control with badges, context menu, select field, bottom sheet, list and grid helpers, reward popup |
| [Gameplay/README.md](Gameplay/README.md) | Inventory model (stacks, merge, split, swap, rules), item slots, slot grid with drag and keyboard pick-up |
| [DesignSystem/README.md](DesignSystem/README.md) | Tokens, themes, components, icons, fonts (upstream guides) |
| [DesignSystem/THEMING.md](DesignSystem/THEMING.md) | Runtime and brand themes, token list, contrast, high contrast, `ThemeManager`, the touch tier |
| [Components/README.md](Components/README.md) | UIComponents framework: layouts, queries, dependency injection, generators |
| [Animation/README.md](Animation/README.md) | Animation extension methods and animated elements |
| [Extras/README.md](Extras/README.md) | Tooltip, popup, colour picker, aspect ratio padding, blur |

The rename tooling and the mapping from upstream names to ours are in `Tools~/rename/` (`README.md`, `rename-map.md`). The test matrix runner is in `Tools~/Matrix/`,
the no-Unity compile, documentation and name checks in `Tools~/CompileCheck/`, the sample smoke tests in `Tools~/SamplesSmoke/` and the documentation checks (links, recipes, snippets) in `Tools~/DocsCheck/` (each with a `README.md`).
The package README gives the overview. Per-module source records (upstream, commit, copied, excluded, modified) are
`Runtime/<Module>/SOURCE.md`; licences and checksums are in `THIRD_PARTY_NOTICES.md`.
