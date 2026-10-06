using Recipe0;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using FutureDigitalPlus.UIKit.Foundation;
using FutureDigitalPlus.UIKit.Language;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Applier;

public class RecipeTests
{
    PanelSettings _settings;
    GameObject _go;
    UIDocument _doc;

    static void Set(object o, string field, object v)
    {
        var f = o.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        Assert.IsNotNull(f, field);
        f.SetValue(o, v);
    }

    GameUi MakeGameUi(bool activate = true)
    {
        PlayerPrefs.DeleteKey("ui.theme");
        _settings = ScriptableObject.CreateInstance<PanelSettings>();
        _settings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("DocsCheckRuntimeTheme");
        _settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
        _settings.referenceResolution = new Vector2Int(1080, 1920);
        _go = new GameObject("ui");
        _go.SetActive(false);
        _doc = _go.AddComponent<UIDocument>();
        _doc.panelSettings = _settings;
        var ui = _go.AddComponent<GameUi>();
        Set(ui, "document", _doc);
        _go.SetActive(true);
        return ui;
    }

    [TearDown]
    public void Down()
    {
        if (_go) Object.Destroy(_go);
        if (_settings) Object.Destroy(_settings);
        UiLocalization.Default.Backend = null;
        UiLocalization.Default.DefaultTables = null;
        UiAccessibility.Reset();
        PlayerPrefs.DeleteKey("ui.theme");
    }

    [UnityTest]
    public IEnumerator R0_Bootstrap_BuildsAndSetsClasses()
    {
        var ui = MakeGameUi();
        yield return null;
        var root = _doc.rootVisualElement;
        Assert.IsNotNull(ui.Shell);
        var classes = string.Join(" ", root.GetClasses());
        Debug.Log("DOC root classes: " + classes);
        Debug.Log("DOC shell classes: " + string.Join(" ", ui.Shell.Root.GetClasses()));
        Debug.Log("DOC panel root classes: " + string.Join(" ", root.parent.GetClasses()));
        Assert.IsTrue(root.GetClasses().Any(c => c.StartsWith("fdp-input-")), classes);
        Assert.IsTrue(root.GetClasses().Any(c => c.StartsWith("fdp-lang-")), classes);
        Assert.IsTrue(ui.Shell.Root.GetClasses().Contains("ds-root"));
        // disable/enable twice must not throw or leak
        _go.SetActive(false); yield return null; _go.SetActive(true); yield return null;
    }

    [UnityTest]
    public IEnumerator R1_ThemedScreen_FromUxml()
    {
        var ui = MakeGameUi();
        var startup = _go.AddComponent<Recipe1.Startup>();
        Set(startup, "gameUi", ui);
        yield return null; yield return null; yield return null;
        var title = ui.Shell.Root.Q<Label>("title");
        Assert.IsNotNull(title, "UXML title label");
        Assert.AreEqual("Hello", title.text);
        Assert.IsNotNull(ui.Shell.Root.Q<Button>("play"));
        Assert.AreEqual("Home", ui.Shell.Router.Current);
    }

    [UnityTest]
    public IEnumerator R2_Dialogs()
    {
        var ui = MakeGameUi();
        yield return null;
        Recipe2.Dialogs.Examples(ui.Shell.Context);
        yield return null; yield return null;
        Assert.IsTrue(ui.Shell.Context.Dialogs.IsModalOpen);
        // press back to cycle through all five
        for (int i = 0; i < 6; i++) { ui.Shell.HandleBackPress(); yield return null; yield return null; }
    }

    [UnityTest]
    public IEnumerator R3_ToastsLoading()
    {
        var ui = MakeGameUi();
        yield return null;
        Recipe3.Feedback.Toasts(ui.Shell.Context);
        var t = Recipe3.Feedback.LoadWithOverlay(ui.Shell.Context);
        yield return null;
        Assert.IsTrue(ui.Shell.Context.Loading.BlocksInput);
        float end = Time.realtimeSinceStartup + 4f;
        while (!t.IsCompleted && Time.realtimeSinceStartup < end) yield return null;
        Assert.IsTrue(t.IsCompleted);
        yield return null;
        Assert.IsFalse(ui.Shell.Context.Loading.BlocksInput);
    }

