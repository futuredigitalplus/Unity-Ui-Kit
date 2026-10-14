using System.Collections.Generic;
using FutureDigitalPlus.UIKit.Foundation.Responsive;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation.Tests
{
    /// <summary>EditMode tests of the responsive pieces: rules in the scale driver, the root binder and the device catalog of the Responsive Manager.</summary>
    public class ResponsiveEditorTests
    {
        static readonly PanelScaleEnvironment Phone = new PanelScaleEnvironment { Handheld = true, Device = DeviceClass.Phone, Dpi = 460f, Width = 1179, Height = 2556 };
        static readonly PanelScaleEnvironment FlipCover = new PanelScaleEnvironment { Handheld = true, Device = DeviceClass.Phone, Dpi = 373f, Width = 948, Height = 1048 };

        readonly List<Object> _created = new List<Object>();
        readonly List<System.IDisposable> _disposables = new List<System.IDisposable>();

        [TearDown]
        public void TearDown()
        {
            foreach (System.IDisposable disposable in _disposables)
                disposable.Dispose();
            _disposables.Clear();
            foreach (Object created in _created)
                Object.DestroyImmediate(created);
            _created.Clear();
        }

        PanelScaleDriver NewDriver(PanelScaleEnvironment environment, ResponsiveRules rules = null)
        {
            PanelSettings source = ScriptableObject.CreateInstance<PanelSettings>();
            _created.Add(source);
            var driver = new PanelScaleDriver(source, PanelScaleProfile.CardPortrait());
            _disposables.Add(driver);
            driver.Rules = rules;
            driver.EnvironmentSource = () => environment;
            driver.Tick();
            return driver;
        }

        static ResponsiveRule Rule(DeviceFormFactor form, float scale, float width = 0f)
        {
            return new ResponsiveRule { form = form, orientation = ResponsiveOrientation.Any, scale = scale, maxContentWidth = width };
        }

        // ---- driver ----

        [Test]
        public void Driver_WithoutRules_ClassifiesAndKeepsThePolicyScale()
        {
            PanelScaleDriver driver = NewDriver(Phone);
            Assert.AreEqual(DeviceFormFactor.Phone, driver.FormFactor);
            Assert.IsTrue(driver.Rule.IsIdentity);
            Assert.IsFalse(driver.Landscape);
            Assert.AreEqual(460f / 160f, driver.PixelsPerUnit, 0.001f);
        }

        [Test]
        public void Driver_AppliesTheRuleScale_OnTopOfTheUserScale()
        {
            var rules = new ResponsiveRules();
            rules.Set(Rule(DeviceFormFactor.Phone, 0.8f));
            PanelScaleDriver driver = NewDriver(Phone, rules);
            Assert.AreEqual(0.8f, driver.Rule.Scale, 0.0001f);
            Assert.AreEqual(460f / 160f * 0.8f, driver.PixelsPerUnit, 0.001f);

            driver.UserScale = 1.25f;
            driver.Tick();
            Assert.AreEqual(460f / 160f, driver.PixelsPerUnit, 0.001f);
            Assert.AreEqual(DeviceFormFactor.Phone, driver.FormFactor, "the user's scale never changes the form factor");
        }

        [Test]
        public void Driver_OnAFlipCover_UsesTheCoverRule_OrFallsBackToThePhone()
        {
            var rules = new ResponsiveRules();
            rules.Set(Rule(DeviceFormFactor.Phone, 0.9f));
            PanelScaleDriver driver = NewDriver(FlipCover, rules);
            Assert.AreEqual(DeviceFormFactor.FlipCover, driver.FormFactor);
            Assert.AreEqual(0.9f, driver.Rule.Scale, 0.0001f);

            rules.Set(Rule(DeviceFormFactor.FlipCover, 0.75f));
            Assert.IsTrue(driver.Tick(), "an edited table is noticed without a new environment");
            Assert.AreEqual(0.75f, driver.Rule.Scale, 0.0001f);
            Assert.AreEqual(373f / 160f * 0.75f, driver.PixelsPerUnit, 0.001f);
            Assert.IsFalse(driver.Tick());
        }

        [Test]
        public void Driver_PixelExact_UsesAConstantPixelScaleOfTheDevice()
        {
            PanelScaleEnvironment preview = Phone;
            preview.PixelExact = true;
            PanelScaleDriver driver = NewDriver(preview);
            Assert.AreEqual(PanelScaleMode.ConstantPixelSize, driver.Settings.scaleMode);
            Assert.AreEqual(460f / 160f, driver.Settings.scale, 0.001f);
            Assert.AreEqual(460f / 160f, driver.PixelsPerUnit, 0.001f);
        }

        [Test]
        public void Driver_IsListedWhileAlive()
        {
            PanelScaleDriver driver = NewDriver(Phone);
            CollectionAssert.Contains(new List<PanelScaleDriver>(PanelScaleDriver.Active), driver);
            driver.Dispose();
            CollectionAssert.DoesNotContain(new List<PanelScaleDriver>(PanelScaleDriver.Active), driver);
        }

        // ---- binder ----

        [Test]
        public void Binder_PutsTheFormFactorAndPostureClassesOnTheRoot()
        {
            var rules = new ResponsiveRules();
            PanelScaleDriver driver = NewDriver(FlipCover, rules);
            var root = new VisualElement();
            var binder = new ResponsiveRootBinder(root, driver);
            _disposables.Add(binder);
            Assert.IsTrue(root.ClassListContains("fdp-ff-flip-cover"));
            Assert.IsFalse(root.ClassListContains("fdp-ff-phone"));
            Assert.IsFalse(root.ClassListContains(DeviceFormFactors.ClassHalfOpened));

            binder.Hinge = HingeAxis.Horizontal;
            binder.Posture = DevicePosture.HalfOpened;
            Assert.IsTrue(root.ClassListContains(DeviceFormFactors.ClassHalfOpened));
            Assert.IsTrue(root.ClassListContains(DeviceFormFactors.ClassHingeHorizontal));

            driver.EnvironmentSource = () => Phone;
            driver.Tick();
            Assert.IsTrue(root.ClassListContains("fdp-ff-phone"));
            Assert.IsFalse(root.ClassListContains("fdp-ff-flip-cover"));
        }

        [Test]
        public void Binder_LimitsTheColumns_AndLiftsTheLimit()
        {
            var rules = new ResponsiveRules();
            rules.Set(Rule(DeviceFormFactor.Phone, 1f, 480f));
            PanelScaleDriver driver = NewDriver(Phone, rules);
            var root = new VisualElement();
            var column = new VisualElement();
            column.AddToClassList("my-column");
            root.Add(column);
            var binder = new ResponsiveRootBinder(root, driver) { ColumnClass = "my-column" };
            _disposables.Add(binder);
            Assert.AreEqual(480f, column.style.maxWidth.value.value, 0.01f);

            rules.Clear(DeviceFormFactor.Phone, ResponsiveOrientation.Any);
            driver.Tick();
            Assert.AreEqual(StyleKeyword.Null, column.style.maxWidth.keyword);
        }

        [Test]
        public void Binder_IsListedWhileAlive()
        {
            PanelScaleDriver driver = NewDriver(Phone);
            var binder = new ResponsiveRootBinder(new VisualElement(), driver);
            Assert.AreSame(driver, binder.Driver);
            CollectionAssert.Contains(new List<ResponsiveRootBinder>(ResponsiveRootBinder.Active), binder);
            binder.Dispose();
            CollectionAssert.DoesNotContain(new List<ResponsiveRootBinder>(ResponsiveRootBinder.Active), binder);
        }

        // ---- catalog ----

        [Test]
        public void Catalog_LoadsWithoutProblems_AndCoversEveryFormFactor()
        {
            var problems = new List<string>();
            ResponsiveDeviceCatalogData catalog = ResponsiveDeviceCatalog.Load(problems);
            CollectionAssert.IsEmpty(problems);
            Assert.Greater(catalog.devices.Count, 20);
            var seen = new HashSet<DeviceFormFactor>();
            foreach (ResponsiveDeviceSpec device in catalog.devices)
            {
                foreach (ResponsiveScreenSpec screen in device.screens)
                    seen.Add(screen.FormFactor(PanelScaleProfile.CardPortrait()));
            }
            CollectionAssert.AreEquivalent(DeviceFormFactors.All, seen);
        }

        [Test]
        public void Catalog_ParseAndMerge()
        {
            Assert.IsNull(ResponsiveDeviceCatalog.Parse(""));
            Assert.IsNull(ResponsiveDeviceCatalog.Parse("not json"));
            ResponsiveDeviceCatalogData catalog = ResponsiveDeviceCatalog.Parse(
                "{\"devices\":[{\"id\":\"a\",\"name\":\"A\",\"group\":\"Phone\",\"screens\":[{\"name\":\"Main\",\"width\":1080,\"height\":2400,\"dpi\":420,\"hinge\":0,\"safePortrait\":[100,50,0,0],\"safeLandscape\":[0,50,100,0]}]}]}");
            Assert.IsNotNull(catalog);
            Assert.AreEqual(100f, catalog.devices[0].screens[0].Insets(false).Top);
            ResponsiveDeviceCatalogData extra = ResponsiveDeviceCatalog.Parse(
                "{\"devices\":[{\"id\":\"a\",\"name\":\"A2\",\"screens\":[{\"width\":720,\"height\":1600,\"dpi\":269,\"hinge\":1}]},{\"id\":\"b\",\"name\":\"B\",\"screens\":[{\"width\":720,\"height\":1600,\"dpi\":269}]}]}");
            ResponsiveDeviceCatalog.Merge(catalog, extra);
            Assert.AreEqual(2, catalog.devices.Count);
            Assert.AreEqual("A2", catalog.devices[0].name);
            Assert.AreEqual(HingeAxis.Horizontal, catalog.devices[0].screens[0].hinge);
        }
    }
}
