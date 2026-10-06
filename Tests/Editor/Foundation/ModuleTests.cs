using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation.Tests
{
    /// <summary>EditMode tests of the small pieces: name tables, asset sources, catalog, state store, scale driver, element defaults, resources and source rules.</summary>
    public class ModuleTests
    {
        const string StylesFolder = "Runtime/Foundation/Resources/FutureDigitalPlus/UIKit/Foundation/Styles/";

        static string PackageFile(string relative)
        {
            Assert.AreNotEqual(FutureDigitalPlus.UIKit.Location.KitInstall.NotFound, FutureDigitalPlus.UIKit.Location.KitLocation.Install, "the kit folder was not found");
            string full = FutureDigitalPlus.UIKit.Location.KitLocation.FullPathOf(relative);
            Assert.IsTrue(File.Exists(full) || Directory.Exists(full), "missing in the kit: " + relative);
            return full;
        }

        [TearDown]
        public void TearDown()
        {
            ScreenCatalog.Default.Clear();
            TestEnvironment.ResetStatics();
        }

        // ---- name tables ----

        [TestCase(RuntimePlatform.IPhonePlayer, "ios")]
        [TestCase(RuntimePlatform.Android, "android")]
        [TestCase(RuntimePlatform.WindowsPlayer, "windows")]
        [TestCase(RuntimePlatform.WindowsEditor, "windows")]
        [TestCase(RuntimePlatform.OSXPlayer, "macos")]
        [TestCase(RuntimePlatform.OSXEditor, "macos")]
        [TestCase(RuntimePlatform.LinuxPlayer, "linux")]
        [TestCase(RuntimePlatform.WebGLPlayer, "webgl")]
        [TestCase(RuntimePlatform.Switch, "console")]
        [TestCase(RuntimePlatform.PS5, "console")]
        [TestCase(RuntimePlatform.GameCoreXboxSeries, "console")]
        [TestCase(RuntimePlatform.tvOS, "console")]
        [TestCase(RuntimePlatform.VisionOS, "xr")]
        public void PlatformName_CoversEveryShippingPlatform(RuntimePlatform platform, string expected)
        {
            Assert.AreEqual(expected, UiProfileWatcher.PlatformName(platform));
        }

        [TestCase(RuntimePlatform.IPhonePlayer, UiInputMode.Touch)]
        [TestCase(RuntimePlatform.Android, UiInputMode.Touch)]
        [TestCase(RuntimePlatform.Switch, UiInputMode.Gamepad)]
        [TestCase(RuntimePlatform.tvOS, UiInputMode.Gamepad)]
        [TestCase(RuntimePlatform.WindowsPlayer, UiInputMode.Pointer)]
        [TestCase(RuntimePlatform.OSXEditor, UiInputMode.Pointer)]
        [TestCase(RuntimePlatform.WebGLPlayer, UiInputMode.Pointer)]
        public void DefaultInputMode_IsTheDeviceTheyUsuallyHave(RuntimePlatform platform, UiInputMode expected)
        {
            Assert.AreEqual(expected, UiProfileWatcher.DefaultInputMode(platform));
        }

        [TestCase(UiInputMode.Pointer, "mouse")]
        [TestCase(UiInputMode.Touch, "touch")]
        [TestCase(UiInputMode.Keyboard, "keyboard")]
        [TestCase(UiInputMode.Gamepad, "gamepad")]
        public void InputName_MatchesTheNamingScheme(UiInputMode mode, string expected)
        {
            Assert.AreEqual(expected, UiProfileWatcher.InputName(mode));
        }

        [TestCase(RuntimePlatform.IPhonePlayer, 460f, 1179, 2556, DeviceClass.Phone)]
        [TestCase(RuntimePlatform.IPhonePlayer, 326f, 1488, 2266, DeviceClass.Tablet)]
        [TestCase(RuntimePlatform.IPhonePlayer, 264f, 2048, 2732, DeviceClass.Tablet)]
        [TestCase(RuntimePlatform.Android, 420f, 1080, 2092, DeviceClass.Phone)]
        [TestCase(RuntimePlatform.Android, 420f, 2208, 1840, DeviceClass.Tablet)]
        [TestCase(RuntimePlatform.WebGLPlayer, 96f, 1920, 1080, DeviceClass.Web)]
        [TestCase(RuntimePlatform.Switch, 0f, 1920, 1080, DeviceClass.Console)]
        [TestCase(RuntimePlatform.WindowsPlayer, 96f, 3840, 2160, DeviceClass.Desktop)]
        [TestCase(RuntimePlatform.OSXPlayer, 144f, 2560, 1440, DeviceClass.Desktop)]
        public void DetectDevice_TellsPhonesTabletsAndTheRestApart(RuntimePlatform platform, float dpi, int width, int height, DeviceClass expected)
        {
            bool handheld = platform == RuntimePlatform.IPhonePlayer || platform == RuntimePlatform.Android;
            Assert.AreEqual(expected, PanelScaleDriver.DetectDevice(platform, handheld, dpi, width, height));
        }

        // ---- asset sources and catalog ----

        [Test]
        public void ResourcesAssetSource_LoadsFromItsFolder_AndAnswersNullForMissingNames()
        {
            ResourcesAssetSource source = new ResourcesAssetSource("FutureDigitalPlus/UIKit/Foundation/Styles");
            Assert.IsNotNull(source.LoadStyle("Foundation"));
            Assert.IsNull(source.LoadStyle("DoesNotExist"));
            Assert.IsNull(source.LoadUxml("DoesNotExist"));
            Assert.IsNull(source.LoadStyle(null));
            Assert.IsNull(source.LoadStyle(string.Empty));
        }

        [Test]
        public void CompositeAssetSource_AsksInOrder_AndSkipsNullSources()
        {
            ResourcesAssetSource empty = new ResourcesAssetSource("Nowhere");
            ResourcesAssetSource real = new ResourcesAssetSource("FutureDigitalPlus/UIKit/Foundation/Styles");
            CompositeAssetSource composite = new CompositeAssetSource(null, empty, real);
            Assert.IsNotNull(composite.LoadStyle("Foundation"));
            Assert.IsNull(new CompositeAssetSource().LoadStyle("Foundation"));
            Assert.IsNull(new CompositeAssetSource(null).LoadUxml("x"));
        }

        [Test]
        public void ScreenDefinitionAssetSource_FindsLayoutAndStyleById_AndLetsALaterDefinitionWin()
        {
            VisualTreeAsset layoutA = ScriptableObject.CreateInstance<VisualTreeAsset>();
            VisualTreeAsset layoutB = ScriptableObject.CreateInstance<VisualTreeAsset>();
            ScreenDefinition a = ScreenDefinition.Create("Home", layoutA);
            ScreenDefinition b = ScreenDefinition.Create("Home", layoutB);
            try
            {
                ScreenDefinitionAssetSource source = new ScreenDefinitionAssetSource(new[] { a, null, b });
                Assert.AreSame(layoutB, source.LoadUxml("Home"));
                Assert.IsNull(source.LoadUxml("Other"));
                Assert.IsNull(source.LoadStyle("Home"), "this definition has no style sheet");
            }
            finally
            {
                Object.DestroyImmediate(a);
                Object.DestroyImmediate(b);
                Object.DestroyImmediate(layoutA);
                Object.DestroyImmediate(layoutB);
            }
        }

        [Test]
        public void ScreenDefinition_FallsBackToTheAssetNameWhenTheIdIsEmpty()
        {
            ScreenDefinition definition = ScriptableObject.CreateInstance<ScreenDefinition>();
            definition.name = "Credits";
            try
            {
                Assert.AreEqual("Credits", definition.Id);
                Assert.IsNull(definition.Layout);
                Assert.AreEqual(0, definition.ExtraStyles.Count);
            }
            finally
            {
                Object.DestroyImmediate(definition);
            }
        }

        [Test]
        public void ScreenCatalog_RegistersReplacesAndValidates()
        {
            ScreenCatalog catalog = new ScreenCatalog();
            catalog.Register("A", () => null);
            catalog.Register("A", () => null);
            Assert.AreEqual(1, catalog.Ids.Count);
            System.Func<UiScreen> factory;
            Assert.IsTrue(catalog.TryGet("A", out factory));
            Assert.IsFalse(catalog.TryGet("B", out factory));
            Assert.IsFalse(catalog.TryGet(null, out factory));
            Assert.Throws<System.ArgumentException>(() => catalog.Register(string.Empty, () => null));
            Assert.Throws<System.ArgumentNullException>(() => catalog.Register("C", (System.Func<UiScreen>)null));
            Assert.Throws<System.ArgumentNullException>(() => catalog.Register((ScreenDefinition)null));
            catalog.Clear();
            Assert.AreEqual(0, catalog.Ids.Count);
        }

        // ---- state store ----

        [Test]
        public void PlayerPrefsUiStateStore_RoundTripsAndDeletes_AndPrefixesItsKeys()
        {
            string prefix = "fdp-test-" + System.Guid.NewGuid().ToString("N");
            PlayerPrefsUiStateStore store = new PlayerPrefsUiStateStore(prefix);
            try
            {
                Assert.IsNull(store.Load("nav"));
                store.Save("nav", "Home\nSettings");
                Assert.AreEqual("Home\nSettings", store.Load("nav"));
                Assert.IsTrue(PlayerPrefs.HasKey(prefix + "/nav"));
                store.Save("nav", null);
                Assert.IsNull(store.Load("nav"), "saving null deletes the value");
                store.Save("other", "x");
                store.Delete("other");
                Assert.IsNull(store.Load("other"));
            }
            finally
            {
                store.Delete("nav");
                store.Delete("other");
            }
        }

        // ---- scale driver ----

        static PanelSettings NewSettings()
        {
            PanelSettings settings = ScriptableObject.CreateInstance<PanelSettings>();
            settings.scaleMode = PanelScaleMode.ConstantPixelSize;
            settings.scale = 1f;
            return settings;
        }

        [Test]
        public void PanelScaleDriver_OnAPhoneWithARealDpi_UsesPhysicalSize_OnARuntimeCopy()
        {
            PanelSettings source = NewSettings();
            PanelScaleDriver driver = new PanelScaleDriver(source, PanelScaleProfile.CardPortrait());
            try
            {
                driver.EnvironmentSource = () => new PanelScaleEnvironment { Handheld = true, Device = DeviceClass.Phone, Dpi = 460f, Width = 1179, Height = 2556 };
                Assert.IsTrue(driver.Tick());
                Assert.AreNotSame(source, driver.Settings);
                Assert.AreEqual(PanelScaleMode.ConstantPhysicalSize, driver.Settings.scaleMode);
                Assert.AreEqual(160f, driver.Settings.referenceDpi, 0.01f);
                Assert.AreEqual(460f / 160f, driver.PixelsPerUnit, 0.001f);
                Assert.AreEqual(PanelScaleMode.ConstantPixelSize, source.scaleMode, "the shared asset is never edited");
                Assert.AreEqual(1f, source.scale);
            }
            finally
            {
                driver.Dispose();
                Object.DestroyImmediate(source);
            }
        }

        [Test]
        public void PanelScaleDriver_OnADesktopWindow_DerivesAPixelScaleFromTheWindow()
        {
            PanelSettings source = NewSettings();
            PanelScaleDriver driver = new PanelScaleDriver(source, PanelScaleProfile.CardPortrait());
            try
            {
                driver.EnvironmentSource = () => new PanelScaleEnvironment { Handheld = false, Device = DeviceClass.Desktop, Dpi = 96f, Width = 3840, Height = 2160 };
                driver.Tick();
                Assert.AreEqual(PanelScaleMode.ConstantPixelSize, driver.Settings.scaleMode);
                Assert.Greater(driver.Settings.scale, 1f);
                Assert.LessOrEqual(driver.Settings.scale, 4f);
                Assert.AreEqual(driver.Settings.scale, driver.PixelsPerUnit, 0.001f);
            }
            finally
            {
                driver.Dispose();
                Object.DestroyImmediate(source);
            }
        }

        [Test]
        public void PanelScaleDriver_ReevaluatesOnlyWhenTheEnvironmentOrTheUserScaleChanges()
        {
            PanelSettings source = NewSettings();
            PanelScaleDriver driver = new PanelScaleDriver(source, PanelScaleProfile.CardPortrait());
            try
            {
                int applied = 0;
                driver.Applied += scale => applied++;
                PanelScaleEnvironment environment = new PanelScaleEnvironment { Handheld = false, Device = DeviceClass.Desktop, Dpi = 96f, Width = 1280, Height = 720 };
                driver.EnvironmentSource = () => environment;
                Assert.IsTrue(driver.Tick());
                Assert.IsFalse(driver.Tick(), "nothing changed");
                environment.Width = 1920;
                environment.Height = 1080;
                Assert.IsTrue(driver.Tick(), "the window grew");
                driver.UserScale = 1.25f;
                Assert.IsTrue(driver.Tick(), "the player changed the UI size");
                Assert.AreEqual(3, applied);
            }
            finally
            {
                driver.Dispose();
                Object.DestroyImmediate(source);
            }
        }

        [Test]
        public void PanelScaleDriver_DisposeDestroysTheCopy_AndStopsTicking()
        {
            PanelSettings source = NewSettings();
            PanelScaleDriver driver = new PanelScaleDriver(source, PanelScaleProfile.CardPortrait());
            PanelSettings copy = driver.Settings;
            driver.Dispose();
            Assert.IsTrue(copy == null, "destroyed");
            Assert.IsFalse(driver.Tick());
            driver.Dispose();
            Object.DestroyImmediate(source);
        }

        [Test]
        public void PanelScaleDriver_RefusesANullSource()
        {
            Assert.Throws<System.ArgumentNullException>(() => new PanelScaleDriver(null, PanelScaleProfile.CardPortrait()));
        }

        // ---- panel kind ----

        [Test]
        public void UiPanelKinds_AnElementWithoutAPanelIsDetached()
        {
            Assert.AreEqual(UiPanelKind.Detached, UiPanelKinds.Detect(null));
            Assert.AreEqual(UiPanelKind.Detached, UiPanelKinds.Detect(new VisualElement()));
        }

        // ---- element defaults ----

        [Test]
        public void ListRow_IsFocusable_ShowsTitleSubtitleValueAndSelection()
        {
            ListRow row = new ListRow { Title = "Language", Subtitle = "Interface", Value = "English" };
            Assert.IsTrue(row.focusable);
            Assert.AreEqual("Language", row.TitleLabel.text);
            Assert.AreEqual(DisplayStyle.Flex, row.SubtitleLabel.style.display.value);
            Assert.AreEqual("English", row.ValueLabel.text);
            row.Subtitle = string.Empty;
            Assert.AreEqual(DisplayStyle.None, row.SubtitleLabel.style.display.value);
            row.Selected = true;
            Assert.IsTrue(row.ClassListContains(ListRow.SelectedClassName));
            row.ShowChevron = false;
            Assert.AreEqual(DisplayStyle.None, row.Chevron.style.display.value);
            Assert.IsFalse(row.HasIcon);
            row.HasIcon = true;
            Assert.IsTrue(row.HasIcon);
        }

        [Test]
        public void TopBar_PadsOnTopLeftRight_AndHidesTheBackButtonOnRequest()
        {
            TopBar bar = new TopBar { Title = "Settings" };
            Assert.AreEqual("top,left,right", bar.Edges);
            Assert.AreEqual("Settings", bar.TitleLabel.text);
            bar.ShowBack = false;
            Assert.AreEqual(DisplayStyle.None, bar.BackButton.style.display.value);
            bar.ShowBack = true;
            Assert.AreEqual(DisplayStyle.Flex, bar.BackButton.style.display.value);
            Assert.IsNotNull(bar.Start);
            Assert.IsNotNull(bar.End);
            Assert.IsTrue(bar.Start.Contains(bar.BackButton));
        }

        [Test]
        public void BannerView_TakesTheKindClass_AndShowsItsButtonsOnlyWhenAsked()
        {
            BannerView banner = new BannerView { Message = "Offline", Kind = ToastKind.Warning };
            Assert.IsTrue(banner.ClassListContains("ds-toast--warning"));
            Assert.IsFalse(banner.ClassListContains("ds-toast--info"));
            Assert.AreEqual(DisplayStyle.None, banner.ActionButton.style.display.value);
            banner.ActionText = "Retry";
            Assert.AreEqual(DisplayStyle.Flex, banner.ActionButton.style.display.value);
            Assert.AreEqual(DisplayStyle.None, banner.DismissButton.style.display.value);
            banner.Dismissible = true;
            Assert.AreEqual(DisplayStyle.Flex, banner.DismissButton.style.display.value);
        }

        [Test]
        public void StatCell_AndSectionHeader_CarryTheirClasses()
        {
            StatCell cell = new StatCell { Caption = "Score", Value = "120" };
            Assert.AreEqual("Score", cell.CaptionLabel.text);
            Assert.AreEqual("120", cell.ValueLabel.text);
            Assert.IsTrue(cell.ClassListContains(StatCell.UssClassName));
            SectionHeader header = new SectionHeader { text = "Sound" };
            Assert.IsTrue(header.ClassListContains(SectionHeader.UssClassName));
        }

        [Test]
        public void SafeAreaPanel_DefaultsToAllEdges_ParsesTheEdgesAttribute_AndNeverTakesPointerInput()
        {
            SafeAreaPanel panel = new SafeAreaPanel();
            Assert.AreEqual("all", panel.Edges);
            Assert.AreEqual(PickingMode.Ignore, panel.pickingMode);
            Assert.IsTrue(panel.ClassListContains(SafeAreaPanel.UssClassName));
            panel.Edges = "top, bottom";
            Assert.AreEqual("top, bottom", panel.Edges);
        }

        // ---- resources and source rules ----

        [Test]
        public void TheFoundationStyleSheets_AreFoundByResourcesLoad()
        {
            Assert.IsNotNull(Resources.Load<StyleSheet>(FoundationStyles.TokensResource));
            Assert.IsNotNull(Resources.Load<StyleSheet>(FoundationStyles.StylesResource));
        }

        [Test]
        public void FoundationStyles_InstallIsIdempotent_AndUninstallRemovesBoth()
        {
            VisualElement root = new VisualElement();
            Assert.IsTrue(FoundationStyles.Install(root));
            Assert.IsTrue(FoundationStyles.Install(root));
            Assert.AreEqual(2, root.styleSheets.count);
            FoundationStyles.Uninstall(root);
            Assert.AreEqual(0, root.styleSheets.count);
            Assert.IsFalse(FoundationStyles.Install(null));
        }

        [Test]
        public void EveryFoundationVariableOfTheSheet_IsDeclaredInTheTokensSheet()
        {
            string sheet = StripComments(File.ReadAllText(PackageFile(StylesFolder + "Foundation.uss")));
            string tokens = StripComments(File.ReadAllText(PackageFile(StylesFolder + "Foundation.Tokens.uss")));
            HashSet<string> declared = new HashSet<string>();
            foreach (Match match in Regex.Matches(tokens, @"(--fdp-foundation-[a-z0-9-]+)\s*:"))
                declared.Add(match.Groups[1].Value);
            Assert.Greater(declared.Count, 0);
            foreach (Match match in Regex.Matches(sheet, @"var\((--fdp-foundation-[a-z0-9-]+)"))
                Assert.IsTrue(declared.Contains(match.Groups[1].Value), match.Groups[1].Value + " is used but not declared in Foundation.Tokens.uss");
        }

        [Test]
        public void EveryVariableOfTheSheet_HasALiteralFallback_SoTheSheetWorksWithoutTheDesignSystem()
        {
            string sheet = StripComments(File.ReadAllText(PackageFile(StylesFolder + "Foundation.uss")));
            foreach (Match match in Regex.Matches(sheet, @"var\((--[a-z0-9-]+)\s*(,?)"))
                Assert.AreEqual(",", match.Groups[2].Value, "var(" + match.Groups[1].Value + ") has no fallback");
        }

        [Test]
        public void TheFoundationSheets_UseNoUnsupportedUss()
        {
            foreach (string file in new[] { "Foundation.uss", "Foundation.Tokens.uss" })
            {
                string sheet = StripComments(File.ReadAllText(PackageFile(StylesFolder + file)));
                foreach (string forbidden in new[] { @"\bgap\s*:", @"calc\(", @"@media", @"@keyframes", @"z-index", @"box-shadow", @"::", @"\s[+~]\s" })
                    Assert.IsFalse(Regex.IsMatch(sheet, forbidden), file + " uses " + forbidden);
            }
        }

        [Test]
        public void TheFoundationViews_NeverTouchTheLegacyInputClassOrUnityEngineScreen()
        {
            string[] files = Directory.GetFiles(PackageFile("Runtime/Foundation/Views"), "*.cs");
            Assert.Greater(files.Length, 10);
            foreach (string file in files)
            {
                string code = File.ReadAllText(file);
                string name = Path.GetFileName(file);
                Assert.IsFalse(Regex.IsMatch(code, @"\bInput\.(Get|mouse|touch|any)"), name + " uses UnityEngine.Input");
                Assert.IsFalse(code.Contains("UnityEngine.InputSystem"), name + " uses the Input System");
                Assert.IsFalse(Regex.IsMatch(code, @"(?<!Device\.)\bScreen\.(width|height|dpi|safeArea)"), name + " reads UnityEngine.Screen directly; use UnityEngine.Device.Screen");
            }
        }

        static string StripComments(string text)
        {
            return Regex.Replace(text, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);
        }
    }
}
