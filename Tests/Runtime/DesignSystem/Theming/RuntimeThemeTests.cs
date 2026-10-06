using System.Collections;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Applier;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Data;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Palette;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Tests.DesignSystemModule
{
    /// <summary>
    /// Themes made at runtime (no bake): the patched stylesheet paints what the theme says, equals a baked theme when nothing
    /// was changed, works on several panels and subtrees, and leaks no stylesheet. The first test is the per-Unity-version
    /// canary: it fails loudly when a Unity version lays a <c>StyleSheet</c> out differently.
    /// </summary>
    public class RuntimeThemeTests : ThemingPanelFixture
    {
        private ThemeData _dark;
        private ThemeData _light;
        private readonly System.Collections.Generic.List<ThemeData> _made = new();

        [SetUp]
        public void SetUp()
        {
            _dark = Resources.Load<ThemeData>(ThemeData.DarkResource);
            _light = Resources.Load<ThemeData>(ThemeData.LightResource);
            Assert.IsNotNull(_dark);
            Assert.IsNotNull(_light);
            Assert.IsNotNull(_dark.StyleSheet, "the template must be baked");
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var theme in _made)
                if (theme) Object.Destroy(theme);
            _made.Clear();
        }

        private ThemeData Runtime(ThemeData template)
        {
            var theme = ThemeData.CreateRuntime(template);
            _made.Add(theme);
            return theme;
        }

        [Test]
        public void CreateRuntime_CopiesValues_HasNoSheet_AndKeepsTheTemplateScope()
        {
            var theme = Runtime(_light);

            Assert.IsTrue(theme.IsRuntime);
            Assert.AreSame(_light, theme.RuntimeTemplate);
            Assert.IsNull(theme.StyleSheet, "a runtime theme owns no baked sheet");
            Assert.AreEqual(_light.primaryColor, theme.primaryColor);
            Assert.AreEqual(".theme-light", theme.scopeSelector);
            Assert.AreEqual("theme-light", theme.ScopeClass);
            Assert.IsFalse(_light.IsRuntime);
        }

        [Test]
        public void CreateRuntime_OfARuntimeTheme_PointsAtTheSameBakedTemplate()
        {
            var first = Runtime(_dark);
            var second = Runtime(first);
            Assert.AreSame(_dark, second.RuntimeTemplate);
        }

        [Test]
        public void PatcherCanary_TheSerialisedStyleSheetLayoutIsTheOneWeUnderstand()
        {
            Assert.IsTrue(ThemeSheetPatcher.TryCreate(_dark.StyleSheet, out var patcher),
                "ThemeSheetPatcher.TryCreate failed on the shipped Dark sheet: Unity changed the serialised StyleSheet layout (see the warning above)");
            Assert.Greater(patcher.TokenNames.Count, 50);
            Assert.Contains("--fdp-color-primary", new System.Collections.Generic.List<string>(patcher.TokenNames));
        }

        [UnityTest]
        public IEnumerator Apply_RuntimeTheme_PaintsItsOwnValues()
        {
            yield return CreatePanel();
            var probes = AddProbes();

            var theme = Runtime(_dark);
            theme.primaryColor = new Color(1f, 0.1f, 0.6f);
            theme.dangerColor = new Color(0.2f, 0.9f, 0.2f);
            theme.bgColor = new Color(0.2f, 0f, 0.3f);
            theme.fontSizeH1 = 40f;
            theme.spacing4 = 21f;

            Assert.IsTrue(ThemeRuntime.Apply(Root, theme));
            Assert.AreSame(theme, ThemeRuntime.WornBy(Root));
            yield return Settle();

            AssertColor(theme.primaryColor, probes.Primary.resolvedStyle.backgroundColor, "primary button");
            AssertColor(theme.dangerColor, probes.Danger.resolvedStyle.backgroundColor, "danger button");
            AssertColor(theme.bgColor, probes.Screen.resolvedStyle.backgroundColor, ".ds-root background");
            Assert.AreEqual(40f, probes.H1.resolvedStyle.fontSize, 0.01f);
            Assert.AreEqual(21f, probes.Primary.resolvedStyle.paddingLeft, 0.01f, "a px token reaches padding");
        }

        [UnityTest]
        public IEnumerator Apply_RuntimeCopyOfABakedTheme_ResolvesLikeTheBakedOne()
        {
            yield return CreatePanel();
            var probes = AddProbes();

            foreach (var template in new[] { _dark, _light })
            {
                ThemeRuntime.Apply(Root, template);
                yield return Settle();
                var baked = Snapshot(probes);

                ThemeRuntime.Apply(Root, Runtime(template));
                yield return Settle();
                var patched = Snapshot(probes);

                for (var i = 0; i < baked.Length; i++)
                    Assert.AreEqual(baked[i], patched[i], template.name + ": probe " + i + " differs between the baked and the patched sheet");
            }
        }

        private static string[] Snapshot(Probes p) => new[]
        {
            p.Primary.resolvedStyle.backgroundColor.ToString(),
            p.Primary.resolvedStyle.color.ToString(),
            p.Danger.resolvedStyle.backgroundColor.ToString(),
            p.Screen.resolvedStyle.backgroundColor.ToString(),
            p.Screen.resolvedStyle.color.ToString(),
            p.H1.resolvedStyle.fontSize.ToString("0.##"),
            p.Body.resolvedStyle.fontSize.ToString("0.##"),
            p.Primary.resolvedStyle.borderTopLeftRadius.ToString("0.##"),
            p.Primary.resolvedStyle.paddingLeft.ToString("0.##"),
            p.Card.resolvedStyle.backgroundColor.ToString(),
            p.Card.resolvedStyle.borderTopWidth.ToString("0.##"),
            p.Card.resolvedStyle.borderTopColor.ToString(),
        };

        [UnityTest]
        public IEnumerator Apply_AfterChangingAValue_BuildsANewSheet_AndDestroysTheOldOne()
        {
            yield return CreatePanel();
            var probes = AddProbes();
            var before = ThemeRuntime.LiveRuntimeSheets;

            var theme = Runtime(_dark);
            theme.primaryColor = Color.red;
            Assert.IsTrue(ThemeRuntime.Apply(Root, theme));
            var first = Root.styleSheets[Root.styleSheets.count - 1];
            Assert.AreEqual(before + 1, ThemeRuntime.LiveRuntimeSheets);

            for (var i = 0; i < 5; i++)
            {
                theme.primaryColor = new Color(0.1f * i, 0.5f, 0.9f);
                Assert.IsTrue(ThemeRuntime.Apply(Root, theme));
                Assert.AreEqual(before + 1, ThemeRuntime.LiveRuntimeSheets, "swap " + i + ": exactly one generated sheet is alive");
            }

            yield return null;   // Object.Destroy takes effect at the end of the frame
            Assert.IsTrue(first == null, "the first generated sheet was destroyed after the swap");
            yield return Settle();
            AssertColor(theme.primaryColor, probes.Primary.resolvedStyle.backgroundColor, "latest value");

            ThemeRuntime.Clear(Root);
            Assert.AreEqual(before, ThemeRuntime.LiveRuntimeSheets, "clearing the root releases the sheet");
        }

        [UnityTest]
        public IEnumerator Apply_TheSameRevisionTwice_IsANoOp()
        {
            yield return CreatePanel();
            var theme = Runtime(_dark);

            Assert.IsTrue(ThemeRuntime.Apply(Root, theme));
            var sheet = Root.styleSheets[Root.styleSheets.count - 1];
            Assert.IsTrue(ThemeRuntime.Apply(Root, theme));

            Assert.AreEqual(1, CountOf(Root, sheet));
            Assert.AreSame(sheet, Root.styleSheets[Root.styleSheets.count - 1], "no new clone for an unchanged theme");
        }

        private static int CountOf(VisualElement root, StyleSheet sheet)
        {
            var n = 0;
            for (var i = 0; i < root.styleSheets.count; i++)
                if (root.styleSheets[i] == sheet) n++;
            return n;
        }

        [UnityTest]
        public IEnumerator DestroyingARuntimeTheme_ReleasesItsSheetOnceNoRootWearsIt()
        {
            yield return CreatePanel();
            var before = ThemeRuntime.LiveRuntimeSheets;

            var theme = ThemeData.CreateRuntime(_dark);
            ThemeRuntime.Apply(Root, theme);
            Assert.AreEqual(before + 1, ThemeRuntime.LiveRuntimeSheets);

            ThemeRuntime.Clear(Root);
            Object.Destroy(theme);
            yield return null;
            Assert.AreEqual(before, ThemeRuntime.LiveRuntimeSheets);
        }

        [UnityTest]
        public IEnumerator TwoPanels_WearDifferentRuntimeThemesIndependently()
        {
            yield return CreatePanel();
            var probesA = AddProbes();

            // A second UIDocument on its own panel.
            var hostB = new GameObject("DsThemingPanelTests-B");
            var settingsB = ScriptableObject.CreateInstance<PanelSettings>();
            var targetB = new RenderTexture(320, 240, 0);
            settingsB.scaleMode = PanelScaleMode.ConstantPixelSize;
            settingsB.targetTexture = targetB;
            hostB.SetActive(false);
            var docB = hostB.AddComponent<UIDocument>();
            docB.panelSettings = settingsB;
            hostB.SetActive(true);
            yield return null;
            yield return null;

            var rootB = docB.rootVisualElement;
            rootB.parent.styleSheets.Add(Resources.Load<StyleSheet>(BaseSheet));
            var probesB = AddProbes(rootB);

            var a = Runtime(_dark);
            a.primaryColor = Color.red;
            var b = Runtime(_dark);
            b.primaryColor = Color.blue;

            ThemeRuntime.Apply(Root, a);
            ThemeRuntime.Apply(rootB, b);
            yield return Settle();

            AssertColor(Color.red, probesA.Primary.resolvedStyle.backgroundColor, "panel A");
            AssertColor(Color.blue, probesB.Primary.resolvedStyle.backgroundColor, "panel B");

            ThemeRuntime.Clear(rootB);
            Object.Destroy(hostB);
            Object.Destroy(settingsB);
            targetB.Release();
            Object.Destroy(targetB);
        }

        [UnityTest]
        public IEnumerator TwoSubtrees_OfOnePanel_WearDifferentRuntimeThemes_AndNestedOnesWin()
        {
            yield return CreatePanel();

            var left = new VisualElement();
            var right = new VisualElement();
            var inner = new VisualElement();
            Root.Add(left);
            Root.Add(right);
            right.Add(inner);
            var pl = AddProbes(left);
            var pr = AddProbes(right);
            var pi = AddProbes(inner);

            var red = Runtime(_light);
            red.primaryColor = Color.red;
            var blue = Runtime(_light);
            blue.primaryColor = Color.blue;
            var green = Runtime(_light);
            green.primaryColor = Color.green;

            // The same scope class on all three: a sheet only applies inside the element it is attached to.
            ThemeRuntime.Apply(left, red);
            ThemeRuntime.Apply(right, blue);
            ThemeRuntime.Apply(inner, green);
            yield return Settle();

            AssertColor(Color.red, pl.Primary.resolvedStyle.backgroundColor, "left subtree");
            AssertColor(Color.blue, pr.Primary.resolvedStyle.backgroundColor, "right subtree");
            AssertColor(Color.green, pi.Primary.resolvedStyle.backgroundColor, "nested subtree beats its parent");
        }

        [UnityTest]
        public IEnumerator Apply_ARuntimeThemeWhoseTemplateIsNotBaked_ReturnsFalse_WithOneWarning()
        {
            yield return CreatePanel();
            LogAssert.ignoreFailingMessages = false;

            var unbaked = ScriptableObject.CreateInstance<ThemeData>();
            var theme = Runtime(unbaked);

            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"no baked stylesheet to patch"));
            Assert.IsFalse(ThemeRuntime.Apply(Root, theme));
            Assert.IsNull(ThemeRuntime.WornBy(Root));
            Assert.IsFalse(ThemeRuntime.Apply(Root, theme), "a second call does not warn again");

            Object.Destroy(unbaked);
        }

        [UnityTest]
        public IEnumerator Apply_OverAnotherTheme_OnALivePanel_CutsTransitionsFor120Ms()
        {
            yield return CreatePanel();
            AddProbes();

            ThemeRuntime.Apply(Root, _dark);
            yield return Settle();
            Assert.IsFalse(Root.ClassListContains(ThemeRuntime.NoTransitionClass), "the first apply is not a swap");

            ThemeRuntime.Apply(Root, _light);
            Assert.IsTrue(Root.ClassListContains(ThemeRuntime.NoTransitionClass), "a swap cuts transitions");

            yield return new WaitForSeconds(ThemeRuntime.NoTransitionMillis / 1000f + 0.25f);
            Assert.IsFalse(Root.ClassListContains(ThemeRuntime.NoTransitionClass), "and gives them back");
        }

        [UnityTest]
        public IEnumerator FontSizes_OfTheTheme_AreWhatThePhoneTierShows()
        {
            yield return CreatePanel();
            var probes = AddProbes();
            Root.AddToClassList("fdp-input-touch");

            var theme = Runtime(_dark);
            theme.fontSizeH1 = 32f;
            theme.fontSizeBody1 = 17f;
            ThemeRuntime.Apply(Root, theme);
            yield return Settle();

            Assert.AreEqual(32f, probes.H1.resolvedStyle.fontSize, 0.01f, "the touch tier no longer overrides the theme's heading size");
            Assert.AreEqual(17f, probes.Body.resolvedStyle.fontSize, 0.01f, "nor the body size");
        }

        [UnityTest]
        public IEnumerator TouchTier_GrowsControlsThroughTokens()
        {
            yield return CreatePanel();
            var probes = AddProbes();
            yield return Settle();
            Assert.AreEqual(36f, probes.Primary.resolvedStyle.height, 0.5f, "pointer tier button");

            Root.AddToClassList("fdp-input-touch");
            yield return Settle();
            Assert.AreEqual(48f, probes.Primary.resolvedStyle.height, 0.5f, "touch tier button, from --fdp-control-h-md");
        }

        [UnityTest]
        public IEnumerator Buttons_UseTheDurationTokens()
        {
            yield return CreatePanel();
            var probes = AddProbes();
            ThemeRuntime.Apply(Root, _dark);
            yield return Settle();

            // D12 of the plan: var() in transition-duration resolves; the token is 120 ms.
            var durations = probes.Primary.resolvedStyle.transitionDuration;
            var found = false;
            foreach (var d in durations)
            {
                found = true;
                var millis = d.unit == TimeUnit.Millisecond ? d.value : d.value * 1000f;
                Assert.AreEqual(120f, millis, 0.5f);
            }

            Assert.IsTrue(found, "ds-btn has a transition duration");
        }

        [UnityTest]
        public IEnumerator TextField_ShowsAPrimaryBorderWhileFocused()
        {
            yield return CreatePanel();
            ThemeRuntime.Apply(Root, _dark);

            var screen = new VisualElement();
            screen.AddToClassList("ds-root");
            var field = new TextField();
            field.AddToClassList("ds-input");
            screen.Add(field);
            Root.Add(screen);
            yield return Settle();

            var input = field.Q(className: "unity-base-text-field__input") ?? field.Q(className: "unity-text-input");
            Assert.IsNotNull(input);
            var rest = input.resolvedStyle.borderTopColor;
            AssertColor(_dark.borderStrongColor, rest, "unfocused border is the strong border");

            field.Focus();
            yield return Settle();
            AssertColor(_dark.primaryColor, input.resolvedStyle.borderTopColor, "focused border");
        }

        [UnityTest]
        public IEnumerator HighContrast_OnAndOff_SwapsThePaletteAndRestoresTheTheme()
        {
            yield return CreatePanel();
            var probes = AddProbes();
            ThemeRuntime.Apply(Root, _dark);
            yield return Settle();
            var normalText = probes.Screen.resolvedStyle.color;

            Assert.IsTrue(ThemeRuntime.SetHighContrast(Root, true));
            Assert.IsTrue(ThemeRuntime.IsHighContrast(Root));
            yield return Settle();
            AssertColor(Color.white, probes.Screen.resolvedStyle.color, "high-contrast dark text is white");
            var hc = ThemeRuntime.WornBy(Root);
            Assert.AreNotSame(_dark, hc);
            Assert.IsEmpty(ContrastAudit.Failures(hc.ToColorMap(), ContrastLevel.High), "the worn palette passes the High audit");

            Assert.IsTrue(ThemeRuntime.SetHighContrast(Root, false));
            Assert.IsFalse(ThemeRuntime.IsHighContrast(Root));
            Assert.AreSame(_dark, ThemeRuntime.WornBy(Root));
            yield return Settle();
            AssertColor(normalText, probes.Screen.resolvedStyle.color, "text colour is back");
        }

        [UnityTest]
        public IEnumerator FromBrand_BuildsATwoColourThemeWithoutAnEditorBake()
        {
            yield return CreatePanel();
            var probes = AddProbes();

            var brand = ThemeData.FromBrand(new Color(0.9f, 0.3f, 0.1f), new Color(0.06f, 0.06f, 0.09f), dark: true);
            _made.Add(brand);
            Assert.IsNotNull(brand);

            Assert.IsTrue(ThemeRuntime.Apply(Root, brand));
            yield return Settle();
            AssertColor(new Color(0.9f, 0.3f, 0.1f), probes.Primary.resolvedStyle.backgroundColor, "brand primary");
            AssertColor(new Color(0.06f, 0.06f, 0.09f), probes.Screen.resolvedStyle.backgroundColor, "brand background");
        }
    }
}
