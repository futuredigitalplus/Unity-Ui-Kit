using FutureDigitalPlus.UIKit.DesignSystem.Theme.Applier;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Tests
{
    /// <summary>EditMode smoke tests of the facade: no panel needed, everything runs on detached elements.</summary>
    public class UIKitEditModeTests
    {
        [Test]
        public void Options_HaveSensibleDefaults()
        {
            var o = new UIKitOptions();
            Assert.IsNull(o.Theme);
            Assert.IsTrue(o.InstallBaseStyles);
            Assert.IsTrue(o.ApplySafeArea);
            Assert.AreEqual(MobileMode.Auto, o.Mobile);
            Assert.AreEqual(768f, o.MobileBreakpoint);
        }

        [Test]
        public void Install_ThemeBaseStylesAndMobileClass_OnDetachedRoot_ThenDispose()
        {
            var light = Resources.Load<ThemeData>("FutureDigitalPlus/UIKit/DesignSystem/Themes/Light");
            var root = new VisualElement();
            var kit = UIKitInstaller.Install(root, new UIKitOptions { Theme = light, Mobile = MobileMode.On, ApplySafeArea = false });

            Assert.IsTrue(kit.IsMobile);
            Assert.AreSame(light, ThemeRuntime.WornBy(root));
            Assert.AreEqual(2, root.styleSheets.count, "base styles + theme");

            kit.Dispose();
            Assert.AreEqual(0, root.styleSheets.count);
            Assert.IsFalse(root.ClassListContains(UIKitInstaller.MobileClass));
        }

        [Test]
        public void SafeAreaInsets_FromScreen_ScalesToPanelUnits()
        {
            var i = SafeAreaInsets.FromScreen(new Rect(0, 100, 1000, 1900), new Vector2(1000, 2000), new Vector2(500, 1000));
            Assert.AreEqual(50f, i.Bottom, 1e-3f);
            Assert.AreEqual(0f, i.Top, 1e-3f);
        }
    }
}
