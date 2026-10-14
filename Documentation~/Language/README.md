# Language module

Text that follows the player's language: localized labels and buttons, a language-to-style mapping on the root (font per script, right-to-left layout), a mirrored stylesheet for the design
system, and system-font fallbacks for the scripts a game did not bundle a font for. It works with no localization package at all (labels show their fallback text or a visible `[KEY]`), with
Unity Localization through an optional adapter, or with your own text source.

| | |
|---|---|
| Assembly | `FutureDigitalPlus.UIKit.Language` (`Runtime/Language`), auto-referenced; references only `Foundation.Core` (the pure `UiText`, key resolver and `LocaleStyle`) |
| Optional assembly | `FutureDigitalPlus.UIKit.Language.UnityLocalization`, compiled only when `com.unity.localization` 1.4 or newer is installed (`versionDefines`, define `FDP_UIKIT_UNITY_LOCALIZATION`) |
| Font tooling | in the DesignSystem module: `DsOsFonts`, `DsOsFontOrder`, `DsFonts.ApplyLocale` (`Runtime/DesignSystem/Typography`) |
| Stylesheet | `Resources/FutureDigitalPlus/UIKit/Language/Styles/Rtl.uss`, attached by `LanguageStyles.Install(panelRoot)` |
| Depends on | nothing else; no TextMeshPro, no uGUI, no game code |

## Quick start

```csharp
using FutureDigitalPlus.UIKit.Foundation;   // UiText
using FutureDigitalPlus.UIKit.Language;

// 1. A text source. No package: an in-memory one (code, CSV or JSON). Unity Localization: nothing to do, see below.
var texts = new DictionaryTextSource("en");
texts.LoadCsv("table,key,en,de,ar\nUI,PLAY,Play,Spielen,العب\n");
UiLocalization.Default.Backend = texts;

// 2. Bind text. The element follows every language change while it is on a panel, and unsubscribes when it leaves.
playButton.BindText(UiText.FromKey("PLAY").WithFallback("Play"));     // Label, Button, any TextElement
toggle.BindText(UiText.FromKey("SOUND"), text => toggle.label = text); // any other text property

// 3. Put the language on the root, so stylesheets can react (fdp-lang-ar, fdp-rtl) and install the mirrored layout.
LanguageStyles.Install(panelRoot);                  // after the design system base sheet
LocaleClassApplier.Bind(documentRoot);              // follows UiLocalization.Default until disposed

// 4. Change the language.
texts.SetLocale("ar");                              // raises Changed: bound text and root classes update
```

In UXML: `<fdp:LocalizedLabel key="HOME-PLAY" fallback="Play" />` and `<fdp:LocalizedButton key="UI:SAVE" />` with `xmlns:fdp="FutureDigitalPlus.UIKit.Language"`. Arguments for `{0}` placeholders are set from
C# with `SetArgs(...)`.

## API

