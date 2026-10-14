using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FutureDigitalPlus.UIKit.Foundation.Responsive
{
    /// <summary>
    /// <c>Tools &gt; FDP UI Kit &gt; Responsive Manager</c>: review the game's screens on phones, tablets and the cover, main and inner screens of flip and fold phones,
    /// held upright or sideways, flat or half opened, and set the size and scale per form factor.
    /// Outside Play mode it lists every catalog screen with its size in units, its form factor and the scale it gets. In Play mode it renders the running game's panel
    /// as each device shows it (<see cref="ResponsivePreviewSession"/>), lists layout faults (<see cref="ResponsiveLayoutAudit"/>), opens the game's screens through
    /// its <see cref="IResponsivePreviewHost"/>, and checks every device in one go. Rules are edited in the project's <see cref="ResponsiveRulesAsset"/> and apply at once.
    /// </summary>
    public sealed class ResponsiveManagerWindow : EditorWindow
    {
        const float DeviceListWidth = 230f;
        const float SideWidth = 340f;
        const double SettleSeconds = 0.05;
        const double ScreenChangeSeconds = 0.8;
        const string DefaultCaptureFolder = "Temp/ResponsiveCaptures";

        static readonly ResponsiveOrientation[] Orientations = { ResponsiveOrientation.Any, ResponsiveOrientation.Portrait, ResponsiveOrientation.Landscape };

        [SerializeField] string _deviceId;
        [SerializeField] int _screenIndex;
        [SerializeField] bool _landscape;
        [SerializeField] bool _halfOpened;
        [SerializeField] bool _showSafeArea = true;
        [SerializeField] bool _showHinge = true;
        [SerializeField] string _filter = "";
        [SerializeField] int _hostScreen = -1;
        [SerializeField] bool _batchBothOrientations;
        [SerializeField] bool _batchAllScreens;
        [SerializeField] bool _batchSavePngs;
        [SerializeField] string _captureFolder = DefaultCaptureFolder;
        [SerializeField] Color _background = new Color(0.1f, 0.1f, 0.1f, 1f);
        [SerializeField] float _minTarget = ResponsiveLayoutAudit.DefaultMinTargetUnits;
        [SerializeField] int _formFoldouts = 1 << (int)DeviceFormFactor.Phone;

        ResponsiveDeviceCatalogData _catalog;
        readonly List<string> _catalogProblems = new List<string>();
        IResponsivePreviewHost _host;
        string _hostProblem;
        ResponsiveRulesAsset _rules;
        ResponsivePreviewSession _session;
        string _sessionProblem;
        List<ResponsiveLayoutIssue> _issues = new List<ResponsiveLayoutIssue>();
        bool _auditPending;
        double _auditMinSeconds;
        Vector2 _deviceScroll;
        Vector2 _sideScroll;
        Vector2 _overviewScroll;
        Vector2 _issueScroll;
        BatchRun _batch;
        List<BatchRow> _batchRows;
        double _lastRepaint;

        /// <summary>Opens the window.</summary>
        [MenuItem("Tools/FDP UI Kit/Responsive Manager", priority = 40)]
        public static void Open()
        {
            var window = GetWindow<ResponsiveManagerWindow>("Responsive Manager");
            window.minSize = new Vector2(980f, 560f);
        }

        void OnEnable()
        {
            Reload();
            EditorApplication.update += OnEditorUpdate;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            Undo.undoRedoPerformed -= OnUndoRedo;
            StopBatch();
            StopPreview();
        }

        void Reload()
        {
            _catalogProblems.Clear();
            _catalog = ResponsiveDeviceCatalog.Load(_catalogProblems);
            _host = FindHost(out _hostProblem);
            _rules = ResponsiveRulesStore.Find();
            if (SelectedDevice == null && _catalog.devices.Count > 0)
                _deviceId = _catalog.devices[0].id;
        }

        static IResponsivePreviewHost FindHost(out string problem)
        {
            problem = null;
            foreach (Type type in TypeCache.GetTypesDerivedFrom<IResponsivePreviewHost>())
            {
                if (type.IsAbstract || type.IsInterface || type.GetConstructor(Type.EmptyTypes) == null)
                    continue;
                try
                {
                    return (IResponsivePreviewHost)Activator.CreateInstance(type);
                }
                catch (Exception exception)
                {
                    problem = type.Name + ": " + exception.Message;
                }
            }
            return null;
        }

        PanelScaleProfile Profile
        {
            get { return _host != null ? _host.Profile : PanelScaleProfile.CardPortrait(); }
        }

        ResponsiveRules Rules
        {
            get { return _rules != null ? _rules.Rules : null; }
        }

        ResponsiveDeviceSpec SelectedDevice
        {
            get
            {
                if (_catalog == null || _catalog.devices == null)
                    return null;
                foreach (ResponsiveDeviceSpec device in _catalog.devices)
                {
                    if (device != null && device.id == _deviceId && device.screens != null && device.screens.Count > 0)
                        return device;
                }
                return null;
            }
        }

        ResponsiveScreenSpec SelectedScreen
        {
            get
            {
                ResponsiveDeviceSpec device = SelectedDevice;
                if (device == null)
                    return null;
                return device.screens[Mathf.Clamp(_screenIndex, 0, device.screens.Count - 1)];
            }
        }

        // ---- updates

        void OnEditorUpdate()
        {
            if (_session != null && !_session.IsAlive)
            {
                _session = null;
                _sessionProblem = "The game's panel went away.";
                Repaint();
            }
            if (_batch != null)
            {
                StepBatch();
                return;
            }
            if (_auditPending && _session != null)
            {
                EditorApplication.QueuePlayerLoopUpdate();
                if (_session.IsSettled(_auditMinSeconds))
                {
                    _auditPending = false;
                    _issues = ResponsiveLayoutAudit.Run(_session.PanelRoot, _minTarget);
                }
            }
            if (_session != null && EditorApplication.timeSinceStartup - _lastRepaint > 0.1)
            {
                _lastRepaint = EditorApplication.timeSinceStartup;
                Repaint();
            }
        }

        void OnPlayModeChanged(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingPlayMode)
            {
                StopBatch();
                StopPreview();
            }
            Repaint();
        }

        void OnUndoRedo()
        {
            ApplyPreview();
            Repaint();
        }

        // ---- preview

        void StartPreview()
        {
            StopPreview();
            _session = ResponsivePreviewSession.Start(out _sessionProblem);
            ApplyPreview();
        }

        void StopPreview()
        {
            if (_session == null)
                return;
            _session.Dispose();
            _session = null;
            _issues.Clear();
        }

        void ApplyPreview()
        {
            ResponsiveScreenSpec screen = SelectedScreen;
            if (_session == null || screen == null)
                return;
            _session.Show(screen, _landscape, _halfOpened ? DevicePosture.HalfOpened : DevicePosture.Flat, _background);
            RequestAudit(SettleSeconds);
        }

        void RequestAudit(double minSeconds)
        {
            // a pending wait for a screen change is not shortened by a later, shorter request
            _auditMinSeconds = _auditPending ? Math.Max(_auditMinSeconds, minSeconds) : minSeconds;
            _auditPending = true;
        }

        void ShowHostScreen(int index)
        {
            _hostScreen = index;
            if (_host == null || _session == null || index < 0 || index >= _host.Screens.Count)
                return;
            if (!_host.Show(_host.Screens[index]))
                _sessionProblem = "The game could not open " + _host.Screens[index].Label + " now.";
            _session.MarkChanged();
            RequestAudit(ScreenChangeSeconds);
        }

        // ---- drawing

        void OnGUI()
        {
            if (_catalog == null)
                Reload();
            using (new EditorGUILayout.HorizontalScope())
            {
                DrawDeviceList();
                using (new EditorGUILayout.VerticalScope())
                {
                    DrawToolbar();
                    if (_session != null)
                        DrawPreview();
                    else
                        DrawOverview();
                }
                DrawSide();
            }
        }

        void DrawDeviceList()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(DeviceListWidth)))
            {
                _filter = EditorGUILayout.TextField(_filter, EditorStyles.toolbarSearchField);
                _deviceScroll = EditorGUILayout.BeginScrollView(_deviceScroll);
                string group = null;
                foreach (ResponsiveDeviceSpec device in _catalog.devices)
                {
                    if (device == null || device.screens == null || device.screens.Count == 0)
                        continue;
                    if (!string.IsNullOrEmpty(_filter) && (device.name ?? "").IndexOf(_filter, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    if (device.group != group)
                    {
                        group = device.group;
                        EditorGUILayout.LabelField(group ?? "Other", EditorStyles.boldLabel);
                    }
                    bool selected = device.id == _deviceId;
                    string label = device.name + "  " + FormTags(device);
                    if (GUILayout.Toggle(selected, label, "Button") && !selected)
                        Select(device.id, 0);
                }
                EditorGUILayout.EndScrollView();
                if (_catalogProblems.Count > 0)
                    EditorGUILayout.HelpBox(string.Join("\n", _catalogProblems), MessageType.Warning);
                if (GUILayout.Button("Reload catalog and rules"))
                    Reload();
            }
        }

        string FormTags(ResponsiveDeviceSpec device)
        {
            var tags = new StringBuilder();
            foreach (ResponsiveScreenSpec screen in device.screens)
            {
                if (screen == null || screen.dpi <= 0f)
                    continue;
                tags.Append(tags.Length > 0 ? "/" : "").Append(ShortForm(screen.FormFactor(Profile)));
            }
            return tags.Length > 0 ? "(" + tags + ")" : "";
        }

        static string ShortForm(DeviceFormFactor form)
        {
            switch (form)
            {
                case DeviceFormFactor.Pad: return "pad";
                case DeviceFormFactor.FlipCover: return "flip";
                case DeviceFormFactor.FoldCover: return "cover";
                case DeviceFormFactor.FoldInner: return "inner";
                default: return "phone";
            }
        }

        void Select(string deviceId, int screen)
        {
            _deviceId = deviceId;
            _screenIndex = screen;
            ResponsiveScreenSpec spec = SelectedScreen;
            if (spec == null || spec.hinge == HingeAxis.None)
                _halfOpened = false;
            ApplyPreview();
            GUI.FocusControl(null);
        }

        void DrawToolbar()
        {
            ResponsiveDeviceSpec device = SelectedDevice;
            ResponsiveScreenSpec screen = SelectedScreen;
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (device != null && device.screens.Count > 1)
                {
                    for (int i = 0; i < device.screens.Count; i++)
                    {
                        bool on = GUILayout.Toggle(_screenIndex == i, device.screens[i].name, EditorStyles.toolbarButton);
                        if (on && _screenIndex != i)
                        {
                            Select(device.id, i);
                            screen = SelectedScreen;
                        }
                    }
                    GUILayout.Space(8f);
                }
                bool landscape = GUILayout.Toggle(_landscape, _landscape ? "Landscape" : "Portrait", EditorStyles.toolbarButton, GUILayout.Width(76f));
                if (landscape != _landscape)
                {
                    _landscape = landscape;
                    ApplyPreview();
                }
                using (new EditorGUI.DisabledScope(screen == null || screen.hinge == HingeAxis.None))
                {
                    bool half = GUILayout.Toggle(_halfOpened, _halfOpened ? "Half opened" : "Flat", EditorStyles.toolbarButton, GUILayout.Width(84f));
                    if (half != _halfOpened)
                    {
                        _halfOpened = half;
                        ApplyPreview();
                    }
                }
                GUILayout.Space(8f);
                _showSafeArea = GUILayout.Toggle(_showSafeArea, "Safe area", EditorStyles.toolbarButton);
                _showHinge = GUILayout.Toggle(_showHinge, "Hinge", EditorStyles.toolbarButton);
                GUILayout.FlexibleSpace();
                if (_session == null)
                {
                    using (new EditorGUI.DisabledScope(!Application.isPlaying))
                    {
                        if (GUILayout.Button("Start preview", EditorStyles.toolbarButton))
                            StartPreview();
                    }
                }
                else if (GUILayout.Button("Stop preview", EditorStyles.toolbarButton))
                {
                    StopPreview();
                }
            }
            if (screen != null)
                EditorGUILayout.LabelField(Describe(screen, _landscape), EditorStyles.miniLabel);
            if (!Application.isPlaying)
                EditorGUILayout.HelpBox("Enter Play mode and press Start preview to see the game's screens on the selected device.", MessageType.None);
            else if (EditorApplication.isPaused)
                EditorGUILayout.HelpBox("Play mode is paused: the preview updates when it runs.", MessageType.Info);
            if (!string.IsNullOrEmpty(_sessionProblem))
                EditorGUILayout.HelpBox(_sessionProblem, MessageType.Warning);
        }

        string Describe(ResponsiveScreenSpec screen, bool landscape)
        {
            screen.PixelSize(landscape, out int w, out int h);
            DeviceFormFactor form = screen.FormFactor(Profile);
            ResponsiveRule rule = Rules != null ? Rules.Resolve(form, landscape) : ResponsiveRule.Identity(form, ResponsiveOrientation.Any);
            PanelScale scale = screen.Scale(Profile, landscape, rule.Scale);
            float ppu = scale.PixelsPerUnit > 0f ? scale.PixelsPerUnit : 1f;
            string text = w + "x" + h + " px @ " + screen.dpi.ToString("0") + " dpi   " + (w / ppu).ToString("0") + "x" + (h / ppu).ToString("0") + " units   " +
                          DeviceFormFactors.DisplayName(form) + " (" + DeviceFormFactors.ClassName(form) + ")   scale x" + rule.Scale.ToString("0.00") +
                          "   " + ppu.ToString("0.00") + " px/unit";
            if (rule.MaxContentWidth > 0f)
                text += "   column " + rule.MaxContentWidth.ToString("0");
            if (_session != null && _session.Screen == screen)
                text += "   live: " + DeviceFormFactors.DisplayName(_session.Driver.FormFactor);
            return text;
        }

        void DrawPreview()
        {
            Rect area = GUILayoutUtility.GetRect(10f, 10000f, 10f, 10000f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            RenderTexture texture = _session.Texture;
            if (texture == null || Event.current.type != EventType.Repaint)
                return;
            float fit = Mathf.Min((area.width - 16f) / texture.width, (area.height - 16f) / texture.height);
            if (!(fit > 0f))
                return;
            var image = new Rect(0f, 0f, texture.width * fit, texture.height * fit) { center = area.center };
            EditorGUI.DrawRect(new Rect(image.x - 4f, image.y - 4f, image.width + 8f, image.height + 8f), new Color(0f, 0f, 0f, 0.6f));
            GUI.DrawTexture(image, texture, ScaleMode.StretchToFill, false);

            ResponsiveScreenSpec screen = _session.Screen;
            if (_showSafeArea && screen != null)
            {
                SafeAreaInsets insets = screen.Insets(_session.Landscape);
                var tint = new Color(1f, 0.2f, 0.2f, 0.25f);
                EditorGUI.DrawRect(new Rect(image.x, image.y, image.width, insets.Top * fit), tint);
                EditorGUI.DrawRect(new Rect(image.x, image.yMax - insets.Bottom * fit, image.width, insets.Bottom * fit), tint);
                EditorGUI.DrawRect(new Rect(image.x, image.y, insets.Left * fit, image.height), tint);
                EditorGUI.DrawRect(new Rect(image.xMax - insets.Right * fit, image.y, insets.Right * fit, image.height), tint);
            }
            if (_showHinge && _session.Hinge != HingeAxis.None)
            {
                Color line = _session.Posture == DevicePosture.HalfOpened ? new Color(1f, 0.8f, 0.1f, 0.85f) : new Color(1f, 1f, 1f, 0.25f);
                float band = _session.Posture == DevicePosture.HalfOpened ? 4f : 1f;
                if (_session.Hinge == HingeAxis.Horizontal)
                    EditorGUI.DrawRect(new Rect(image.x, image.center.y - band * 0.5f, image.width, band), line);
                else
                    EditorGUI.DrawRect(new Rect(image.center.x - band * 0.5f, image.y, band, image.height), line);
            }
        }

        void DrawOverview()
        {
            EditorGUILayout.LabelField("Every catalog screen with the profile " + (_host != null ? "of " + _host.GetType().Name : "CardPortrait (no IResponsivePreviewHost found)") +
                                       " and the rules" + (_rules != null ? " of " + AssetDatabase.GetAssetPath(_rules) : " (none: no rules asset)"), EditorStyles.wordWrappedMiniLabel);
            _overviewScroll = EditorGUILayout.BeginScrollView(_overviewScroll);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("Device / screen", EditorStyles.boldLabel, GUILayout.Width(220f));
                GUILayout.Label("Units", EditorStyles.boldLabel, GUILayout.Width(80f));
                GUILayout.Label("Form factor", EditorStyles.boldLabel, GUILayout.Width(90f));
                GUILayout.Label("Scale", EditorStyles.boldLabel, GUILayout.Width(50f));
                GUILayout.Label("Units (landscape)", EditorStyles.boldLabel);
            }
            foreach (ResponsiveDeviceSpec device in _catalog.devices)
            {
                if (device == null || device.screens == null)
                    continue;
                for (int i = 0; i < device.screens.Count; i++)
                {
                    ResponsiveScreenSpec screen = device.screens[i];
                    if (screen == null || screen.dpi <= 0f)
                        continue;
                    DeviceFormFactor form = screen.FormFactor(Profile);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        string label = device.screens.Count > 1 ? device.name + " " + screen.name : device.name;
                        if (GUILayout.Button(label, device.id == _deviceId && i == _screenIndex ? EditorStyles.boldLabel : EditorStyles.label, GUILayout.Width(220f)))
                            Select(device.id, i);
                        GUILayout.Label(Units(screen, false), GUILayout.Width(80f));
                        GUILayout.Label(DeviceFormFactors.DisplayName(form), GUILayout.Width(90f));
                        ResponsiveRule rule = Rules != null ? Rules.Resolve(form, false) : ResponsiveRule.Identity(form, ResponsiveOrientation.Any);
                        GUILayout.Label("x" + rule.Scale.ToString("0.00"), GUILayout.Width(50f));
                        GUILayout.Label(Units(screen, true));
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }

        string Units(ResponsiveScreenSpec screen, bool landscape)
        {
            DeviceFormFactor form = screen.FormFactor(Profile);
            ResponsiveRule rule = Rules != null ? Rules.Resolve(form, landscape) : ResponsiveRule.Identity(form, ResponsiveOrientation.Any);
            float ppu = screen.Scale(Profile, landscape, rule.Scale).PixelsPerUnit;
            if (!(ppu > 0f))
                return "-";
            screen.PixelSize(landscape, out int w, out int h);
            return (w / ppu).ToString("0") + "x" + (h / ppu).ToString("0");
        }

        void DrawSide()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(SideWidth)))
            {
                _sideScroll = EditorGUILayout.BeginScrollView(_sideScroll);
                DrawScreens();
                DrawIssues();
                DrawRules();
                DrawBatch();
                EditorGUILayout.EndScrollView();
            }
        }

        void DrawScreens()
        {
            EditorGUILayout.LabelField("Game screen", EditorStyles.boldLabel);
            if (_host == null)
            {
                EditorGUILayout.HelpBox(_hostProblem ?? "Implement IResponsivePreviewHost in an editor assembly of the game to pick screens here. The open screen is previewed.", MessageType.None);
            }
            else
            {
                IReadOnlyList<ResponsivePreviewScreen> screens = _host.Screens;
                var labels = new string[screens.Count];
                for (int i = 0; i < screens.Count; i++)
                    labels[i] = screens[i].Label;
                using (new EditorGUI.DisabledScope(_session == null))
                {
                    int picked = EditorGUILayout.Popup(_hostScreen, labels);
                    if (picked != _hostScreen)
                        ShowHostScreen(picked);
                }
            }
            using (new EditorGUILayout.HorizontalScope())
            using (new EditorGUI.DisabledScope(_session == null))
            {
                if (GUILayout.Button("Page up"))
                    Scroll(-1);
                if (GUILayout.Button("Page down"))
                    Scroll(1);
                if (GUILayout.Button("Save PNG"))
                    SaveCurrentPng();
            }
            _background = EditorGUILayout.ColorField("Background", _background);
        }

        void Scroll(int pages)
        {
            ResponsiveLayoutAudit.ScrollPages(_session.PanelRoot, pages);
            EditorApplication.QueuePlayerLoopUpdate();
        }

        void SaveCurrentPng()
        {
            ResponsiveDeviceSpec device = SelectedDevice;
            if (_session == null || device == null)
                return;
            string path = Path.Combine(_captureFolder, FileName(device, _screenIndex, _landscape, _hostScreen) + ".png");
            _session.SavePng(path);
            Debug.Log("Responsive Manager: saved " + Path.GetFullPath(path));
        }

        string FileName(ResponsiveDeviceSpec device, int screen, bool landscape, int hostScreen)
        {
            string name = device.id + "-" + device.screens[screen].name.ToLowerInvariant() + (landscape ? "-land" : "-port");
            if (_host != null && hostScreen >= 0 && hostScreen < _host.Screens.Count)
                name += "-" + _host.Screens[hostScreen].Id.ToLowerInvariant();
            foreach (char bad in Path.GetInvalidFileNameChars())
                name = name.Replace(bad, '_');
            return name;
        }

        void DrawIssues()
        {
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Layout check" + (_session != null ? " (" + _issues.Count + ")" : ""), EditorStyles.boldLabel);
                using (new EditorGUI.DisabledScope(_session == null))
                {
                    if (GUILayout.Button("Check", GUILayout.Width(60f)))
                        RequestAudit(0);
                }
            }
            _minTarget = EditorGUILayout.Slider("Min touch target", _minTarget, 24f, 64f);
            if (_session == null)
                return;
            if (_issues.Count == 0)
            {
                EditorGUILayout.LabelField("No truncated text, cut text, off-screen text or small targets.", EditorStyles.wordWrappedMiniLabel);
                return;
            }
            _issueScroll = EditorGUILayout.BeginScrollView(_issueScroll, GUILayout.MaxHeight(160f));
            foreach (ResponsiveLayoutIssue issue in _issues)
                EditorGUILayout.LabelField(issue.ToString(), EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndScrollView();
        }

        void DrawRules()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Size and scale per form factor", EditorStyles.boldLabel);
            if (_rules == null)
            {
                EditorGUILayout.HelpBox("No Responsive Rules asset. Create one at " + ResponsiveRulesStore.DefaultAssetPath +
                                        " and hand its Rules to the game's PanelScaleDriver.", MessageType.Info);
                if (GUILayout.Button("Create rules asset"))
                    _rules = ResponsiveRulesStore.Create();
                return;
            }
            EditorGUILayout.ObjectField(_rules, typeof(ResponsiveRulesAsset), false);
            EditorGUILayout.LabelField("Scale multiplies the panel scale (1 = as the policy decides). Column limits the width of elements with the binder's column class (0 = none). " +
                                       "Covers fall back to Phone, Fold inner to Pad.", EditorStyles.wordWrappedMiniLabel);
            ResponsiveScreenSpec selected = SelectedScreen;
            DeviceFormFactor current = selected != null ? selected.FormFactor(Profile) : DeviceFormFactor.Phone;
            foreach (DeviceFormFactor form in DeviceFormFactors.All)
            {
                int bit = 1 << (int)form;
                string title = DeviceFormFactors.DisplayName(form) + (form == current ? "  (selected device)" : "");
                bool open = EditorGUILayout.Foldout((_formFoldouts & bit) != 0, title, true);
                _formFoldouts = open ? _formFoldouts | bit : _formFoldouts & ~bit;
                if (!open)
                    continue;
                EditorGUI.indentLevel++;
                foreach (ResponsiveOrientation orientation in Orientations)
                    DrawRule(form, orientation);
                EditorGUI.indentLevel--;
            }
        }

        void DrawRule(DeviceFormFactor form, ResponsiveOrientation orientation)
        {
            bool stored = _rules.Rules.TryFind(form, orientation, out ResponsiveRule rule);
            using (new EditorGUILayout.HorizontalScope())
            {
                bool want = EditorGUILayout.ToggleLeft(orientation.ToString(), stored, GUILayout.Width(110f));
                if (want != stored)
                {
                    if (want)
                    {
                        ResponsiveRule start = _rules.Rules.Resolve(form, orientation == ResponsiveOrientation.Landscape);
                        start.form = form;
                        start.orientation = orientation;
                        ResponsiveRulesStore.Set(_rules, start);
                    }
                    else
                    {
                        ResponsiveRulesStore.Clear(_rules, form, orientation);
                    }
                    ApplyPreview();
                    return;
                }
                if (!stored)
                {
                    GUILayout.Label("inherits", EditorStyles.miniLabel);
                    return;
                }
            }
            EditorGUI.BeginChangeCheck();
            float scale = EditorGUILayout.Slider("Scale", rule.Scale, ResponsiveRule.MinScale, ResponsiveRule.MaxScale);
            float width = EditorGUILayout.FloatField("Column (units)", rule.MaxContentWidth);
            if (EditorGUI.EndChangeCheck())
            {
                rule.scale = Mathf.Round(scale * 100f) / 100f;
                rule.maxContentWidth = Mathf.Max(0f, Mathf.Round(width));
                ResponsiveRulesStore.Set(_rules, rule);
                ApplyPreview();
            }
        }

        // ---- check every device

        void DrawBatch()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Check every device", EditorStyles.boldLabel);
            _batchBothOrientations = EditorGUILayout.ToggleLeft("Portrait and landscape", _batchBothOrientations);
            using (new EditorGUI.DisabledScope(_host == null))
                _batchAllScreens = EditorGUILayout.ToggleLeft("Every game screen", _batchAllScreens);
            _batchSavePngs = EditorGUILayout.ToggleLeft("Save PNGs", _batchSavePngs);
            if (_batchSavePngs)
                _captureFolder = EditorGUILayout.TextField("Folder", _captureFolder);
            using (new EditorGUI.DisabledScope(_session == null || _batch != null))
            {
                if (GUILayout.Button("Run"))
                    StartBatch();
            }
            if (_batchRows == null)
                return;
            int failing = 0;
            foreach (BatchRow row in _batchRows)
                failing += row.Issues.Count > 0 ? 1 : 0;
            EditorGUILayout.LabelField(_batchRows.Count + " previews, " + failing + " with findings", EditorStyles.miniBoldLabel);
            foreach (BatchRow row in _batchRows)
            {
                if (row.Issues.Count == 0)
                    continue;
                if (GUILayout.Button(row.Title + ": " + row.Issues.Count, EditorStyles.miniButton))
                {
                    _landscape = row.Landscape;
                    _halfOpened = false;
                    Select(row.DeviceId, row.Screen);
                    if (row.HostScreen >= 0)
                        ShowHostScreen(row.HostScreen);
                }
            }
            if (GUILayout.Button("Copy report"))
                EditorGUIUtility.systemCopyBuffer = Report();
        }

        string Report()
        {
            var text = new StringBuilder();
            text.AppendLine("| Preview | Form factor | Units | Findings |");
            text.AppendLine("|---|---|---|---|");
            foreach (BatchRow row in _batchRows)
                text.AppendLine("| " + row.Title + " | " + DeviceFormFactors.DisplayName(row.Form) + " | " + row.Units + " | " + row.Issues.Count + " |");
            foreach (BatchRow row in _batchRows)
            {
                if (row.Issues.Count == 0)
                    continue;
                text.AppendLine().AppendLine("### " + row.Title);
                foreach (ResponsiveLayoutIssue issue in row.Issues)
                    text.AppendLine("- " + issue);
            }
            return text.ToString();
        }

        void StartBatch()
        {
            var steps = new List<BatchStep>();
            var hostScreens = new List<int>();
            if (_batchAllScreens && _host != null)
            {
                for (int i = 0; i < _host.Screens.Count; i++)
                    hostScreens.Add(i);
            }
            else
            {
                hostScreens.Add(-1);
            }
            foreach (int hostScreen in hostScreens)
            {
                foreach (ResponsiveDeviceSpec device in _catalog.devices)
                {
                    if (device == null || device.screens == null)
                        continue;
                    for (int s = 0; s < device.screens.Count; s++)
                    {
                        if (device.screens[s] == null || device.screens[s].dpi <= 0f)
                            continue;
                        string screenLabel = hostScreen >= 0 ? _host.Screens[hostScreen].Label + ": " : "";
                        steps.Add(new BatchStep(device, s, false, hostScreen, screenLabel));
                        if (_batchBothOrientations)
                            steps.Add(new BatchStep(device, s, true, hostScreen, screenLabel));
                    }
                }
            }
            _batchRows = new List<BatchRow>();
            _batch = new BatchRun(steps, _deviceId, _screenIndex, _landscape, _halfOpened) { FirstWait = _auditPending ? _auditMinSeconds : 0 };
        }

        void StopBatch()
        {
            if (_batch == null)
                return;
            BatchRun run = _batch;
            _batch = null;
            EditorUtility.ClearProgressBar();
            _deviceId = run.DeviceId;
            _screenIndex = run.Screen;
            _landscape = run.Landscape;
            _halfOpened = run.HalfOpened;
            ApplyPreview();
            Repaint();
        }

        void StepBatch()
        {
            if (_session == null)
            {
                StopBatch();
                return;
            }
            BatchRun run = _batch;
            if (run.Index >= run.Steps.Count)
            {
                StopBatch();
                return;
            }
            BatchStep step = run.Steps[run.Index];
            if (EditorUtility.DisplayCancelableProgressBar("Responsive Manager", step.Title, (float)run.Index / run.Steps.Count))
            {
                StopBatch();
                return;
            }
            EditorApplication.QueuePlayerLoopUpdate();
            if (!run.Waiting)
            {
                // the first step also waits for a screen change that was still settling when the run started
                double settle = run.Index == 0 ? Math.Max(SettleSeconds, run.FirstWait) : SettleSeconds;
                if (step.HostScreen != run.ShownHostScreen && _host != null && step.HostScreen >= 0)
                {
                    _host.Show(_host.Screens[step.HostScreen]);
                    run.ShownHostScreen = step.HostScreen;
                    settle = ScreenChangeSeconds;
                }
                _session.Show(step.Device.screens[step.Screen], step.Landscape, DevicePosture.Flat, _background);
                run.Waiting = true;
                run.MinSeconds = settle;
                return;
            }
            if (!_session.IsSettled(run.MinSeconds))
                return;
            List<ResponsiveLayoutIssue> issues = ResponsiveLayoutAudit.Run(_session.PanelRoot, _minTarget);
            ResponsiveScreenSpec screen = step.Device.screens[step.Screen];
            _batchRows.Add(new BatchRow(step, screen.FormFactor(Profile), Units(screen, step.Landscape), issues, step.Title));
            if (_batchSavePngs)
                _session.SavePng(Path.Combine(_captureFolder, FileName(step.Device, step.Screen, step.Landscape, step.HostScreen) + ".png"));
            run.Index++;
            run.Waiting = false;
        }

        sealed class BatchStep
        {
            public BatchStep(ResponsiveDeviceSpec device, int screen, bool landscape, int hostScreen, string prefix)
            {
                Device = device;
                Screen = screen;
                Landscape = landscape;
                HostScreen = hostScreen;
                Title = prefix + device.name + (device.screens.Count > 1 ? " " + device.screens[screen].name : "") + (landscape ? " landscape" : "");
            }

            public ResponsiveDeviceSpec Device { get; }
            public int Screen { get; }
            public bool Landscape { get; }
            public int HostScreen { get; }
            public string Title { get; }
        }

        sealed class BatchRun
        {
            public BatchRun(List<BatchStep> steps, string deviceId, int screen, bool landscape, bool halfOpened)
            {
                Steps = steps;
                DeviceId = deviceId;
                Screen = screen;
                Landscape = landscape;
                HalfOpened = halfOpened;
            }

            public List<BatchStep> Steps { get; }
            public string DeviceId { get; }
            public int Screen { get; }
            public bool Landscape { get; }
            public bool HalfOpened { get; }
            public int Index { get; set; }
            public bool Waiting { get; set; }
            public double MinSeconds { get; set; }
            public double FirstWait { get; set; }
            public int ShownHostScreen { get; set; } = -1;
        }

        sealed class BatchRow
        {
            public BatchRow(BatchStep step, DeviceFormFactor form, string units, List<ResponsiveLayoutIssue> issues, string title)
            {
                DeviceId = step.Device.id;
                Screen = step.Screen;
                Landscape = step.Landscape;
                HostScreen = step.HostScreen;
                Form = form;
                Units = units;
                Issues = issues;
                Title = title;
            }

            public string DeviceId { get; }
            public int Screen { get; }
            public bool Landscape { get; }
            public int HostScreen { get; }
            public DeviceFormFactor Form { get; }
            public string Units { get; }
            public List<ResponsiveLayoutIssue> Issues { get; }
            public string Title { get; }
        }
    }
}
