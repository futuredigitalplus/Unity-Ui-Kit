using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Interaction.Tests
{
    /// <summary>Focus.uss: a ring on every control family, only in navigation mode, without moving the layout.</summary>
    public class FocusRingTests
    {
        private InteractionPanel _panel;

        [SetUp]
        public void SetUp() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            _panel?.Dispose();
            _panel = null;
        }

        private struct Family
        {
            public string Name;
            public VisualElement Focus;   // the element that takes focus
            public VisualElement Part;    // the element whose border shows the ring
            public float ShiftBudget;     // how far the next sibling may move when the ring appears
        }

        private static Family Make(VisualElement panelScreen, string name, VisualElement focus, string partClass = null, float shiftBudget = 0.5f)
        {
            var wrap = new VisualElement();
            wrap.style.flexDirection = FlexDirection.Row;
            wrap.style.marginRight = 20;
            wrap.style.marginBottom = 10;
            wrap.Add(new Label("<"));
            wrap.Add(focus);
            wrap.Add(new Label(">") { name = "after" });
            panelScreen.Add(wrap);
            return new Family { Name = name, Focus = focus, Part = partClass == null ? focus : focus.Q(className: partClass), ShiftBudget = shiftBudget };
        }

        private static bool SameColor(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) < 0.02f && Mathf.Abs(a.g - b.g) < 0.02f && Mathf.Abs(a.b - b.b) < 0.02f && a.a > 0.9f;

        private List<Family> BuildFamilies()
        {
            var s = _panel.Screen;
            s.style.flexDirection = FlexDirection.Row;
            s.style.flexWrap = Wrap.Wrap;
            s.style.alignItems = Align.FlexStart;
            var list = new List<Family>
            {
                Make(s, "ds-btn", InteractionPanel.WithClasses(new Button { text = "Play" }, "ds-btn ds-btn--primary"), shiftBudget: 2.5f),
                Make(s, "ds-btn--ghost", InteractionPanel.WithClasses(new Button { text = "Play" }, "ds-btn ds-btn--ghost"), shiftBudget: 2.5f),
                Make(s, "ds-tab", InteractionPanel.WithClasses(new Button { text = "Tab" }, "ds-tab")),
                Make(s, "ds-chip", InteractionPanel.WithClasses(new Button { text = "Chip" }, "ds-chip"), shiftBudget: 2.5f),
                Make(s, "ds-nav-item", InteractionPanel.WithClasses(new VisualElement { focusable = true }, "ds-nav-item")),
                Make(s, "ds-toggle", InteractionPanel.WithClasses(new Toggle(), "ds-toggle"), "unity-toggle__checkmark"),
                Make(s, "ds-check", InteractionPanel.WithClasses(new Toggle(), "ds-check"), "unity-toggle__checkmark"),
                Make(s, "ds-radio", InteractionPanel.WithClasses(new RadioButton(), "ds-radio"), "unity-radio-button__checkmark-background"),
                Make(s, "ds-slider", InteractionPanel.WithClasses(new Slider(0, 1), "ds-slider"), "unity-base-slider__dragger"),
                Make(s, "ds-dropdown", InteractionPanel.WithClasses(new DropdownField(new List<string> { "a" }, 0), "ds-dropdown"), "unity-base-popup-field__input"),
                Make(s, "ds-input", InteractionPanel.WithClasses(new TextField(), "ds-input"), "unity-base-text-field__input"),
            };
            return list;
        }

        [UnityTest]
        public IEnumerator Ring_ShowsOnEveryFamilyInNavigationMode_AndOnlyThen()
        {
            _panel = InteractionPanel.Create(1200, 800);
            var families = BuildFamilies();
            yield return InteractionPanel.Frames(4);

            var ring = _panel.Screen.resolvedStyle.color;   // the ring is the theme's text colour
            using (var profile = InputProfile.Bind(_panel.Root))
            {
                profile.Force(InputDeviceKind.Pointer);
                foreach (var f in families)
                {
                    f.Focus.Focus();
                    yield return new WaitForSecondsRealtime(0.3f);   // longer than the design system's transitions
                    Assert.IsFalse(SameColor(f.Part.resolvedStyle.borderTopColor, ring), $"{f.Name}: no ring while the last device was a pointer");
                    f.Focus.Blur();
                }

                profile.Force(InputDeviceKind.Keyboard);
                yield return new WaitForSecondsRealtime(0.3f);
                foreach (var f in families)
                {
                    var after = f.Focus.parent.Q("after");
                    var before = after.worldBound.x;
                    f.Focus.Focus();
                    // The border colour transitions; a loaded machine updates the panel late, so wait until it has arrived (two seconds at most) instead of for a fixed time.
                    var until = Time.realtimeSinceStartup + 2f;
                    while (Time.realtimeSinceStartup < until && !SameColor(f.Part.resolvedStyle.borderTopColor, ring)) yield return null;
                    Assert.IsTrue(SameColor(f.Part.resolvedStyle.borderTopColor, ring), $"{f.Name}: ring in navigation mode, got {f.Part.resolvedStyle.borderTopColor}, want {ring}");
                    Assert.LessOrEqual(Mathf.Abs(after.worldBound.x - before), f.ShiftBudget, $"{f.Name}: focusing moved the next sibling");
                    f.Focus.Blur();
                    yield return null;
                }
            }
        }
    }
}
