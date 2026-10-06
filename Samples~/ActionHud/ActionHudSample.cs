using System;
using FutureDigitalPlus.UIKit.Foundation;
using FutureDigitalPlus.UIKit.Hud;
using FutureDigitalPlus.UIKit.Interaction;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Samples.ActionHud
{
    /// <summary>
    /// A heads-up display over a 3D scene, the way an action game or shooter would build it. Put it on an empty GameObject in an empty scene and press
    /// Play. The component builds a camera, a ground plane and an enemy that walks a wide circle (sometimes off screen), then a <see cref="UiShell"/> whose
    /// HUD layer holds: a health meter with a damage trail, a stamina meter, a coin counter that counts up, a wave countdown, three cooldown icons, a
    /// kill feed, a charge ring, floating damage numbers and a name plate pinned to the enemy that turns into an edge arrow when the enemy leaves the
    /// screen. The pause button, Escape and the gamepad B button open the shell's <see cref="PauseMenu"/> on the router.
    /// <para>
    /// The HUD document keeps <see cref="InputProfile.NavigationEnabled"/> off, so WASD and the D-pad never move UI focus while the game is running; the
    /// switch is turned on only while the pause menu is on top.
    /// </para>
    /// </summary>
    [AddComponentMenu("Future Digital Plus UI Kit/Action HUD Sample")]
    public sealed class ActionHudSample : MonoBehaviour
    {
        private const string ThemeResource = "FutureDigitalPlus/UIKit/Samples/ActionHud/UnityDefaultRuntimeTheme";
        private const string IconFolder = "FutureDigitalPlus/UIKit/DesignSystem/Icons/";
        private const string GameScreen = "Game";
        private const float MaxHealth = 100f;
        private const float EnemyMaxHealth = 120f;
        private const float WaveSeconds = 90f;

        private static readonly string[] SkillIcons = { "bolt", "shield", "fire" };
        private static readonly float[] SkillCooldowns = { 3f, 8f, 15f };

        private PanelSettings _settings;
        private UIDocument _document;
        private InputProfile _input;
        private BackKeySource _back;
        private UiShell _shell;
        private Camera _camera;
        private Transform _enemy;
        private VisualElement _hud;

        private MeterBar _health;
        private MeterBar _stamina;
        private MeterBar _enemyHealth;
        private CurrencyLabel _coins;
        private CountdownLabel _wave;
        private RadialProgress _charge;
        private TimedFeed _feed;
        private FloatingTextLayer _floating;
        private readonly CooldownIcon[] _skills = new CooldownIcon[SkillIcons.Length];

        private float _playerHealth = MaxHealth;
        private float _enemyHealthValue = EnemyMaxHealth;
        private long _coinCount;
        private float _chargeValue;
        private bool _paused;

        private void Start()
        {
            BuildScene();
            BuildDocument();
            BuildShell();
            BuildHud(_shell.Context.GetLayer(UiLayer.Hud));
            RefreshBars();
        }

        private void Update()
        {
            if (_shell == null)
                return;

            _shell.Tick();
            if (_paused)
                return;

            // The enemy walks a wide ellipse, so it leaves the screen now and then and the edge arrow shows.
            var t = Time.time * 0.5f;
            _enemy.position = new Vector3(Mathf.Sin(t) * 14f, 0.5f, 4f + Mathf.Cos(t) * 5f);

            _stamina.Value = Mathf.Min(_stamina.MaxValue, _stamina.Value + 12f * Time.deltaTime);
            _chargeValue = Mathf.Repeat(_chargeValue + 0.25f * Time.deltaTime, 1f);
            _charge.Value = _chargeValue;
        }

        private void OnApplicationFocus(bool focus)
        {
            _shell?.NotifyApplicationFocus(focus);
        }

        private void OnDestroy()
        {
            Time.timeScale = 1f;
            _back?.Dispose();
            _shell?.Dispose();
            _input?.Dispose();
            if (_settings != null)
                Destroy(_settings);
        }

        // ---- scene -----------------------------------------------------------------------------------------------

        private void BuildScene()
        {
            _camera = new GameObject("HUD sample camera").AddComponent<Camera>();
            _camera.transform.SetParent(transform, false);
            _camera.transform.SetPositionAndRotation(new Vector3(0f, 7f, -9f), Quaternion.Euler(30f, 0f, 0f));
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.07f, 0.09f, 0.14f);

            var light = new GameObject("HUD sample light").AddComponent<Light>();
            light.transform.SetParent(transform, false);
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.transform.SetParent(transform, false);
            ground.transform.localScale = new Vector3(4f, 1f, 4f);

            var enemy = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            enemy.name = "Enemy";
            enemy.transform.SetParent(transform, false);
            _enemy = enemy.transform;
        }

        // ---- UI plumbing -----------------------------------------------------------------------------------------

        private void BuildDocument()
        {
            _settings = ScriptableObject.CreateInstance<PanelSettings>();
            _settings.name = "ActionHudPanelSettings";
            _settings.themeStyleSheet = Resources.Load<ThemeStyleSheet>(ThemeResource);
            _settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            _settings.referenceResolution = new Vector2Int(1280, 720);
            _settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            _settings.match = 0.5f;
            _settings.sortingOrder = 10;

            // Inactive while configured, so the UIDocument builds its tree once with everything set.
            var go = new GameObject("HUD sample (UIDocument)");
            go.transform.SetParent(transform, false);
            go.SetActive(false);
            _document = go.AddComponent<UIDocument>();
            _document.panelSettings = _settings;
            go.SetActive(true);

            var root = _document.rootVisualElement;
            // Style sheets: the design system on the panel root, then the modules on the document root.
            FoundationStyles.InstallDesignSystem(root.parent ?? root);
            HudStyles.Install(root);
            InteractionStyles.Install(root);

            // The HUD must not react to WASD and the D-pad while the game runs (the game reads the same keys).
            _input = InputProfile.Bind(root);
            _input.NavigationEnabled = false;
        }

        private void BuildShell()
        {
            var options = new UiShellOptions
            {
                InputModeSource = new InputProfileSource(_input),
                TransitionsEnabled = true,
            };
            _shell = new UiShell(_document.rootVisualElement, options);

            // A router needs a start screen; the game itself is not a screen, so this one is empty and lets every touch through.
            _shell.Catalog.Register(GameScreen, () => new GameScreenStub());

            var pause = new PauseMenuOptions();
            pause.Items.Add(new PauseMenuItem(UiText.FromLiteral("Restart wave"), ui =>
            {
                ResetFight();
                ui.Router.Pop();
            }));
            PauseMenu.Register(_shell.Catalog, pause);
            _shell.Boot(GameScreen);
            // Subscribe after Boot: the first route change (to the empty game screen) happens before the HUD exists.
            _shell.Router.Changed += OnRouteChanged;

            // Escape, the Android back button and gamepad B arrive here; at the game screen nothing consumes them, so they open the pause menu.
            _back = new BackKeySource();
            _back.Pressed += () =>
            {
                if (!_shell.HandleBackPress())
                    _shell.Router.Push(PauseMenu.DefaultId);
            };
        }

        private void OnRouteChanged(string from, string to)
        {
            _paused = to == PauseMenu.DefaultId;
            _input.NavigationEnabled = _paused;
            _hud.SetEnabled(!_paused);
            // A gamepad needs something focused to move from: focus the first button of the pause menu one frame after it appeared.
            if (_paused)
                _shell.Root.schedule.Execute(() => UiFocus.FocusFirst(_shell.Context.GetLayer(UiLayer.Screens)));
        }

        // ---- HUD -------------------------------------------------------------------------------------------------

        private void BuildHud(VisualElement layer)
        {
            // Everything lives in a safe-area panel, so no widget sits under a notch; the zones inside it are pass-through.
            var safe = new SafeAreaPanel { name = "hud-safe" };
            Fill(safe);
            layer.Add(safe);
            _hud = safe;

            var zones = new HudZones();
            Fill(zones);
            safe.Add(zones);

            // Top left: health and stamina.
            var vitals = new VisualElement();
            vitals.style.width = 260;
            _health = new MeterBar { LabelMode = MeterLabelMode.ValueAndMax, Size = MeterBarSize.Large, MaxValue = MaxHealth, Value = MaxHealth };
            _stamina = new MeterBar { Size = MeterBarSize.Small, MaxValue = 100f, Value = 100f, Segments = 5 };
            _stamina.style.marginTop = 6;
            vitals.Add(_health);
            vitals.Add(_stamina);
            zones.Add(HudZone.TopLeft, vitals);

            // Top centre: the wave timer.
            _wave = new CountdownLabel();
            _wave.StartIn(WaveSeconds);
            _wave.Expired += () => _feed.Push("Wave complete", "wave", "success");
            zones.Add(HudZone.TopCenter, _wave);

            // Top right: coins and the pause button.
            var top = new VisualElement();
            top.style.flexDirection = FlexDirection.Row;
            top.style.alignItems = Align.Center;
            _coins = new CurrencyLabel { Format = NumberFormatStyle.Grouped };
            _coins.SetIcon(Background.FromVectorImage(Resources.Load<VectorImage>(IconFolder + "coin")));
            var pauseButton = new Button(() => _shell.Router.Push(PauseMenu.DefaultId)) { text = "II", tooltip = "Pause" };
            pauseButton.AddToClassList("ds-btn");
            pauseButton.AddToClassList("ds-btn--secondary");
            pauseButton.style.marginLeft = 12;
            top.Add(_coins);
            top.Add(pauseButton);
            zones.Add(HudZone.TopRight, top);

            // Bottom left: kill and loot feed.
            _feed = new TimedFeed { MaxEntries = 5, TimeToLive = 4f };
            zones.Add(HudZone.BottomLeft, _feed);

            // Bottom centre: three skills, each a focusable slot (Space or Enter on a keyboard, A on a gamepad, a click or a tap).
            var skillRow = new VisualElement();
            skillRow.style.flexDirection = FlexDirection.Row;
            for (var i = 0; i < SkillIcons.Length; i++)
            {
                var index = i;
                var icon = new CooldownIcon { ShowText = true };
                icon.SetIcon(Background.FromVectorImage(Resources.Load<VectorImage>(IconFolder + SkillIcons[i])));
                _skills[i] = icon;

                var slot = new VisualElement { tooltip = "Skill " + (i + 1) };
                slot.style.marginLeft = slot.style.marginRight = 6;
                slot.Add(icon);
                Activate.Register(slot, () => CastSkill(index));
                skillRow.Add(slot);
            }
            zones.Add(HudZone.BottomCenter, skillRow);

            // Bottom right: the actions that stand in for mouse, keyboard and gamepad bindings in this sample, plus a charge ring.
            var actions = new VisualElement();
            actions.style.flexDirection = FlexDirection.Row;
            actions.style.alignItems = Align.Center;
            _charge = new RadialProgress { Thickness = 6f, SmoothSeconds = 0.15f };
            _charge.style.width = _charge.style.height = 44;
            _charge.style.marginRight = 12;
            actions.Add(_charge);
            actions.Add(ActionButton("Hit", Hit));
            actions.Add(ActionButton("Heal", Heal));
            zones.Add(HudZone.BottomRight, actions);

            // Layers above the zones: floating numbers and the world-anchored name plate. Both are full-size, transparent and pass-through.
            _floating = new FloatingTextLayer();
            Fill(_floating);
            safe.Add(_floating);

            var anchors = new WorldAnchorLayer { Camera = _camera, EdgeMargin = 40f };
            Fill(anchors);
            safe.Add(anchors);

            _enemyHealth = new MeterBar { Size = MeterBarSize.Small, MaxValue = EnemyMaxHealth, Value = EnemyMaxHealth, ShowTrail = true };
            _enemyHealth.style.width = 90;
            var plate = new VisualElement();
            plate.style.alignItems = Align.Center;
            plate.Add(new Label("Slime") { pickingMode = PickingMode.Ignore });
            plate.Add(_enemyHealth);
            // Clamp keeps the plate on the edge of the layer, with an arrow, when the enemy is off screen.
            anchors.Pin(plate, _enemy, Vector3.up * 1.2f, OffScreenMode.Clamp, AnchorPivot.Above);
        }

        private Button ActionButton(string caption, Action onClick)
        {
            var button = new Button(onClick) { text = caption };
            button.AddToClassList("ds-btn");
            button.AddToClassList("ds-btn--primary");
            button.style.marginLeft = 6;
            return button;
        }

        // ---- game rules (a few lines, only to drive the widgets) ---------------------------------------------------

        private void Hit()
        {
            if (_paused || _stamina.Value < 15f)
                return;
            _stamina.Value -= 15f;

            var critical = UnityEngine.Random.value < 0.25f;
            var damage = UnityEngine.Random.Range(8, 18) * (critical ? 2 : 1);
            _enemyHealthValue = Mathf.Max(0f, _enemyHealthValue - damage);
            _enemyHealth.Value = _enemyHealthValue;

            var spot = RuntimePanelUtils.CameraTransformWorldToPanel(_document.rootVisualElement.panel, _enemy.position + Vector3.up, _camera);
            _floating.Spawn(spot, damage.ToString(), critical ? FloatingTextKind.Critical : FloatingTextKind.Normal);

            // The enemy hits back a little, so the damage trail on the health bar has something to show.
            _playerHealth = Mathf.Max(0f, _playerHealth - UnityEngine.Random.Range(2, 9));
            _health.Value = _playerHealth;

            if (_enemyHealthValue <= 0f)
            {
                _coinCount += 25;
                _coins.Value = _coinCount;
                _feed.Push("Slime defeated", "kill", "kill");
                _feed.Push("+25 gold", "gold", "loot");
                _enemyHealthValue = EnemyMaxHealth;
                _enemyHealth.Set(EnemyMaxHealth, EnemyMaxHealth);
            }
        }

        private void Heal()
        {
            if (_paused)
                return;
            _playerHealth = Mathf.Min(MaxHealth, _playerHealth + 20f);
            _health.Value = _playerHealth;
            // An element's worldBound is already in panel space, so text can also start from a widget.
            _floating.Spawn(_health.worldBound.center, "+20", FloatingTextKind.Heal);
            _feed.Push("Healed", "heal", "loot");
        }

        private void CastSkill(int index)
        {
            if (_paused || !_skills[index].IsReady)
                return;
            _skills[index].StartCooldown(SkillCooldowns[index]);
            _feed.Push("Skill " + (index + 1), "skill" + index);
        }

        private void ResetFight()
        {
            _playerHealth = MaxHealth;
            _enemyHealthValue = EnemyMaxHealth;
            _enemyHealth.Set(EnemyMaxHealth, EnemyMaxHealth);
            _health.Set(MaxHealth, MaxHealth);
            _wave.StartIn(WaveSeconds);
            foreach (var skill in _skills)
                skill.ResetCooldown();
        }

        private void RefreshBars()
        {
            _health.Set(_playerHealth, MaxHealth);
            _coins.SnapTo(_coinCount);
        }

        private static void Fill(VisualElement element)
        {
            element.style.position = Position.Absolute;
            element.style.left = element.style.right = element.style.top = element.style.bottom = 0;
        }

        // ---- small helper types ----------------------------------------------------------------------------------

        /// <summary>The router's start screen: an empty page that does not take pointer input, so the game world stays reachable.</summary>
        private sealed class GameScreenStub : UiScreen
        {
            public override PickingMode RootPicking => PickingMode.Ignore;
        }

        /// <summary>Lets the shell's layout classes follow the device the <see cref="InputProfile"/> sees (the two modules are linked by this one seam).</summary>
        private sealed class InputProfileSource : IUiInputModeSource
        {
            private readonly InputProfile _profile;

            public InputProfileSource(InputProfile profile)
            {
                _profile = profile;
                _profile.ModeChanged += kind => ModeChanged?.Invoke(Map(kind));
            }

            public UiInputMode Mode => Map(_profile.Mode);

            public event Action<UiInputMode> ModeChanged;

            private static UiInputMode Map(InputDeviceKind kind)
            {
                switch (kind)
                {
                    case InputDeviceKind.Touch: return UiInputMode.Touch;
                    case InputDeviceKind.Keyboard: return UiInputMode.Keyboard;
                    case InputDeviceKind.Gamepad: return UiInputMode.Gamepad;
                    default: return UiInputMode.Pointer;
                }
            }
        }
    }
}