| Type | What it does |
|---|---|
| `UiLocalization` | The one place a `UiText` becomes a string. `Default` is shared; create your own for a second, independent localization (a store window, a world-space panel). `Backend` (default: a null backend), `DefaultTables`, `Resolve(UiText)`, `Changed`, `TableMissing`, `CurrentLocale`, `NotifyChanged()`. Never throws, never returns null. |
| `ILocalizationBackend` | `CurrentLocale`, `Changed`, `TryGet(table, key, args, out text)` returning `LookupStatus` (`Found`, `EntryMissing`, `TableMissing`). Implement it to plug in a server, a game's own tables, or another package. |
| `NullLocalizationBackend` | The default: every table is missing, so fallbacks and `[KEY]` show. |
| `LocaleFallbackLookup` (Foundation.Core) | The fallback walk of `UnityLocalizationBackend`, pure and engine-free: `Find(selected, fallbacksOf, lookup, out text)` asks the selected locale, then its fallbacks breadth first, each once (at most `MaxLocales`, 16); an entry that is missing, empty or unformattable passes the key on; only the selected locale's `TableMissing` is reported. |
| `DictionaryTextSource` | In-memory backend: `Add`, `LoadCsv` (header `table,key,<locale>...`, quotes, commas and line breaks in fields, BOM), `LoadJson` (`{"entries":[{"locale","table","key","value"}]}`), `SetLocale`, `FallbackLocale`. A lookup tries `pt-BR`, then `pt`, then the fallback locale (default `en`). For prototypes, tests, small games and server-delivered text. |
| `LocalizedTextBinder` | Keeps one text property in sync with a `UiText`: resolves now and on every change while attached, never overwrites a text set by hand before the first `SetText`, unsubscribes on detach. The element holds the callbacks; nobody keeps or disposes the binder. |
| `BindText`, `BindKey` | Extension methods that create a binder for a `TextElement` or any setter. |
| `LocalizedLabel`, `LocalizedButton` | UXML elements with `key`, `table` and `fallback` attributes. |
| `LocaleClassApplier` | `Apply(root, locale)` puts `fdp-lang-<code>` (and `fdp-rtl` plus `languageDirection = RTL` for Arabic, Hebrew, Persian, Urdu and the other right-to-left languages) on a root and removes the previous language's classes; `Bind(root)` does it again on every change. |
| `LanguageStyles.Install(panelRoot)` | Adds `Rtl.uss` (idempotent). Inert until `fdp-rtl` is on a root. |
| `UnityLocalizationBackend` (optional assembly) | Unity Localization as a backend. Registers itself as `UiLocalization.Default.Backend` before the first scene when the project has Localization settings and no other backend was set. Loads each table once, asynchronously (on every platform except WebGL the first lookup of an unloaded table waits for it; `SynchronousFirstLoad`), answers `TableMissing` until a table has arrived and then raises `Changed`, reloads the tables it used when the selected locale changes. An entry the selected locale lacks or has empty comes from the locale's fallbacks (its `FallbackLocale` metadata: es-AR, then es, then en, breadth first, each locale once, so a loop ends), as Unity's own `LocalizedString` does when the string database's Use Fallback is on (the setting is honoured); a fallback locale's table is loaded the same way, and one that does not exist is skipped. An entry that cannot be formatted (a malformed Smart String or `{0}` pattern, an argument it cannot take) logs one warning and counts as missing, so the fallback locale's text or the `UiText` fallback shows: it never turns the whole table into `TableMissing` (only a table that cannot be loaded does). `Preload(name, table)` hands it a table directly, `Preload(name, table, locale)` a table of a fallback locale. |
| `BindText(TextElement, LocalizedString)` (optional assembly) | Binds a Unity Localization `LocalizedString` (smart-string arguments and all) while the element is on a panel. |
| `DsOsFonts` (DesignSystem) | System-font fallbacks for one panel, see "Fonts" below. |
| `DsFonts.ApplyLocale(locale, osFonts, family, warmText)` | One call for "the language changed": sets `DsScripts.Han` (Han letterforms), reorders the system fonts, warms glyphs of the next screen. |

## Right-to-left

`LocaleClassApplier` also sets `languageDirection`, which reorders the text itself (bidirectional text) but does **not** mirror flex layouts. `Rtl.uss` does that for the design system's components
under `.fdp-rtl` (about 100 selectors): icon and label rows (button, chip, search field, nav item, toast, sheet row, info row, toggle, check, radio), tabs, pagination, stepper, dialog and modal action rows
list from the right; the gap between an icon and its label moves to the other side; chevrons and arrows that point sideways are flipped (`scale: -1 1`); meter and progress fills grow from the right;
toggle knob and travel; side drawers swap sides; left-aligned text aligns right and the other way round. Numbers and Latin words keep their direction (Unicode bidi does that).

