using FutureDigitalPlus.UIKit.Animation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Tests.AnimationModule
{
    /// <summary>New smoke tests for the animated elements and their Resources stylesheets (no panel needed).</summary>
    public class ElementTests
    {
        // Documented Resources paths of this module (see Documentation~/Animation/README.md).
        [TestCase("FutureDigitalPlus/UIKit/Animation/Styles/AnimatedFoldoutStyle")]
        [TestCase("FutureDigitalPlus/UIKit/Animation/Styles/AnimatedLabel")]
        [TestCase("FutureDigitalPlus/UIKit/Animation/Styles/MaskedInputField")]
        public void StyleSheet_LoadsFromResources(string path)
            => Assert.IsNotNull(Resources.Load<StyleSheet>(path), path);

        [Test]
        public void AnimatedFoldout_DefaultsToCollapsed_AndHostsChildrenInContent()
        {
            var foldout = new AnimatedFoldout();
            Assert.IsFalse(foldout.value);

            foldout.text = "Title";
            Assert.AreEqual("Title", foldout.text);

            var child = new Label("child");
            foldout.Add(child);
            // VisualElement.parent reports the logical parent (the foldout); the physical parent is the content container.
            Assert.AreSame(foldout.contentContainer, child.hierarchy.parent);
        }

        [Test]
        public void AnimatedFoldout_StartExpanded_ReportsExpanded()
            => Assert.IsTrue(new AnimatedFoldout(true).value);

        [Test]
        public void AnimatedLabel_ForwardsTextToInnerLabel()
        {
            var label = new AnimatedLabel { text = "abc" };
            Assert.AreEqual("abc", label.text);
            Assert.AreEqual("abc", label.Q<Label>().text);
        }

        [Test]
        public void MaskedInputField_MasksValue_WithoutAFont()
        {
            MaskedInputField.MonoFont = null; // optional: upstream font files are not part of this package
            var field = new MaskedInputField { prefixNum = 2, suffixNum = 9 };

            Assert.AreEqual(("0123456789abc", "01********abc"), field.ApplyPrivacyMask("0123456789abc"));

            field.value = "0123456789abc";
            Assert.AreEqual("0123456789abc", field.value);
            Assert.AreEqual("01********abc", field.maskedText.value);
        }

        [Test]
        public void Expander_CanBeBuilt_AndTracksAnimationFlag()
        {
            var expander = new Expander();
            var states = new System.Collections.Generic.List<bool>();
            expander.AnimationStatus = states.Add;
            expander.IsAnimating = true;
            expander.IsAnimating = false;
            CollectionAssert.AreEqual(new[] { true, false }, states);
        }
    }
}
