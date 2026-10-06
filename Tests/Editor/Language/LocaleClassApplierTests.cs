using System.Linq;
using FutureDigitalPlus.UIKit.Foundation;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Language.Tests
{
    public class LocaleClassApplierTests
    {
        private static int LanguageClasses(VisualElement root) =>
            root.GetClasses().Count(c => c.StartsWith(LocaleStyle.ClassPrefix));

        [Test]
        public void ALeftToRightLanguage_GetsItsClass_AndNoRtlClass()
        {
            var root = new VisualElement();

            var rtl = LocaleClassApplier.Apply(root, "de");

            Assert.IsFalse(rtl);
            Assert.IsTrue(root.ClassListContains(LocaleStyle.ClassFor("de")));
            Assert.IsFalse(root.ClassListContains(LocaleClassApplier.RtlClass));
            Assert.AreEqual(LanguageDirection.LTR, root.languageDirection);
        }

        [Test]
        public void ARightToLeftLanguage_SwitchesTheMirroredLayoutOn()
        {
            var root = new VisualElement();

            var rtl = LocaleClassApplier.Apply(root, "ar");

            Assert.IsTrue(rtl);
            Assert.IsTrue(root.ClassListContains(LocaleStyle.ClassFor("ar")));
            Assert.IsTrue(root.ClassListContains("fdp-rtl"));
            Assert.AreEqual(LanguageDirection.RTL, root.languageDirection);
        }

        [Test]
        public void ChangingTheLanguage_RemovesTheOldClasses_AndKeepsOtherClasses()
        {
            var root = new VisualElement();
            root.AddToClassList("ds-root");

            LocaleClassApplier.Apply(root, "ar");
            LocaleClassApplier.Apply(root, "zh-Hant");

            Assert.AreEqual(1, LanguageClasses(root));
            Assert.IsTrue(root.ClassListContains(LocaleStyle.ClassFor("zh-Hant")));
            Assert.IsFalse(root.ClassListContains(LocaleClassApplier.RtlClass));
            Assert.AreEqual(LanguageDirection.LTR, root.languageDirection);
            Assert.IsTrue(root.ClassListContains("ds-root"));
        }

        [Test]
        public void ApplyingTwiceChangesNothing()
        {
            var root = new VisualElement();
            LocaleClassApplier.Apply(root, "he");
            var before = string.Join(" ", root.GetClasses());

            LocaleClassApplier.Apply(root, "he");

            Assert.AreEqual(before, string.Join(" ", root.GetClasses()));
        }

        [Test]
        public void ABlankLanguageIsEnglish_AndANullRootIsIgnored()
        {
            var root = new VisualElement();
            Assert.IsFalse(LocaleClassApplier.Apply(root, null));
            Assert.IsTrue(root.ClassListContains(LocaleStyle.ClassFor("en")));
            Assert.IsFalse(LocaleClassApplier.Apply(null, "ar"));
        }

        [Test]
        public void BindFollowsTheLocalization_UntilDisposed()
        {
            var source = new DictionaryTextSource("en");
            var localization = new UiLocalization(source);
            var root = new VisualElement();

            using (LocaleClassApplier.Bind(root, localization))
            {
                Assert.IsTrue(root.ClassListContains(LocaleStyle.ClassFor("en")), "applied at once");

                source.SetLocale("ar");
                Assert.IsTrue(root.ClassListContains(LocaleClassApplier.RtlClass));
                Assert.IsTrue(root.ClassListContains(LocaleStyle.ClassFor("ar")));

                source.SetLocale("fr");
                Assert.IsFalse(root.ClassListContains(LocaleClassApplier.RtlClass));
            }

            source.SetLocale("ar");
            Assert.IsFalse(root.ClassListContains(LocaleClassApplier.RtlClass), "no longer followed after Dispose");
            Assert.IsTrue(root.ClassListContains(LocaleStyle.ClassFor("fr")));
        }

        [Test]
        public void BindRejectsANullRoot()
        {
            Assert.Throws<System.ArgumentNullException>(() => LocaleClassApplier.Bind(null));
        }
    }
}