    [UnityTest]
    public IEnumerator R4_OptionsScreen()
    {
        var ui = MakeGameUi();
        yield return null;
        ui.Shell.Catalog.Register("Options", () => new Recipe4.OptionsScreen(ui.Profile));
        ui.Shell.Catalog.Register("Home", () => new Recipe1.HomeScreen());
        ui.Shell.Boot("Home");
        ui.Shell.Router.Push("Options");
        yield return null; yield return null; yield return null;
        Assert.AreEqual("Options", ui.Shell.Router.Current);
        ui.Shell.HandleBackPress();
        yield return null;
        Assert.AreEqual("Home", ui.Shell.Router.Current);
    }

    [UnityTest]
    public IEnumerator R5_Hud()
    {
        var ui = MakeGameUi();
        yield return null;
        var hud = _go.AddComponent<Recipe5.PlayerHud>();
        Set(hud, "gameUi", ui);
        yield return null; yield return null;
        hud.Damage(30); hud.AddCoins(250); hud.Dash();
        yield return null;
        Assert.IsNotNull(ui.Shell.Root.Q<FutureDigitalPlus.UIKit.Hud.MeterBar>());
    }

    [UnityTest]
    public IEnumerator R6_Inventory()
    {
        var ui = MakeGameUi();
        yield return null;
        var bag = Recipe6.BackpackScreen.NewBag();
        ui.Shell.Catalog.Register("Backpack", () => new Recipe6.BackpackScreen(bag, Texture2D.whiteTexture));
        ui.Shell.Boot("Backpack");
        yield return null; yield return null;
        var grid = ui.Shell.Root.Q<FutureDigitalPlus.UIKit.Gameplay.SlotGrid>();
        Assert.IsNotNull(grid);
        Assert.AreEqual(10, bag[0].Count);
        Assert.AreEqual(4, bag[1].Count);
    }

    [UnityTest]
    public IEnumerator R7_Texts()
    {
        var ui = MakeGameUi();
        _go.SetActive(false);
        var tx = _go.AddComponent<Recipe7.Texts>();
        Set(tx, "csv", new TextAsset("table,key,en,de,ar\nUI,PLAY,Play,Spielen,العب\nUI,SCORE,\"Score: {0}\",Punkte: {0},x\n"));
        _go.SetActive(true);
        yield return null; yield return null;
        var play = new Button(); var score = new Label();
        ui.Shell.Root.Add(play); ui.Shell.Root.Add(score);
        Recipe7.Texts.Bind(play, score);
        yield return null;
        Assert.AreEqual("Play", play.text);
        tx.SetLanguage("de");
        yield return null;
        Assert.AreEqual("Spielen", play.text);
        tx.SetLanguage("ar");
        yield return null;
        Assert.IsTrue(_doc.rootVisualElement.ClassListContains("fdp-rtl"));
    }

    [UnityTest]
    public IEnumerator R8_BrandTheme()
    {
        var ui = MakeGameUi();
        yield return null;
        var b = _go.AddComponent<Recipe8.BrandThemes>();
        Set(b, "gameUi", ui);
        yield return null; yield return null;
        Assert.AreEqual("brand", ui.Themes.CurrentName);
        b.UseLight();
        yield return null;
        Assert.AreEqual("light", ui.Themes.CurrentName);
        PlayerPrefs.DeleteKey("ui.theme");
    }

    [UnityTest]
    public IEnumerator R9_Scale()
    {
        _settings = ScriptableObject.CreateInstance<PanelSettings>();
        _settings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("DocsCheckRuntimeTheme");
        _go = new GameObject("ui"); _go.SetActive(false);
        _doc = _go.AddComponent<UIDocument>();
        _doc.panelSettings = _settings;
        var sb = _go.AddComponent<Recipe9.ScaleBootstrap>();
        Set(sb, "document", _doc); Set(sb, "sourceSettings", _settings);
        var ui = _go.AddComponent<GameUi>();
        Set(ui, "document", _doc);
        _go.SetActive(true);
        yield return null;
        Assert.AreNotSame(_settings, _doc.panelSettings);
        Assert.IsTrue(ui.Shell.Root.panel == _doc.rootVisualElement.panel, "shell is on the live panel");
        Assert.IsTrue(ui.Shell.Root.parent != null && ui.Shell.Root.parent.panel != null);
        sb.SetUiSize(120);
        yield return null; yield return null;
        Assert.IsTrue(ui.Shell.Root.panel != null);
    }
}

