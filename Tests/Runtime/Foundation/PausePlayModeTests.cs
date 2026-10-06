using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation.Tests
{
    /// <summary>PlayMode tests of the ready pause menu: the time-scale hook, the screens above it and the buttons.</summary>
    public class PausePlayModeTests : PlayModeBase
    {
        [UnityTest]
        public IEnumerator Pause_OpeningPausesTime_AndBackRestoresThePreviousScale()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            PauseMenu.Register(shell.Catalog);
            shell.Boot("Home");
            Time.timeScale = 0.5f;

            shell.Router.Push(PauseMenu.DefaultId);
            yield return FoundationPanel.Frames(2);
            Assert.AreEqual(0f, Time.timeScale);

            Assert.IsTrue(shell.RequestBack());
            yield return FoundationPanel.Frames(2);
            Assert.AreEqual("Home", shell.Router.Current);
            Assert.AreEqual(0.5f, Time.timeScale, "the value from before the menu came back");
        }

        [UnityTest]
        public IEnumerator Pause_AScreenPushedOverTheMenuKeepsTheGamePaused()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            PauseMenu.Register(shell.Catalog);
            shell.Boot("Home");
            shell.Router.Push(PauseMenu.DefaultId);
            shell.Router.Push("Settings");
            yield return FoundationPanel.Frames(2);
            Assert.AreEqual(0f, Time.timeScale);
            shell.Router.Pop();
            yield return FoundationPanel.Frames(2);
            Assert.AreEqual(0f, Time.timeScale, "the menu is on top again");
            shell.Router.Pop();
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator Pause_TheResumeButtonClosesTheMenu_AndAnItemRunsItsAction()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            PauseMenuOptions options = new PauseMenuOptions();
            int resumed = 0;
            options.Resumed = () => resumed++;
            UiContext seen = null;
            options.Items.Add(new PauseMenuItem(UiText.FromLiteral("Quit"), ui => seen = ui) { Destructive = true });
            PauseMenu.Register(shell.Catalog, options);
            shell.Boot("Home");
            shell.Router.Push(PauseMenu.DefaultId);
            yield return FoundationPanel.Frames(3);

            var buttons = Layer(UiLayer.Screens).Query<Button>(className: "fdp-pause__button").ToList();
            Assert.AreEqual(2, buttons.Count, "Resume and one item");
            Assert.IsTrue(buttons[1].ClassListContains("ds-btn--danger"));
            Panel.Click(buttons[1]);
            Assert.AreSame(shell.Context, seen);
            Assert.AreEqual(PauseMenu.DefaultId, shell.Router.Current, "an item does not close the menu by itself");

            Panel.Click(buttons[0]);
            yield return FoundationPanel.Frames(2);
            Assert.AreEqual("Home", shell.Router.Current);
            Assert.AreEqual(1, resumed);
        }

        [UnityTest]
        public IEnumerator Pause_ACustomHookReplacesTheTimeScale()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            int pauses = 0;
            int resumes = 0;
            PauseMenuOptions options = new PauseMenuOptions { PauseHook = paused => { if (paused) pauses++; else resumes++; } };
            PauseMenu.Register(shell.Catalog, options);
            shell.Boot("Home");
            shell.Router.Push(PauseMenu.DefaultId);
            yield return FoundationPanel.Frames(2);
            Assert.AreEqual(1, pauses);
            Assert.AreEqual(1f, Time.timeScale, "the hook owns pausing");
            shell.Router.Pop();
            Assert.AreEqual(1, resumes);
        }

        [UnityTest]
        public IEnumerator Pause_WithPauseTimeOff_LeavesTheTimeScaleAlone()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            PauseMenu.Register(shell.Catalog, new PauseMenuOptions { PauseTime = false });
            shell.Boot("Home");
            shell.Router.Push(PauseMenu.DefaultId);
            yield return FoundationPanel.Frames(2);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator Pause_DisposingTheShellWhileOpenRestoresTime()
        {
            yield return CreatePanel();
            UiShell shell = CreateShell();
            PauseMenu.Register(shell.Catalog);
            shell.Boot("Home");
            shell.Router.Push(PauseMenu.DefaultId);
            yield return FoundationPanel.Frames(2);
            Assert.AreEqual(0f, Time.timeScale);
            shell.Dispose();
            Assert.AreEqual(1f, Time.timeScale);
        }
    }
}
