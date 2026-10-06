using System.Collections;
using FutureDigitalPlus.UIKit.Foundation;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Language.Tests
{
    public class LocalizedTextTests : LanguagePanelFixture
    {
        private DictionaryTextSource _source;
        private UiLocalization _localization;

        [SetUp]
        public void SetUp()
        {
            _source = new DictionaryTextSource("en");
            _source.Add("en", "UI", "PLAY", "Play");
            _source.Add("de", "UI", "PLAY", "Spielen");
            _source.Add("en", "UI", "COINS", "{0} coins");
            _source.Add("de", "UI", "COINS", "{0} Münzen");
            _localization = new UiLocalization(_source, new[] { "UI" }) { LogMissingTables = false };
        }

        [TearDown]
        public void TearDown()
        {
            UiLocalization.Default.Backend = null;
            UiLocalization.Default.DefaultTables = null;
            UiLocalization.Default.LogMissingTables = true;
        }

        [UnityTest]
        public IEnumerator BindText_ShowsTheTextAtOnce_AndFollowsTheLanguage()
        {
            yield return CreatePanel();
            var label = new Label();
            Root.Add(label);

            label.BindText(UiText.FromKey("PLAY"), _localization);
            Assert.AreEqual("Play", label.text);

            _source.SetLocale("de");
            Assert.AreEqual("Spielen", label.text);
        }

        [UnityTest]
        public IEnumerator Arguments_FillThePlaceholders_InEveryLanguage()
        {
            yield return CreatePanel();
            var label = new Label();
            Root.Add(label);

            label.BindText(UiText.FromKey("COINS", 12), _localization);
            Assert.AreEqual("12 coins", label.text);

            _source.SetLocale("de");
            Assert.AreEqual("12 Münzen", label.text);
        }

        [UnityTest]
        public IEnumerator AnElementThatLeftThePanel_IsNotUpdated_UntilItIsBackOnIt()
        {
            yield return CreatePanel();
            var label = new Label();
            Root.Add(label);
            label.BindText(UiText.FromKey("PLAY"), _localization);

            label.RemoveFromHierarchy();
            _source.SetLocale("de");
            Assert.AreEqual("Play", label.text, "a detached element is not written to");

            Root.Add(label);
            Assert.AreEqual("Spielen", label.text, "attaching resolves again");
        }

        [UnityTest]
        public IEnumerator ABinderDoesNotOverwriteATextSetByHand_BeforeItsFirstSetText()
        {
            yield return CreatePanel();
            var label = new Label { text = "set by a screen" };
            var binder = new LocalizedTextBinder(label, value => label.text = value, _localization);

            Root.Add(label);
            Assert.AreEqual("set by a screen", label.text, "attaching alone must not blank the text");

            binder.SetText(UiText.FromKey("PLAY"));
            Assert.AreEqual("Play", label.text);
            Assert.AreEqual(UiText.FromKey("PLAY"), binder.Text);
        }

        [UnityTest]
        public IEnumerator WithoutABackend_TheFallbackOrAPlaceholderShows_AndNothingIsLogged()
        {
            yield return CreatePanel();
            var none = new UiLocalization { LogMissingTables = false };
            var withFallback = new Label();
            var withoutFallback = new Label();
            Root.Add(withFallback);
            Root.Add(withoutFallback);

            withFallback.BindText(UiText.FromKey("HOME-PLAY").WithFallback("Play"), none);
            withoutFallback.BindText(UiText.FromKey("HOME-PLAY"), none);

            Assert.AreEqual("Play", withFallback.text);
            Assert.AreEqual("[HOME-PLAY]", withoutFallback.text);
        }

        [UnityTest]
        public IEnumerator BindKeyAndTheSetterOverloadWork_ForAnyTextProperty()
        {
            yield return CreatePanel();
            var button = new Button();
            var toggle = new Toggle();
            Root.Add(button);
            Root.Add(toggle);
            UiLocalization.Default.Backend = _source;
            UiLocalization.Default.DefaultTables = new[] { "UI" };

            button.BindKey("PLAY");
            toggle.BindText(UiText.FromKey("PLAY"), value => toggle.label = value);

            Assert.AreEqual("Play", button.text);
            Assert.AreEqual("Play", toggle.label);
            _source.SetLocale("de");
            Assert.AreEqual("Spielen", button.text);
            Assert.AreEqual("Spielen", toggle.label);
        }

        [UnityTest]
        public IEnumerator LocalizedLabelAndButton_ReadKeyTableFallbackAndArguments()
        {
            yield return CreatePanel();
            UiLocalization.Default.Backend = _source;
            UiLocalization.Default.LogMissingTables = false;
            UiLocalization.Default.DefaultTables = new[] { "UI" };

            var label = new LocalizedLabel { Key = "PLAY", Fallback = "Play?" };
            var button = new LocalizedButton { Key = "UI:COINS" };
            var qualified = new LocalizedLabel { Table = "UI", Key = "PLAY" };
            var missing = new LocalizedLabel { Key = "NOPE", Fallback = "Shown instead" };
            Root.Add(label);
            Root.Add(button);
            Root.Add(qualified);
            Root.Add(missing);

            button.SetArgs(5);

            Assert.AreEqual("Play", label.text);
            Assert.AreEqual("5 coins", button.text);
            Assert.AreEqual("Play", qualified.text);
            Assert.AreEqual("Shown instead", missing.text);

            _source.SetLocale("de");
            Assert.AreEqual("Spielen", label.text);
            Assert.AreEqual("5 Münzen", button.text);
        }

        [UnityTest]
        public IEnumerator ALocalizedLabelWithoutAKey_KeepsItsTextUntilAKeyIsSet()
        {
            yield return CreatePanel();
            var label = new LocalizedLabel { text = "typed in UXML" };
            Root.Add(label);
            Assert.AreEqual("typed in UXML", label.text);
        }

        [UnityTest]
        public IEnumerator LanguageStyles_AddTheSheetOnce()
        {
            yield return CreatePanel(installRtl: false);
            var before = Root.parent.styleSheets.count;

            Assert.IsTrue(LanguageStyles.Install(Root.parent));
            Assert.IsTrue(LanguageStyles.Install(Root.parent));

            Assert.AreEqual(before + 1, Root.parent.styleSheets.count);
            Assert.IsFalse(LanguageStyles.Install(null));
        }
    }
}