public class GettingStartedTests
{
    [UnityTest]
    public IEnumerator Step2_UxmlWithHost_RendersCenteredCard()
    {
        PlayerPrefs.DeleteKey("ui.theme");
        var rt = new RenderTexture(800, 1200, 24);
        var settings = ScriptableObject.CreateInstance<PanelSettings>();
        settings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("DocsCheckRuntimeTheme");
        settings.targetTexture = rt;
        settings.scaleMode = PanelScaleMode.ConstantPixelSize;
        var go = new GameObject("doc"); go.SetActive(false);
        var doc = go.AddComponent<UIDocument>();
        doc.panelSettings = settings;
        doc.visualTreeAsset = Resources.Load<VisualTreeAsset>("GS/MainMenu");
        var host = go.AddComponent<FutureDigitalPlus.UIKit.UIKitHost>();
        host.Options.Theme = Resources.Load<FutureDigitalPlus.UIKit.DesignSystem.Theme.Data.ThemeData>("FutureDigitalPlus/UIKit/DesignSystem/Themes/Light");
        go.SetActive(true);
        yield return null; yield return null; yield return null;
        var root = doc.rootVisualElement;
        var dsRoot = root.Q(className: "ds-root");
        var card = root.Q(className: "ds-card");
        var btn = root.Q<Button>("play");
        Debug.Log("DOC gs root classes " + string.Join(" ", root.GetClasses()) + " | root h=" + root.resolvedStyle.height + " dsroot h=" + dsRoot.resolvedStyle.height + " w=" + dsRoot.resolvedStyle.width + " card w=" + card.resolvedStyle.width + " card x=" + card.worldBound.x + " btn h=" + btn.resolvedStyle.height + " dsroot bg=" + dsRoot.resolvedStyle.backgroundColor + " btn bg=" + btn.resolvedStyle.backgroundColor + " label color=" + root.Q<Label>().resolvedStyle.color);
        Assert.Greater(dsRoot.resolvedStyle.height, 1000f);
        Assert.AreEqual(320f, card.resolvedStyle.width, 1f);
        Assert.AreEqual(400f - 160f, card.worldBound.x, 2f);
        Assert.Greater(btn.resolvedStyle.height, 20f);
        Object.Destroy(go); Object.Destroy(settings); rt.Release();
    }
}

/// <summary>The UXML written in the module guides (Hud, Controls, Language, Extras) builds and its attributes arrive.</summary>
public class GuideUxmlTests
{
    [UnityTest]
    public IEnumerator GuideUxml_BuildsWithItsAttributes()
    {
        var settings = ScriptableObject.CreateInstance<PanelSettings>();
        settings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("DocsCheckRuntimeTheme");
        var go = new GameObject("doc"); go.SetActive(false);
        var doc = go.AddComponent<UIDocument>();
        doc.panelSettings = settings;
        doc.visualTreeAsset = Resources.Load<VisualTreeAsset>("DocsUxml");
        go.SetActive(true);
        yield return null; yield return null;
        var root = doc.rootVisualElement;
        var meter = root.Q<FutureDigitalPlus.UIKit.Hud.MeterBar>("meter");
        Assert.AreEqual(60f, meter.Value, 0.01f);
        Assert.AreEqual(100f, meter.MaxValue, 0.01f);
        var tabs = root.Q<FutureDigitalPlus.UIKit.Controls.TabBar>("tabs");
        Assert.AreEqual("home", tabs.value);
        var select = root.Q<FutureDigitalPlus.UIKit.Controls.SelectField>("select");
        Assert.AreEqual("Low,Medium,High", select.Entries);
        Assert.AreEqual("Quality", select.Placeholder);
        Assert.AreEqual("Play", root.Q<FutureDigitalPlus.UIKit.Language.LocalizedLabel>("label").text);
        Assert.IsNotNull(root.Q<FutureDigitalPlus.UIKit.Language.LocalizedButton>("button"));
        Assert.IsNotNull(root.Q("aspect"));
        Object.Destroy(go); Object.Destroy(settings);
    }
}
