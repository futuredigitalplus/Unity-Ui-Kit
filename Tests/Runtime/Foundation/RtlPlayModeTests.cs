using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation.Tests
{
    /// <summary>
    /// Right-to-left layout of the foundation views: with <c>fdp-rtl</c> above them, a <see cref="ListRow"/> and a <see cref="TopBar"/> list their parts
    /// from the right. Each test measures the order of the parts left to right in a left-to-right root and again with the class, and expects the mirror image.
    /// </summary>
    public class RtlPlayModeTests : PlayModeBase
    {
        const string RtlClass = "fdp-rtl";

        static float X(VisualElement element)
        {
            return element.worldBound.center.x;
        }

        [UnityTest]
        public IEnumerator ListRow_ListsIconTextValueAndChevronFromTheRight()
        {
            yield return CreatePanel(480, 600);
            UiShell shell = CreateShell();
            shell.Boot("Home");
            ListRow row = new ListRow { Title = "Language", Value = "English", ShowChevron = true };
            row.HasIcon = true;
            row.Icon.AddToClassList("ds-icon--settings");
            Layer(UiLayer.Overlays).Add(row);
            yield return FoundationPanel.Frames(3);

            VisualElement body = row.Q(className: "fdp-list-row__body");
            VisualElement valueBox = row.Q(className: "fdp-list-row__value-box");
            VisualElement[] parts = { row.Icon, body, valueBox, row.Chevron };
            float[] ltr = new float[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                ltr[i] = X(parts[i]);
            for (int i = 1; i < parts.Length; i++)
                Assert.Less(ltr[i - 1], ltr[i], "left to right: part " + (i - 1) + " is left of part " + i);

            Panel.Root.AddToClassList(RtlClass);
            yield return FoundationPanel.Frames(3);
            float[] rtl = new float[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                rtl[i] = X(parts[i]);
            for (int i = 1; i < parts.Length; i++)
                Assert.Greater(rtl[i - 1], rtl[i], "right to left: part " + (i - 1) + " is right of part " + i);
            Assert.AreEqual(Align.Center, row.resolvedStyle.alignItems);
            Assert.AreEqual(0f, row.Icon.resolvedStyle.marginRight, 0.5f, "the gap moved to the other side of the icon");
            Assert.Greater(row.Icon.resolvedStyle.marginLeft, 0f);
            Assert.AreEqual(0f, row.Chevron.resolvedStyle.marginLeft, 0.5f);
            Assert.Greater(row.Chevron.resolvedStyle.marginRight, 0f);
        }

        [UnityTest]
        public IEnumerator ListRow_GoesBackToLeftToRight_WhenTheClassIsRemoved()
        {
            yield return CreatePanel(480, 600);
            UiShell shell = CreateShell();
            shell.Boot("Home");
            ListRow row = new ListRow { Title = "Sound", ShowChevron = true };
            Layer(UiLayer.Overlays).Add(row);
            Panel.Root.AddToClassList(RtlClass);
            yield return FoundationPanel.Frames(3);
            Assert.Greater(X(row.TitleLabel), X(row.Chevron));
            Panel.Root.RemoveFromClassList(RtlClass);
            yield return FoundationPanel.Frames(3);
            Assert.Less(X(row.TitleLabel), X(row.Chevron));
        }

        [UnityTest]
        public IEnumerator TopBar_PutsTheBackButtonAtTheRightAndTheActionSlotAtTheLeft()
        {
            yield return CreatePanel(480, 600);
            UiShell shell = CreateShell();
            shell.Boot("Home");
            TopBar bar = new TopBar { Title = "Settings", ShowBack = true };
            Button action = new Button { text = "Help" };
            bar.End.Add(action);
            Layer(UiLayer.Overlays).Add(bar);
            yield return FoundationPanel.Frames(3);

            VisualElement back = bar.BackButton;
            Assert.IsNotNull(back, "the back button");
            Assert.Less(X(back), X(bar.TitleLabel), "left to right: back, title");
            Assert.Less(X(bar.TitleLabel), X(action), "left to right: title, action");

            Panel.Root.AddToClassList(RtlClass);
            yield return FoundationPanel.Frames(3);
            Assert.Greater(X(back), X(bar.TitleLabel), "right to left: back is right of the title");
            Assert.Greater(X(bar.TitleLabel), X(action), "right to left: the action is left of the title");
        }
    }
}
