# Animation Toolkit Demo

A small runtime page that shows the Animation module of the Future Digital Plus UI Kit. It is new code written for this
package; the upstream example scene and editor window are not part of it (see `Runtime/Animation/SOURCE.md`).

## Use it

1. Import the sample from Package Manager (*Future Digital Plus UI Kit > Samples > Animation Toolkit Demo*).
2. Create a GameObject with a `UIDocument` component. Any `PanelSettings` asset works, no UXML is needed.
3. Add the `AnimationToolkitDemo` component to the same GameObject and press Play.

## What it shows

| Control | Methods used |
|---|---|
| Fade message | `AnimFadeInSequence` |
| Animate bar width | `AnimateWidth` |
| Hover me | `HoverBackground` |
| Show toast | `SetOpacity`, `ShowForDuration` |
| Start / stop pulse | `AnimBorderPulse`, `SetBorderWidth`, `SetBorderColor` |
| Details | `AnimatedFoldout` |

The sample has its own assembly definition (`FutureDigitalPlus.UIKit.Samples.Animation`) that references
`FutureDigitalPlus.UIKit.Animation`. A usage guide for every method is in `Documentation~/Animation/README.md`.
