using System.Collections;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Applier;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Data;
using FutureDigitalPlus.UIKit.Extras;
using NUnit.Framework;
using FutureDigitalPlus.UIKit.Components;
using FutureDigitalPlus.UIKit.Components.Testing;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Tests
{
    /// <summary>
    /// One screen that uses all four modules: a Components <see cref="UIComponent"/> built from UXML, styled by the
    /// DesignSystem theme through the facade, a button animated with the Animation module and an Extras tooltip.
    /// </summary>
    public partial class CrossModuleTests
    {
        [Layout("FutureDigitalPlus/UIKit/Tests/CrossModuleScreen")]
        private partial class SampleScreen : UIComponent
        {
            [Query("status")] public Label Status;
            [Query("action-button")] public Button Action;

            public override void OnInit()
            {
                Action.clicked += () => Status.text = "clicked";
            }
        }

        private PanelHarness _panel;

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            _panel?.Dispose();
        }

        // FutureDigitalPlus.UIKit.Components' own Task-to-IEnumerator helper is internal to the package, so polling is done here.
        private static IEnumerator WaitFor(System.Threading.Tasks.Task task)
        {
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception;
        }

        [UnityTest]
        public IEnumerator SampleScreen_ThemedQueriedAnimatedAndWithTooltip()
        {
            if (!Application.isPlaying) Assert.Ignore("Needs a runtime panel (Play Mode).");
            LogAssert.ignoreFailingMessages = true;
            _panel = PanelHarness.Create();
            yield return PanelHarness.Frames();

            // Components: the [Layout] UXML is loaded and [Query] fields are filled by the source generator.
            var screen = new TestBed<SampleScreen>().Instantiate();
            yield return WaitFor(screen.Initialize());
            Assert.IsNotNull(screen.Status, "[Query] populated the label");
            Assert.IsNotNull(screen.Action, "[Query] populated the button");
            _panel.Root.Add(screen);

            // DesignSystem through the facade: light theme + mobile tier.
            var light = Resources.Load<ThemeData>("FutureDigitalPlus/UIKit/DesignSystem/Themes/Light");
            var kit = UIKitInstaller.Install(_panel.Root, new UIKitOptions { Theme = light, Mobile = MobileMode.On, ApplySafeArea = false });
            yield return PanelHarness.Frames();
            Assert.AreSame(light, ThemeRuntime.WornBy(_panel.Root));
            Assert.AreEqual(48f, screen.Action.resolvedStyle.height, 0.5f, "mobile tier makes the .ds-btn 48 tall");

            // Extras: a tooltip attached to the button through its manipulator.
            var tooltip = new Tooltip();
            _panel.Root.Add(tooltip);
            screen.Action.AddManipulator(new TooltipManipulator(tooltip));
            tooltip.Show(screen.Action);
            Assert.AreEqual(Visibility.Visible, tooltip.style.visibility.value);
            tooltip.Close();

            // Animation through the facade helper, then the click handler of the component.
            var faded = false;
            screen.Action.FadeIn(60, () => faded = true);
            var end = Time.realtimeSinceStartup + 5f;
            while (!faded && Time.realtimeSinceStartup < end) yield return null;
            Assert.IsTrue(faded, "animation completed");

            using (var submit = NavigationSubmitEvent.GetPooled())
            {
                submit.target = screen.Action;
                screen.Action.SendEvent(submit);
            }
            Assert.AreEqual("clicked", screen.Status.text);
            kit.Dispose();
        }
    }
}
