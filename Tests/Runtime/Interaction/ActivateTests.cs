using System.Collections;
using FutureDigitalPlus.UIKit.DesignSystem.Behaviour;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Interaction.Tests
{
    public class ActivateTests
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

        [UnityTest]
        public IEnumerator Register_FiresOncePerSubmit_AndOncePerClick_OnAButton()
        {
            _panel = InteractionPanel.Create();
            var button = InteractionPanel.WithClasses(new Button { text = "B" }, "ds-btn");
            _panel.Screen.Add(button);
            yield return InteractionPanel.Frames();

            var calls = 0;
            using (Activate.Register(button, () => calls++))
            {
                Send.Submit(button);
                Assert.AreEqual(1, calls, "Submit");
                Send.Click(button);
                Assert.AreEqual(2, calls, "click");
            }

            Send.Submit(button);
            Send.Click(button);
            Assert.AreEqual(2, calls, "nothing after Dispose");
        }

        [UnityTest]
        public IEnumerator Register_MakesAPlainElementFocusable_AndDisposeRestoresIt()
        {
            _panel = InteractionPanel.Create();
            var item = InteractionPanel.WithClasses(new VisualElement { name = "item" }, "ds-nav-item");
            _panel.Screen.Add(item);
            yield return InteractionPanel.Frames();
            Assert.IsFalse(item.focusable);

            var registration = Activate.Register(item, () => { });
            Assert.IsTrue(item.focusable);
            item.Focus();
            Assert.AreEqual("item", _panel.FocusedName);

            registration.Dispose();
            Assert.IsFalse(item.focusable);
        }

        [UnityTest]
        public IEnumerator Register_WithFocusableFalse_LeavesTheElementAlone()
        {
            _panel = InteractionPanel.Create();
            var item = new VisualElement();
            _panel.Screen.Add(item);
            yield return InteractionPanel.Frames();

            using (Activate.Register(item, () => { }, focusable: false))
                Assert.IsFalse(item.focusable);
        }

        [UnityTest]
        public IEnumerator Register_DoesNotTurnAnAlreadyFocusableElementOffOnDispose()
        {
            _panel = InteractionPanel.Create();
            var item = new VisualElement { focusable = true };
            _panel.Screen.Add(item);
            yield return InteractionPanel.Frames();

            Activate.Register(item, () => { }).Dispose();
            Assert.IsTrue(item.focusable);
        }

        [UnityTest]
        public IEnumerator Register_Throws_ForNullArguments()
        {
            Assert.Throws<System.ArgumentNullException>(() => Activate.Register(null, () => { }));
            Assert.Throws<System.ArgumentNullException>(() => Activate.Register(new VisualElement(), null));
            yield return null;
        }

        [UnityTest]
        public IEnumerator ActivateAndTheDesignSystemBridge_AreOneReaction_InEitherOrder([Values(true, false)] bool activateFirst)
        {
            _panel = InteractionPanel.Create();
            var item = InteractionPanel.WithClasses(new VisualElement(), "ds-nav-item");
            _panel.Screen.Add(item);
            yield return InteractionPanel.Frames();

            var calls = 0;
            System.IDisposable registration = null;
            if (activateFirst) registration = Activate.Register(item, () => calls++);
            DesignSystemBehaviourBase<UIDocument>.MakeActivatable(item);
            if (!activateFirst) registration = Activate.Register(item, () => calls++);

            Send.Submit(item);
            Assert.AreEqual(1, calls, "one Submit is one action");

            Send.Click(item);
            Assert.AreEqual(2, calls, "one click is one action");
            registration.Dispose();
        }
    }
}
