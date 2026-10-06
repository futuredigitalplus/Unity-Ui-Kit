# Hud

Widgets for the screen a player looks at while playing: health and mana bars, ability icons with cooldowns, rolling counters for score and currency, countdown timers, floating combat text, name plates and markers pinned to things in the 3D world, and a message feed. It is for any game with an in-game overlay: action, FPS, RPG, strategy, racing, puzzle and casual games alike. Nothing here knows about a game's rules; each widget is told a number and draws it.

| | |
|---|---|
| Assemblies | `FutureDigitalPlus.UIKit.Hud.Core` (pure C#, no `UnityEngine`, tested by dotnet) and `FutureDigitalPlus.UIKit.Hud` (elements) |
| Namespace | `FutureDigitalPlus.UIKit.Hud` |
| Depends on | nothing else in the kit. Uses design system tokens when the design system sheet is on the panel, with a literal fallback for every token |
| Does not need | Foundation, Interaction, Components, TextMeshPro, uGUI, Cinemachine, any render pipeline package |
| Styles | `Resources/FutureDigitalPlus/UIKit/Hud/Styles/Hud.uss`, installed with `HudStyles.Install(root)` |

## Set up

```csharp
using FutureDigitalPlus.UIKit.Hud;

var root = uiDocument.rootVisualElement;
HudStyles.Install(root);                     // once per panel, after the design system base sheet (optional)

var zones = new HudZones { TitleSafePercent = 0 };   // 5 on a TV
root.Add(zones);                                     // pass-through: the game still gets pointer events

var health = zones.Add(HudZone.TopLeft, new MeterBar { Size = MeterBarSize.Large, LabelMode = MeterLabelMode.ValueAndMax });
health.Set(184f, 240f);                              // animates, shows the damage trail, flips to warning and danger

var coins = zones.Add(HudZone.TopRight, new CurrencyLabel());
coins.Value = 1250;                                  // rolls up, shows "+250" for a moment

var dash = zones.Add(HudZone.BottomCenter, new CooldownIcon());
dash.StartCooldown(6f);                              // the sweep runs, the seconds show, a flash marks "ready"
dash.Ready += () => Debug.Log("dash ready");
```

A zone is a column that hugs its content: widgets added to one zone stack vertically (wrap them in a row `VisualElement` for a skill bar), and a `MeterBar` has a minimum width of 160 units (set `style.width`, or put it in a wider
container, for another size).

Put `HudZones` inside the safe area container of the shell (Foundation) so that nothing sits under a notch; `TitleSafePercent` adds the extra margin televisions need. The Hud has no safe area logic of its own.

In UXML the elements are `[UxmlElement]` types: `xmlns:hud="FutureDigitalPlus.UIKit.Hud"` and `<hud:MeterBar value="60" max-value="100"/>`. `TickDriver` and `MotionGate` are internal.

## Elements

| Element | What it does | Main members |
|---|---|---|
| `MeterBar` | Health, mana, stamina, XP, progress. Carries the design system meter skin (`ds-meter`) so it matches themed bars. Delayed damage trail, optional segments, label modes, warning and danger colours by threshold with hysteresis (no flicker at the edge), `HighIsBad` for heat and fear meters | `Value`, `MaxValue`, `Set(value, max)`, `LabelMode`, `Size`, `ShowTrail`, `Segments`, `HighIsBad`, `WarningThreshold`, `DangerThreshold`, `Level` |
| `RadialProgress` | A ring drawn with `Painter2D` (cast time, charge, loading). The ring is the element's `color`, the track its `-unity-background-image-tint-color`, so a theme styles it with ordinary USS | `Value`, `Thickness`, `StartAngle`, `Clockwise`, `SmoothSeconds` |
| `CooldownIcon` | Ability or item icon: dark wedge sweeping away clockwise, remaining seconds, a flash and a `--ready` class when it comes back | `SetIcon`, `StartCooldown`, `ResetCooldown`, `ReduceCooldown`, `Ready`, `IsReady`, `RemainingSeconds`, `ShowText` |
| `CountUpLabel` | A number that rolls from the old value to the new one (score, damage dealt) | `Value`, `DurationSeconds`, `Format` (plain, grouped, compact `1.2K`), `Prefix`, `Suffix`, `Culture`, `SnapTo` |
| `CurrencyLabel` | Icon, rolling count and a short "+250" / "-40" note | `Value`, `ShowDelta`, `DurationSeconds`, `Format`, `Culture`, `SetIcon`, `SnapTo` |
| `CountdownLabel` | Time left to a deadline: `3d 4h`, `02:15`, `45s`; raises `Expired` once. The clock is injectable (wall clock, server time, game time) | `StartIn(seconds)`, `StartAt(endTime)`, `Stop`, `Clock`, `Format`, `ExpiredText`, `Units` (localised unit letters), `Expired`, `Remaining` |
| `FloatingTextLayer` | Damage numbers, heals, "+10": rise, fade, pop on critical, de-overlap, a cap per frame. Labels are pooled, so spawning allocates nothing after warm-up | `Spawn(panelPosition, text, kind)`, `Model` (tuning), `ClearTexts` |
| `WorldAnchorLayer` + `WorldAnchor` | Pins any element to a world position or a `Transform` (name plates, quest markers, off-screen arrows). You hand it the `Camera`, so Cinemachine, a custom rig or an orthographic camera all work with no dependency; it projects with the viewport fraction of the camera's target scaled to the panel's size, so it is right for a screen panel and for a panel that renders into a `RenderTexture` | `Camera`, `Pin(content, transform / position, ...)`, `Remove`, `RemoveAll`, `EdgeMargin`, `ArrowDistance`; per anchor `Mode` (`Hide` or `Clamp` with an arrow), `Pivot`, `Offset`, `Result`. An anchor whose `Transform` was destroyed is hidden (call `Remove`); a position that is not finite hides it too |
| `TimedFeed` | Kill feed, loot log, system notices. Identical messages merge into one line with a counter, lines fade and go | `Push(text, key, style)`, `MaxEntries`, `TimeToLive`, `NewestOnTop`, `ClearEntries`, `Model` |
| `HudZones` | Nine anchor areas (corners, edges, centre) in a full-size pass-through container | `Zone(HudZone)`, `Add(zone, widget)`, `TitleSafePercent` |

The pure part (`Hud.Core`) holds the logic of every element, so it can be tested and reused without Unity: `MeterModel`, `CooldownModel`, `CountUpModel`, `CountdownModel`, `TimedFeedModel`, `FloatingTextModel`, `WorldAnchorMath` and `NumberFormatter` (grouping by an injected `CultureInfo`, compact K/M/B, invariant fallback).

## How it behaves

- **No Update loops.** Every element runs from the element scheduler and only while something moves; a bar that is not animating schedules nothing. Nothing in the module is a `MonoBehaviour`.
- **No garbage in steady state.** `MeterBar` and `RadialProgress` repaint without allocating, floating text and world anchors reuse their elements. PlayMode tests assert 0 bytes (see "Tests"). The Mono editor cannot count allocations per thread, so these tests skip themselves there; they count on IL2CPP and .NET runtimes only.
- **Unscaled time.** Rolling counters, the countdown and the feed use real time, so they keep moving while the game is paused with `Time.timeScale = 0`.
- **Reduced motion.** When an ancestor has the class `fdp-reduce-motion` (written by the Interaction module's accessibility applier, or by you), counters change at once, the trail and the flash are off, and the USS transitions of the module drop to 0 ms.
- **Cultures.** Numbers use the element's `Culture` (default: invariant grouping with a comma). Pass the player's culture or the one of your localisation system. The countdown unit letters are replaceable through `CountdownUnits`.
- **Pointer events.** HUD elements and layers use `pickingMode: Ignore`, so the game under the HUD keeps getting clicks; give a widget `PickingMode.Position` when it must be clickable.

## Choosing widgets by game type

| Game | Typical set |
|---|---|
| Action, FPS, shooter | `MeterBar` for health and shield, `CooldownIcon` for abilities, `FloatingTextLayer`, `WorldAnchorLayer` for name plates and objective markers, `TimedFeed` for the kill feed |
| RPG, MOBA | Above plus `MeterBar` with segments for resource pips, `CountdownLabel` for buffs |
| Strategy, city builder | `CurrencyLabel` per resource, `CountdownLabel` for build and research timers, `TimedFeed` for alerts |
| Racing, sports | `CountUpLabel` for lap time or score, `RadialProgress` for boost, `CountdownLabel` for the start |
| Puzzle, casual, card | `CurrencyLabel`, `CountUpLabel` for score, `MeterBar` for moves or progress, `FloatingTextLayer` for "+50" popups |
| XR | Pin the same widgets in a world-space panel; `WorldAnchorLayer` needs a screen camera, so use plain layout there |

For gamepad-only games a HUD is read-only; the elements take no focus.

## Tests

Current test counts: [../CompatibilityMatrix.md](../CompatibilityMatrix.md), "Test counts".

- `Tests/Pure/Hud` (EditMode asmdef `...Hud.Core.Tests`, also run by `Tools~/PureTests` under dotnet): the models and the formatter.
- `Tests/Runtime/Hud` (PlayMode `...Hud.Tests`, UI Toolkit panel on a `RenderTexture`): every element, including the painted pixels of `RadialProgress`, pooling, a real camera for the world anchors, an injected clock for the countdown, culture grouping, reduced motion, the allocation tests, and `SixWidgetHud_BuildsAndLaysOut` which is the acceptance sample (six widgets in the zones).

## Not included

`AmmoCounter` and `Crosshair` (stretch items of the plan) were not built: an ammo counter is a `CountUpLabel` plus an icon and a crosshair is one `VisualElement`; add them when a game needs more than that.
