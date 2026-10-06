using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Language.Tests
{
    /// <summary>
    /// Right-to-left layout: with <c>fdp-rtl</c> on the root and <c>Rtl.uss</c> installed, the design-system components list their
    /// parts from the right. Each test builds one component, measures the order of its parts left to right in a left-to-right
    /// root and again after <see cref="LocaleClassApplier.Apply"/> with a right-to-left language, and expects the mirror image.
    /// </summary>
    public class RtlLayoutTests : LanguagePanelFixture
    {
        private VisualElement _screen;

        private IEnumerator Build(params VisualElement[] children)
        {
            yield return CreatePanel();
            _screen = new VisualElement();
            _screen.AddToClassList("ds-root");
            _screen.style.width = 480;
            _screen.style.flexGrow = 0;
            foreach (var child in children) _screen.Add(child);
            Root.Add(_screen);
        }

        private IEnumerator Measure(VisualElement[] parts, System.Action<float[]> ltr, System.Action<float[]> rtl)
        {
            yield return Settle();
            ltr(Xs(parts));

            LocaleClassApplier.Apply(Root, "ar");
            yield return Settle();
            rtl(Xs(parts));

            LocaleClassApplier.Apply(Root, "en");
            yield return Settle();
        }

        private static float[] Xs(VisualElement[] parts)
        {
            var xs = new float[parts.Length];
            for (var i = 0; i < parts.Length; i++) xs[i] = parts[i].worldBound.center.x;
            return xs;
        }

        private static void AssertAscending(float[] xs, string what)
        {
            for (var i = 1; i < xs.Length; i++)
                Assert.Less(xs[i - 1], xs[i], what + ": part " + (i - 1) + " is left of part " + i + " (" + string.Join(", ", xs) + ")");
        }

        private static void AssertDescending(float[] xs, string what)
        {
            for (var i = 1; i < xs.Length; i++)
                Assert.Greater(xs[i - 1], xs[i], what + ": part " + (i - 1) + " is right of part " + i + " (" + string.Join(", ", xs) + ")");
        }

        private IEnumerator AssertMirrored(string what, params VisualElement[] parts)
        {
            float[] left = null, right = null;
            yield return Measure(parts, xs => left = xs, xs => right = xs);
            AssertAscending(left, what + " left to right");
            AssertDescending(right, what + " right to left");
        }

        private static Button Tab(string text, params string[] classes)
        {
            var tab = new Button { text = text };
            tab.AddToClassList("ds-tab");
            foreach (var c in classes) tab.AddToClassList(c);
            return tab;
        }

        [UnityTest]
        public IEnumerator Tabs_ListFromTheRight()
        {
            var tabs = new VisualElement();
            tabs.AddToClassList("ds-tabs");
            var parts = new[] { Tab("One", "is-active"), Tab("Two"), Tab("Three") };
            foreach (var part in parts) tabs.Add(part);

            yield return Build(tabs);
            yield return AssertMirrored("tabs", parts);
        }

        [UnityTest]
        public IEnumerator AButtonWithAnIcon_PutsTheIconOnTheRight()
        {
            var button = new Button();
            button.AddToClassList("ds-btn");
            button.AddToClassList("ds-btn--primary");
            button.AddToClassList("ds-btn--with-icon");
            var icon = new VisualElement();
            icon.AddToClassList("ds-icon");
            icon.AddToClassList("ds-icon--check");
            icon.AddToClassList("ds-btn__icon");
            var label = new Label("Save");
            button.Add(icon);
            button.Add(label);

            yield return Build(button);
            yield return AssertMirrored("icon button", icon, label);
        }

        [UnityTest]
        public IEnumerator ASearchField_PutsItsIconOnTheRight()
        {
            var search = new VisualElement();
            search.AddToClassList("ds-search");
            var icon = new VisualElement();
            icon.AddToClassList("ds-icon");
            icon.AddToClassList("ds-icon--sm");
            icon.AddToClassList("ds-icon--search");
            icon.AddToClassList("ds-search__icon");
            var field = new TextField { value = "Search" };
            field.AddToClassList("ds-search__field");
            search.Add(icon);
            search.Add(field);

            yield return Build(search);
            yield return AssertMirrored("search", icon, field);
        }

        [UnityTest]
        public IEnumerator DialogButtons_ListFromTheRight()
        {
            var actions = new VisualElement();
            actions.AddToClassList("ds-dialog__actions");
            var cancel = new Button { text = "Cancel" };
            cancel.AddToClassList("ds-btn");
            var ok = new Button { text = "OK" };
            ok.AddToClassList("ds-btn");
            ok.AddToClassList("ds-btn--primary");
            actions.Add(cancel);
            actions.Add(ok);

            yield return Build(actions);
            yield return AssertMirrored("dialog buttons", cancel, ok);
        }

        [UnityTest]
        public IEnumerator AToast_PutsItsIconOnTheRight()
        {
            var toast = new VisualElement();
            toast.AddToClassList("ds-toast");
            toast.AddToClassList("ds-toast--success");
            var icon = new VisualElement();
            icon.AddToClassList("ds-icon");
            icon.AddToClassList("ds-icon--sm");
            icon.AddToClassList("ds-icon--check");
            icon.AddToClassList("ds-toast__icon");
            var message = new Label("Saved successfully.");
            message.AddToClassList("ds-toast__message");
            toast.Add(icon);
            toast.Add(message);

            yield return Build(toast);
            yield return AssertMirrored("toast", icon, message);
        }

        [UnityTest]
        public IEnumerator AMeterFill_GrowsFromTheRight()
        {
            var meter = new VisualElement();
            meter.AddToClassList("ds-meter");
            var fill = new VisualElement();
            fill.AddToClassList("ds-meter__fill");
            fill.style.width = new Length(50, LengthUnit.Percent);
            meter.Add(fill);

            yield return Build(meter);
            yield return Settle();
            Assert.Less(fill.worldBound.xMin - meter.worldBound.xMin, 4f, "left to right: the fill starts at the left edge");

            LocaleClassApplier.Apply(Root, "he");
            yield return Settle();
            Assert.Less(meter.worldBound.xMax - fill.worldBound.xMax, 4f, "right to left: the fill ends at the right edge");
            Assert.Greater(fill.worldBound.xMin - meter.worldBound.xMin, meter.worldBound.width * 0.25f, "and it is the right half");
        }

        [UnityTest]
        public IEnumerator LeftToRight_IsUnchangedByTheSheet()
        {
            var tabs = new VisualElement();
            tabs.AddToClassList("ds-tabs");
            var parts = new[] { Tab("One"), Tab("Two") };
            foreach (var part in parts) tabs.Add(part);
            yield return Build(tabs);
            yield return Settle();
            var before = Xs(parts);

            LocaleClassApplier.Apply(Root, "ar");
            yield return Settle();
            LocaleClassApplier.Apply(Root, "de");
            yield return Settle();

            CollectionAssert.AreEqual(before, Xs(parts), "back to a left-to-right language restores the original layout");
        }
    }
}
