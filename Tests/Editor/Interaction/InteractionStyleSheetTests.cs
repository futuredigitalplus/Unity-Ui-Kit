using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Interaction.Tests
{
    /// <summary>
    /// Guards the two sheets of the module against drift: Focus.uss and A11y.uss select design system classes by name, and a rename or removal in the design system would
    /// silently turn a rule into a no-op (USS ignores selectors that match nothing).
    /// </summary>
    public class InteractionStyleSheetTests
    {
        private static readonly Regex ClassName = new Regex(@"\.(ds-[A-Za-z0-9_-]+)", RegexOptions.Compiled);

        private static string Text(string resourcePath)
        {
            var sheet = Resources.Load<StyleSheet>(resourcePath);
            Assert.IsNotNull(sheet, resourcePath + " must load from Resources");
            return File.ReadAllText(AssetDatabase.GetAssetPath(sheet));
        }

        private static string AllDesignSystemSheets()
        {
            var text = new System.Text.StringBuilder();
            foreach (var guid in AssetDatabase.FindAssets("t:StyleSheet", new[] { FutureDigitalPlus.UIKit.Location.KitLocation.AssetPath("Runtime/DesignSystem") }))
                text.AppendLine(File.ReadAllText(AssetDatabase.GUIDToAssetPath(guid)));
            return text.ToString();
        }

        [Test]
        public void BothSheetsLoadAndInstall()
        {
            Assert.IsNotNull(Resources.Load<StyleSheet>(InteractionStyles.FocusResource));
            Assert.IsNotNull(Resources.Load<StyleSheet>(InteractionStyles.AccessibilityResource));
            var root = new VisualElement();
            Assert.IsTrue(InteractionStyles.Install(root));
            Assert.AreEqual(2, root.styleSheets.count);
            InteractionStyles.Install(root);
            Assert.AreEqual(2, root.styleSheets.count, "installing twice must not add the sheets twice");
            InteractionStyles.Uninstall(root);
            Assert.AreEqual(0, root.styleSheets.count);
        }

        [TestCase(InteractionStyles.FocusResource)]
        [TestCase(InteractionStyles.AccessibilityResource)]
        public void EveryDesignSystemClassTheSheetSelectsExists(string resource)
        {
            var designSystem = AllDesignSystemSheets();
            Assert.IsNotEmpty(designSystem, "the design system sheets must be found");
            var missing = new SortedSet<string>();
            foreach (Match m in ClassName.Matches(StripComments(Text(resource))))
                if (!designSystem.Contains("." + m.Groups[1].Value)) missing.Add(m.Groups[1].Value);
            Assert.IsEmpty(missing, "classes selected here but not styled by any design system sheet: " + string.Join(", ", missing));
        }

        [Test]
        public void EveryTextScaleStepHasAClassInTheSheet()
        {
            var sheet = Text(InteractionStyles.AccessibilityResource);
            foreach (var step in TextScaleSteps.All)
            {
                var cls = AccessibilityClasses.TextScale(step);
                if (cls == null) continue;
                StringAssert.Contains("." + cls, sheet);
            }

            StringAssert.Contains("." + AccessibilityClasses.ReduceMotion, sheet);
            StringAssert.Contains("." + AccessibilityClasses.BoldText, sheet);
        }

        private static string StripComments(string css) => Regex.Replace(css, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
    }
}
