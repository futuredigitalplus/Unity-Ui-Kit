using FutureDigitalPlus.UIKit.Animation;
using FutureDigitalPlus.UIKit.Animation.Extensions;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Samples.AnimationModule
{
    /// <summary>
    /// Builds a small page that shows the Animation module: a fading message, a hover colour, an animated bar,
    /// a self-hiding toast, a pulsing border and an AnimatedFoldout. Everything is created in code, no UXML needed.
    ///
    /// How to use: add this component to a GameObject that has a UIDocument (any PanelSettings asset), then press Play.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class AnimationToolkitDemo : MonoBehaviour
    {
        private static readonly Color Calm = new Color(0.20f, 0.30f, 0.45f, 1f);
        private static readonly Color Bright = new Color(0.30f, 0.55f, 0.95f, 1f);
        private static readonly Color Text = new Color(0.90f, 0.92f, 0.95f, 1f);

        private IVisualElementScheduledItem _pulse;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            root.Clear();
            root.style.paddingTop = 24;
            root.style.paddingLeft = 24;
            root.style.paddingRight = 24;

            // 1. Chained fade-in / hold / fade-out of a message that returns to its original text.
            var message = new Label("Press the button to fade this text.");
            message.style.color = Text;
            root.Add(message);
            root.Add(MakeButton("Fade message", () =>
                message.AnimFadeInSequence("Hello from the Animation module", Bright, Text, 400f, 1200f, 400f)));

            // 2. A single base animation: grow a bar to a width and back.
            var bar = new VisualElement();
            bar.style.height = 8;
            bar.style.width = 20;
            bar.style.marginTop = 12;
            bar.style.marginBottom = 12;
            bar.style.backgroundColor = Bright;
            root.Add(bar);
            var grown = false;
            root.Add(MakeButton("Animate bar width", () =>
            {
                bar.AnimateWidth(grown ? 300f : 20f, grown ? 20f : 300f, 600);
                grown = !grown;
            }));

            // 3. Hover colour without USS :hover.
            var hoverBox = new Label("Hover me") { style = { paddingTop = 8, paddingBottom = 8, marginTop = 12 } };
            hoverBox.style.color = Text;
            hoverBox.style.backgroundColor = Calm;
            hoverBox.HoverBackground(Calm, Bright);
            root.Add(hoverBox);

            // 4. A toast that fades in, stays, then fades out. It must be in the hierarchy before it is animated.
            var toast = new Label("Saved") { style = { marginTop = 12, color = Text, backgroundColor = Calm, paddingTop = 6, paddingBottom = 6 } };
            toast.SetOpacity(0f);
            root.Add(toast);
            root.Add(MakeButton("Show toast", () => toast.ShowForDuration(250, 1200)));

            // 5. A border that pulses between two colours until paused.
            var pulseBox = new Label("Pulsing border") { style = { marginTop = 12, color = Text, paddingTop = 8, paddingBottom = 8 } };
            pulseBox.SetBorderWidth(2f);
            pulseBox.SetBorderColor(Calm);
            root.Add(pulseBox);
            root.Add(MakeButton("Start / stop pulse", () =>
            {
                if (_pulse != null && _pulse.isActive)
                {
                    _pulse.Pause();
                    return;
                }

                _pulse = pulseBox.AnimBorderPulse(Calm, Bright, original: Calm, color1DurationMs: 500, color2DurationMs: 500);
            }));

            // 6. The animated foldout element: children are revealed with an animated height.
            var foldout = new AnimatedFoldout { text = "Details" };
            foldout.style.marginTop = 12;
            foldout.Add(new Label("Foldout content line 1") { style = { color = Text } });
            foldout.Add(new Label("Foldout content line 2") { style = { color = Text } });
            root.Add(foldout);
        }

        private static Button MakeButton(string label, System.Action onClick)
        {
            var button = new Button(onClick) { text = label };
            button.style.marginTop = 6;
            button.style.alignSelf = Align.FlexStart;
            return button;
        }
    }
}
