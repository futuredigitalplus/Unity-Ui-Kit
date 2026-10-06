using System.Collections;
using System.Collections.Generic;
using FutureDigitalPlus.UIKit.Components;
using FutureDigitalPlus.UIKit.Components.Testing;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Interaction.Tests
{
    /// <summary>The event interfaces the Interaction work package added to the Components module are registered by the bundled source generator like the upstream ones.</summary>
    public partial class ComponentEventInterfaceTests
    {
        private partial class Probe : UIComponent,
            IOnNavigationSubmit, IOnNavigationCancel, IOnFocusIn, IOnFocusOut, IOnPointerDown, IOnPointerUp, IOnPointerMove, IOnKeyDown
        {
            public readonly List<string> Seen = new List<string>();

            public void OnNavigationSubmit(NavigationSubmitEvent evt) => Seen.Add("submit");
            public void OnNavigationCancel(NavigationCancelEvent evt) => Seen.Add("cancel");
            public void OnFocusIn(FocusInEvent evt) => Seen.Add("focusin");
            public void OnFocusOut(FocusOutEvent evt) => Seen.Add("focusout");
            public void OnPointerDown(PointerDownEvent evt) => Seen.Add("down");
            public void OnPointerUp(PointerUpEvent evt) => Seen.Add("up");
            public void OnPointerMove(PointerMoveEvent evt) => Seen.Add("move");
            public void OnKeyDown(KeyDownEvent evt) => Seen.Add("key");
        }

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

        // The Components module's own Task-to-IEnumerator helper is internal to the package, so polling is done here.
        private static IEnumerator WaitFor(System.Threading.Tasks.Task task)
        {
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception;
        }

        [UnityTest]
        public IEnumerator EveryNewInterface_RegistersItsCallback()
        {
            _panel = InteractionPanel.Create();
            var probe = new TestBed<Probe>().Instantiate();
            yield return WaitFor(probe.Initialize());
            probe.focusable = true;
            _panel.Screen.Add(probe);
            yield return InteractionPanel.Frames();

            Send.Submit(probe);
            Send.Cancel(probe);
            Send.PointerDown(probe, touch: false);
            Send.PointerUp(probe, touch: false);
            Send.PointerMove(probe, touch: false);
            using (var key = KeyDownEvent.GetPooled('a', KeyCode.A, EventModifiers.None))
            {
                key.target = probe;
                probe.SendEvent(key);
            }
            probe.Focus();
            probe.Blur();

            CollectionAssert.AreEquivalent(
                new[] { "submit", "cancel", "down", "up", "move", "key", "focusin", "focusout" },
                probe.Seen);
        }
    }
}