Not mirrored (known limits): `Slider` and the range slider (Unity computes the thumb position from the left edge), the side of a `ScrollView`'s vertical scrollbar, and elements the kit does not style.
Your own screens: write `.fdp-rtl .my-row { flex-direction: row-reverse; }` for each row that needs it, or use `row-reverse` with `justify-content` in the component. The Foundation module carries its own `fdp-rtl` rules for its views. The Controls module's `TabBar` and `SegmentedControl` mirror themselves (items from the right, arrow keys that follow the screen; see [../Controls/README.md](../Controls/README.md), "Right to left"), also without `Rtl.uss`; the other modules' sheets do not mirror yet. To ask whether an element is right to left from code, use `UiDirection.IsRightToLeft(element)` (Foundation): the nearest `languageDirection` set on the way to the root, else `fdp-rtl`; the element's own `languageDirection` is `Inherit` almost everywhere.

## Fonts for other scripts

A shipped font set has no Han, kana or Hangul, and shaping scripts (Arabic, Hebrew, Thai, the Indic scripts, Khmer) need a font the advanced text generator can shape. The design system already has the
bundled-font side (`DsFonts`, `DsScripts`, the Google Fonts importer; `FONTS.md`). `DsOsFonts` adds the operating system's fonts **as a fallback**, by family name only (no font file is copied):

```csharp
var panel = DsOsFonts.UseRuntimePanelSettings(document);   // a runtime copy: the project asset is never changed
var osFonts = new DsOsFonts();
osFonts.Install(panel, localeCode);                          // adds the system fonts for the language after the game's own
// when the language changes:
DsFonts.ApplyLocale(newLocale, osFonts, family, textOfTheNextScreen);
// when the panel goes away:
osFonts.Dispose();
```

Rules it keeps: it adds to the panel's fallback list and removes exactly what it added (never `Clear()`); the panel gets its own `PanelTextSettings` copy; its fonts come after the game's, so a bundled font wins;
the language's own Han typeface comes first (Han unification); a system font for a shaping script is used only if the text generator can shape it (`DsFonts.CanShape` / `TryEnableShaping`), otherwise it is skipped and listed in `Rejected`
(a font that cannot shape draws Arabic as unjoined letters). Nothing happens when no candidate is installed. Candidate families (macOS, Windows, Android, Linux names) are in `DsOsFontOrder`.
`DsFonts.InstallFallbacks` now behaves the same way and `DsFonts.FallbacksOf(panel)` reports a panel's own list.

The fonts are dynamic OS font assets: a glyph is rasterised on first use, so pass the strings of the screen you are about to show to `DsFonts.ApplyLocale` (or `DsFonts.Warm`) to avoid a hitch on the first CJK screen.

## What works without optional packages

| Installed | Behaviour |
|---|---|
| nothing | `UiLocalization.Default` has the null backend; labels show their fallback or `[KEY]`; one warning per missing table and language change (`LogMissingTables = false` silences it); nothing throws. `DictionaryTextSource` and CSV/JSON work. |
| `com.unity.localization` | the adapter assembly compiles and registers itself (when the project has Localization settings); `LocalizedString` binding. |
| WebGL | no system fonts (`DsOsFonts.Install` adds nothing) and no synchronous table loads; bundle fonts for the scripts you ship. |

## Not verified

The PlayMode tests (the binder, the mirrored layout order of the six components, `Rtl.uss` on a panel, the UXML attribute path of `LocalizedLabel`, the per-panel system fonts) and the EditMode tests ran in Unity 6000.3.25f1 in every variant of [../CompatibilityMatrix.md](../CompatibilityMatrix.md); the Unity Localization adapter ran with Localization 1.5.13 against an in-memory table. **Not verified**: the adapter with real tables and locale changes in a game, system-font loading and shaping on any platform but macOS (on macOS Geeza Pro, the advanced and the standard text generator measure the same Arabic width, so the width test skips itself and shaping through a system font stays unproven), IL2CPP.
