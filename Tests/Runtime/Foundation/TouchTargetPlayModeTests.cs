using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation.Tests
{
    /// <summary>PlayMode test of the touch tier: every interactive element the foundation builds is at least 44 units high and wide when the player uses a finger.</summary>
    public class TouchTargetPlayModeTests : PlayModeBase
    {
        const float TouchMin = 44f;

        static string Describe(VisualElement element)
        {
            return element.GetType().Name + " '" + element.name + "' ." + string.Join(".", element.GetClasses());
        }

        static void Collect(VisualElement root, List<VisualElement> into)
        {
            foreach (VisualElement element in root.Query<VisualElement>().Build().ToList())
            {
                bool interactive = element is Button || element is ListRow;
                if (interactive && element.resolvedStyle.display != DisplayStyle.None && element.resolvedStyle.visibility != Visibility.Hidden)
                    into.Add(element);
            }
        }

        [UnityTest]
        public IEnumerator Touch_EveryFoundationControlMeetsTheTouchMinimum()
        {
            yield return CreatePanel(390, 780);
            FakeInputModeSource source = new FakeInputModeSource(UiInputMode.Touch);
            UiShell shell = CreateShell(new UiShellOptions { InputModeSource = source, TransitionsEnabled = false, StateStore = Store });
            shell.Boot("Home");

            TopBar bar = new TopBar { Title = "Settings", ShowBack = true };
            ListRow row = new ListRow { Title = "Sound", Subtitle = "On", ShowChevron = true };
            BannerView banner = new BannerView { Message = "Update available", ActionText = "Update", Dismissible = true };
            Layer(UiLayer.Overlays).Add(bar);
            Layer(UiLayer.Overlays).Add(row);
            Layer(UiLayer.Overlays).Add(banner);
            yield return FoundationPanel.Frames(3);

            List<VisualElement> found = new List<VisualElement>();
            Collect(Layer(UiLayer.Overlays), found);   // the controls the foundation builds, not the plain Button of the probe screen
            Assert.GreaterOrEqual(found.Count, 4, "back, row, action and dismiss at least");
            foreach (VisualElement element in found)
            {
                Assert.GreaterOrEqual(element.resolvedStyle.height, TouchMin - 0.5f, Describe(element) + " height");
                Assert.GreaterOrEqual(element.resolvedStyle.width, TouchMin - 0.5f, Describe(element) + " width");
            }

            shell.Context.Dialogs.Confirm(UiText.FromLiteral("Restart?"), UiText.FromLiteral("Progress is lost."), answer => { });
            yield return FoundationPanel.Frames(3);
            List<Button> dialogButtons = Layer(UiLayer.Dialogs).Query<Button>().ToList();
            Assert.AreEqual(2, dialogButtons.Count);
            foreach (Button button in dialogButtons)
            {
                Assert.GreaterOrEqual(button.resolvedStyle.height, TouchMin - 0.5f, button.text + " height");
                Assert.GreaterOrEqual(button.resolvedStyle.width, TouchMin - 0.5f, button.text + " width");
            }
        }
    }
}
