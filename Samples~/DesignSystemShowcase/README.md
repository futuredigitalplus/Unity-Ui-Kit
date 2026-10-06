# Design System showcase (sample)

A small page that shows the DesignSystem module working: buttons, inputs, tabs, toggles, sliders, meters,
badges, icons, toasts, and a switch between the base dark tokens and the bundled Light theme.

## Use

1. Window > Package Manager > Future Digital Plus UI Kit > Samples > Design System Showcase > Import.
2. Create an empty scene, add an empty GameObject, add the component
   `Future Digital Plus UI Kit > Design System Showcase Sample`.
3. Press Play.

The sample needs nothing but this package: no uGUI, no Input System, no EventSystem, no extra fonts.

## What is in it

| File | What it is |
|---|---|
| `DesignSystemShowcase.cs` | New code. Builds the UIDocument and PanelSettings, adds the design system's runtime behaviour, wires the theme switch. About 100 lines, meant to be read and copied. |
| `Resources/FutureDigitalPlus/UIKit/Samples/DesignSystemShowcase/DesignSystemShowcase.uxml` | Excerpt of the upstream showcase page (MIT). |
| `Resources/FutureDigitalPlus/UIKit/Samples/DesignSystemShowcase/UnityDefaultRuntimeTheme.tss` | One line: imports Unity's default runtime theme, which every runtime PanelSettings needs. |
| `FutureDigitalPlus.UIKit.Samples.DesignSystemShowcase.asmdef` | References `FutureDigitalPlus.UIKit.DesignSystem` by name. Not auto-referenced. |

The sample's Resources subfolder is namespaced (`FutureDigitalPlus/UIKit/Samples/`) so it cannot collide with a game's own
Resources paths. Delete the imported sample folder when you are done; nothing else depends on it.

## What it leaves out

The upstream showcase also has a walkable world-space gallery (Unity 6000.5 and newer only), a Codigrate
IDE-theme provider, a multilingual font section, a documentation overlay and a WebGL build pipeline. None of that
is needed to learn the module and some of it needs Unity 6000.5, so it is not part of this sample. See
`Runtime/DesignSystem/SOURCE.md` for the full exclusion list.
