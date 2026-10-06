# Interaction module

Which device is the player using, and what must the UI do about it: a focus ring for keyboard and gamepad (and none for a mouse click),
"Submit" that works like a click, one "back" signal for Escape, the Android back button and gamepad B, and the accessibility settings
(reduced motion, text size, bold text, high contrast) as classes on the root. Everything works with the legacy Input Manager, with the
Input System package, or with both, and with none of the optional packages installed.

| Assembly (all `FutureDigitalPlus.UIKit.` + ...) | Contents | Needs |
|---|---|---|
| `Interaction.Core` | `InputModeTracker`, `InputDeviceKind`, `BackKeyDeduper`, `TextScaleSteps`, `AccessibilityOptions`, `EffectiveAccessibility`, `SystemAccessibility`, `AccessibilityClasses`. Pure C# (`noEngineReferences`), tested with plain dotnet | nothing |
| `Interaction` | `InputProfile`, `Activate`, `BackKeySource`, `AccessibilityApplier`, `InteractionStyles`, `InputFeed`, the legacy backend, `UnitySystemAccessibility`, `Focus.uss`, `A11y.uss` | UnityEngine, UI Toolkit. The built-in accessibility module is used when present (`FDP_UIKIT_ACCESSIBILITY`) |
| `Interaction.InputSystem` | Input System backend (device kinds from state events with a stick threshold, Escape and gamepad east as back) | `com.unity.inputsystem` (define `FDP_UIKIT_INPUTSYSTEM`; the code is compiled when the project's Active Input Handling includes the Input System). Registers itself, nothing to call |

The module does not reference Foundation, Components, Language or any game code. The assembly that owns the router and the dialogs connects to it
(see "Wiring" below).

## Quick start

```csharp
using FutureDigitalPlus.UIKit.Interaction;

var root = uiDocument.rootVisualElement;
InteractionStyles.Install(root);                      // Focus.uss and A11y.uss, below the design system base sheet
var input = InputProfile.Bind(root);                  // writes fdp-input-* classes, raises ModeChanged
var a11y  = AccessibilityApplier.Bind(root, options); // writes fdp-reduce-motion, fdp-text-scale-N, fdp-bold-text, fdp-high-contrast
var back  = new BackKeySource();                      // Escape, Android back, gamepad B, NavigationCancel
back.Bind(root);
back.Pressed += () => router.HandleBack();

// a clickable plain element that keyboard and gamepad can reach
var row = Activate.Register(rowElement, () => OpenDetails());

// a HUD document that must not react to WASD or the D-pad
hudInput.NavigationEnabled = false;
```

Dispose `InputProfile`, `AccessibilityApplier`, `BackKeySource` and the `Activate` registration when the document goes away. The device backends run only while at least one
of them exists (`InputFeed.Acquire/Release`), so a game that binds none pays nothing.

## The input mode

`InputProfile` keeps one of four modes, the last device that produced real input, and mirrors it as classes on the root:

| Class | While | Used for |
|---|---|---|
| `fdp-input-mouse` | mouse or pen | hover states are meaningful |
| `fdp-input-touch` | a finger | the touch size tier of the design system (`ManageTouchClass = false` leaves the class to another component) |
| `fdp-input-keyboard`, `fdp-input-gamepad` | keyboard, gamepad or joystick | |
| `fdp-input-nav` | keyboard or gamepad | the focus ring, tooltips on focus, "hold focus on screen change" |

Rules that make the answer stable (all in the pure `InputModeTracker`): stick and trigger movement below 0.3 is drift and ignored; a mouse report within 0.75 s of a touch is
emulation of that touch and ignored; `Force(mode)` pins the mode (a settings option "always show focus"); the start mode is the platform default (gamepad on consoles,
touch on handhelds, mouse elsewhere). Escape on a handheld is the back button, not evidence of a keyboard.

Pointer devices are read from the pointer events of the bound element, so this works with every backend and every panel kind. Keyboard and gamepad come from the backends and
from the navigation events that reach the element (a navigation event while a pointer mode is active means keyboard, until a backend says gamepad).
`NavigationEnabled = false` stops Move and Submit events at the root (trickle down) so a HUD document does not steal keys the game reads.

## Focus ring (`Focus.uss`)

UI Toolkit applies `:focus` after a click as well, so a plain `:focus` rule leaves a ring on every pressed button. The ring here exists only under `fdp-input-nav`, on the
design-system control families (`ds-btn`, chip, card, nav items, tabs, toggle, checkbox, radio, slider, range, dropdown, text input and the close buttons). Colour
`--fdp-color-focus-ring` (default: the theme's primary text colour), width `--fdp-focus-ring-width` (default the regular border, 2px). Tabs and the items that have no border at rest (`ds-nav-item`, `ds-sheet__row`, `ds-bottom-nav__item`, `ds-rail-item`) reserve the ring's space with a transparent border
in navigation mode, so a focus move changes colours only; controls that have a 1px border grow by the difference, so set `--fdp-focus-ring-width: 1px` where a 1px layout shift is too much. Text fields: the ring is on the inner input,
because `:focus` sits on the outer field.

## Activate

A pointer click raises `ClickEvent`; Enter and the gamepad south button raise `NavigationSubmitEvent` and no click. Code that listens for `ClickEvent` alone is unreachable without a pointer,
and a plain `VisualElement` is not even focusable. `Activate.Register(element, action, focusable: true)` runs the action once per click and once per Submit and makes the element focusable
(the previous value is restored on dispose). For component code, `IOnNavigationSubmit`, `IOnNavigationCancel`, `IOnFocusIn`, `IOnFocusOut`, `IOnPointerDown/Up/Move` and `IOnKeyDown`
join the Components module's `IOn*` interfaces (same registration by the bundled generator).

The design system's behaviours already do this for its own items: nav items, rail items, sheet rows and bottom-nav items are focusable and take Submit; Escape or B closes an open drawer;
scroll bars flash when focus moves; drag and drop has a keyboard alternative (Submit picks up, arrows move between `ds-drop-zone`s, Submit drops, Cancel aborts); spinners and skeletons
respect reduced motion. Extras: the tooltip shows on focus (navigation mode only) and on a long press with a finger, `PopupPanel` keeps focus inside, closes on Cancel and can register with
a back dispatcher (`PopupPanel.BackRegistrar`), `ColorField` opens on Submit, `Slider2D` takes Submit, arrows and the D-pad (Shift for larger steps).

## Back

`BackKeySource.Pressed` fires once per press from every source: the device backends (Escape, Android back, gamepad east / B) and `NavigationCancelEvent` reaching the bound root.
Both are needed, because UI Toolkit delivers Cancel only to the focused element and on a touch device nothing is focused, so the Android back button never reaches the panel by itself.
One physical press counts once per frame (`BackKeyDeduper`). Limit: a control that consumes Cancel itself (a dropdown popup) does not stop the press from reaching `Pressed`;
such a control should register with the dispatcher instead.

## Accessibility

`AccessibilityOptions` (plain serialisable fields: reduce motion, high contrast, bold text, text size 100 to 200, and four "follow the OS" switches) plus the OS values
(`ISystemAccessibility`; the default reads the font scale and bold text that Unity exposes on iOS and Android) resolve to `EffectiveAccessibility`. `AccessibilityApplier` writes:

| Class | Effect (`A11y.uss`) |
|---|---|
| `fdp-reduce-motion` | the three transition tokens become 0 ms, the five literal transitions of the design system are overridden, spinners pulse instead of rotating, shimmer is hidden |
| `fdp-text-scale-125` .. `-200` | the type tokens get pre-computed sizes (USS has no `calc`, hence steps; the requested percentage is snapped to the nearest step) and fixed-height controls keep their design height as a minimum and grow with the text |
| `fdp-bold-text` | bold body text |
| `fdp-high-contrast` | marker for sheets; the matching theme is a separate choice |

Code that animates on its own calls `applier.ScaleMotion(ms)` or reads `applier.Current`. The Animation module has a hook for this: `EatkMotion.Scaler = applier.ScaleMotion;` makes every
duration the module passes to UI Toolkit 0 ms (it becomes 1 ms, finishing on the next tick). Nothing assigns the hook by itself.

The kit contains no native code: iOS Reduce Motion, the Android animation scale and the Windows high-contrast setting are not readable from Unity. Implement `ISystemAccessibility` with a plugin
of your own and pass it to `AccessibilityApplier.Bind`.

## Wiring to the rest of the kit

`Interaction` has no reference to Foundation, so the router and shell do not know it. Four small adapters, written by the assembly that references both (the `Bootstrap` assembly does: `UIShellHost` and `InputProfileModeSource`, see [../Bootstrap/README.md](../Bootstrap/README.md); a game that wants other joins writes them in a few lines, and
[../Recipes.md](../Recipes.md), recipe 0, shows all four):

- the shell's input-mode source: `Mode` is `profile.Mode` mapped by name (`Pointer`, `Touch`, `Keyboard`, `Gamepad` exist in both enums), `ModeChanged` is `profile.ModeChanged`;
- the shell's back-key source: set a flag in `BackKeySource.Pressed` and return and clear it from the poll method;
- the accessibility values the other modules read (`UiAccessibility`, `UiMotion`): copy `applier.Current` on `AccessibilityApplier.Changed`;
- `EatkMotion.Scaler` and `PopupPanel.BackRegistrar`: assign from the same place.

## Input configurations

| Active Input Handling | Backends | Notes |
|---|---|---|
| Input Manager (old) | legacy | needs the default axes `Horizontal`, `Vertical`, `Submit`, `Cancel`; a warning names a missing one. A joystick stick cannot be told from keyboard arrows (shared axes), so a gamepad is recognised by its buttons; stick-driven navigation shows the keyboard class, which shows the same ring |
| Input System Package (new) | Input System | state events with a 0.3 threshold tell stick drift from input |
| Both | both | the back key is de-duplicated per frame |

## Optional packages

| Package | Without it | With it |
|---|---|---|
| `com.unity.inputsystem` | the Input System assembly does not exist; legacy backend only | adapter registers itself (`RuntimeInitializeOnLoadMethod`, kept by `AlwaysLinkAssembly`) |
| built-in module `com.unity.modules.accessibility` | `UnitySystemAccessibility` reports font scale 1 and no bold text | reads `AccessibilitySettings.fontScale` and bold text and raises `Changed` |
| uGUI | the EventSystem is not touched | the stretch adapter `Interaction.UGui` (select the panel's `PanelEventHandler`) is not part of this version; see Known limits |

## Known limits

- Run in Unity 6000.3.25f1 in every variant of [../CompatibilityMatrix.md](../CompatibilityMatrix.md): legacy Input Manager, Input System only (no device attached), both. The device backends were driven by injected events only:
  no real keyboard, mouse, touch screen or gamepad, no phone, console or TV. The pure logic also runs under plain dotnet.
- `DropdownField` does not change its item with the D-pad in Unity 6000.3 (measured in the analysis); use the Controls module's select field on consoles.
- Screen readers (VoiceOver, TalkBack) are not bridged; Unity's accessibility API is not used for the UI Toolkit tree.
- World-space pointer input and the uGUI bridge (`Interaction.UGui`) are not included (stretch items of the plan).
- `fdp-text-scale-*` sizes are computed from the default token sizes, and a step beats the type sizes of any theme (its rules repeat the class, so the order of the sheets does not matter). A theme that changes the type scale therefore keeps its own sizes only at 100 percent.
