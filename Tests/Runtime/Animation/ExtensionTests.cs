using FutureDigitalPlus.UIKit.Animation;
using FutureDigitalPlus.UIKit.Animation.Extensions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Tests.AnimationModule
{
    /// <summary>New smoke tests for the vendored extension helpers and the animation data types (no panel needed).</summary>
    public class ExtensionTests
    {
        [Test]
        public void ColorUtil_FromHex_ParsesWithAndWithoutHash()
        {
            Assert.AreEqual(Color.red, "#FF0000".FromHex());
            Assert.AreEqual(Color.red, "FF0000".FromHex());
            Assert.AreEqual("#00FF00", "00FF00".EnsureHex());
        }

        [Test]
        public void Zero_ReturnsTinyNonZeroValue()
        {
            Assert.AreEqual(0.0001f, 0.Zero(), 1e-9f);
            Assert.AreEqual(0.0001f, 5f.Zero(), 1e-9f);
        }

        [Test]
        public void SetDisplay_And_SetOpacity_WriteInlineStyle()
        {
            var element = new VisualElement();
            element.SetDisplay(false);
            Assert.AreEqual(DisplayStyle.None, element.style.display.value);
            element.SetDisplay(true);
            Assert.AreEqual(DisplayStyle.Flex, element.style.display.value);

            element.SetOpacity(0f);
            Assert.AreEqual(0.0001f, element.style.opacity.value, 1e-9f);
            element.SetOpacity(0.5f);
            Assert.AreEqual(0.5f, element.style.opacity.value, 1e-6f);
        }

        [Test]
        public void SetBorderWidth_SetsAllFourSides()
        {
            var element = new VisualElement();
            element.SetBorderWidth(3f);
            Assert.AreEqual(3f, element.style.borderTopWidth.value);
            Assert.AreEqual(3f, element.style.borderRightWidth.value);
            Assert.AreEqual(3f, element.style.borderBottomWidth.value);
            Assert.AreEqual(3f, element.style.borderLeftWidth.value);
        }

        [Test]
        public void SetText_SetsLabelText() => Assert.AreEqual("hi", new Label().SetText("hi").text);

        [Test]
        public void ToUSS_NamesElementAndAddsClasses()
        {
            var element = new VisualElement().ToUSS("card", "card--big");
            Assert.AreEqual("card", element.name);
            Assert.IsTrue(element.ClassListContains("card"));
            Assert.IsTrue(element.ClassListContains("card--big"));
        }

        [Test]
        public void ApplyPrivacyMask_MasksCharactersUpToSuffixIndex()
        {
            var masked = "0123456789".ApplyPrivacyMask(0, 4);
            Assert.AreEqual("0123456789", masked.Item1);
            Assert.AreEqual("*****56789", masked.Item2);
            Assert.AreEqual(("", ""), "".ApplyPrivacyMask());
        }

        [Test]
        public void TryParseFloat_ParsesOrReturnsZero()
        {
            Assert.AreEqual(12.5f, "12.5".TryParseFloat(), 1e-6f);
            Assert.AreEqual(0f, "not a number".TryParseFloat());
        }

        [Test]
        public void AnimValueStore_ReturnsTinyValueInsteadOfZero()
        {
            var store = new AnimValueStore(0f, 1f, 100, 0);
            Assert.AreEqual(0.0001f, store.initial, 1e-9f);
            Assert.AreEqual(1f, store.final);
            Assert.AreEqual(100, store.duration);
            Assert.IsNotNull(store.easing, "default easing comes from GetEasing");
        }

        [Test]
        public void AnimColorStore_ParsesHexStrings()
        {
            var store = new AnimColorStore("#FF0000", "#0000FF", 200, 10);
            Assert.AreEqual(Color.red, store.initial);
            Assert.AreEqual(Color.blue, store.final);
            Assert.AreEqual(200, store.duration);
            Assert.AreEqual(10, store.delay);
        }

        [Test]
        public void VisualElementStyleStore_RoundTripsThroughElements()
        {
            // CreateStyleData reads inline values for the enum style properties, so they survive on an element without a panel.
            var source = new Label("a") { style = { justifyContent = Justify.Center, flexDirection = FlexDirection.RowReverse } };
            var data = source.CreateStyleData();
            var target = data.FromStyleData(new Label("b"));
            Assert.AreEqual(Justify.Center, target.style.justifyContent.value);
            Assert.AreEqual(FlexDirection.RowReverse, target.style.flexDirection.value);
        }

        [Test]
        public void SingleTask_RunsTasksInOrderThenCompletes()
        {
            var order = new System.Collections.Generic.List<int>();
            var sequence = new SingleTask();
            sequence.Add(next => { order.Add(1); next(); });
            sequence.Add(next => { order.Add(2); next(); });

            var completed = false;
            sequence.Run(() => completed = true);

            CollectionAssert.AreEqual(new[] { 1, 2 }, order);
            Assert.IsTrue(completed);
        }
    }
}
