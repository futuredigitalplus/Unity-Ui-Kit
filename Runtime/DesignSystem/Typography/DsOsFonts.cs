using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace FutureDigitalPlus.UIKit.DesignSystem.Typography
{
    /// <summary>
    /// Adds the operating system's fonts to the fallback list of one panel, for the scripts a game did not bundle a font for:
    /// Han, kana and Hangul (every handheld and desktop has a good CJK font, and shipping 15 MB per script is rarely worth it),
    /// and, when the text generator can shape them, Arabic, Hebrew, Thai, Devanagari, Bengali, Tamil and Khmer. Only family
    /// NAMES are listed (<see cref="DsOsFontOrder"/>); no font file is copied into a game, and nothing happens for a family
    /// that is not installed (WebGL and most consoles have no system fonts at all).
    /// <para>
    /// The fonts are dynamic OS font assets: a glyph is rasterised when it is first used, so warm the first screen with
    /// <see cref="DsFonts.Warm"/> (or pass the text to <see cref="DsFonts.ApplyLocale"/>).
    /// </para>
    /// <para>
    /// What this class never does: it never <c>Clear()</c>s a fallback list (it removes exactly the fonts it added itself),
    /// never writes into a shared <see cref="PanelTextSettings"/> asset (the panel gets a runtime copy, see
    /// <see cref="DsFonts.PrepareRuntimeTextSettings"/>), and always appends after what the game already has, so a bundled font
    /// wins over a system font. Han letterforms differ per language, so the locale's own group is tried first
    /// (<see cref="DsOsFontOrder.HanCandidates"/>). A shaping script's system font is used only when
    /// <see cref="DsFonts.CanShape"/> or <see cref="DsFonts.TryEnableShaping"/> accepts it; otherwise it is skipped
    /// (<see cref="Rejected"/>), because a font that cannot shape draws Arabic as unjoined letters, which is worse than
    /// asking the next font. Main thread only.
    /// <code>
    /// var panel = DsOsFonts.UseRuntimePanelSettings(document);     // a copy, so the project asset is never changed
    /// var osFonts = new DsOsFonts();
    /// osFonts.Install(panel, "ja");
    /// // later, when the language changes: osFonts.Reorder("zh-Hant");  and finally osFonts.Dispose();
    /// </code>
    /// </para>
    /// </summary>
    public sealed class DsOsFonts : IDisposable
    {
        private readonly Func<IEnumerable<string>> _systemFontNames;
        private readonly Dictionary<string, FontAsset> _byFamily = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<FontAsset> _installed = new();
        private readonly List<string> _rejected = new();
        private PanelSettings _panel;
        private PanelTextSettings _settings;
        private Dictionary<string, List<string>> _styles;
        private string[] _families;

        /// <summary>Creates an instance that reads the installed fonts from the operating system.</summary>
        public DsOsFonts() : this(null)
        {
        }

        /// <summary>Creates an instance with its own list of installed fonts (for tests or a platform Unity cannot list).</summary>
        /// <param name="systemFontNames">Names in Unity's <c>Family - Style</c> form; null reads them from the operating system.</param>
        public DsOsFonts(Func<IEnumerable<string>> systemFontNames)
        {
            _systemFontNames = systemFontNames;
        }

        /// <summary>
        /// Creates the font asset of one installed family and style. The default is
        /// <c>FontAsset.CreateFontAsset(family, style)</c>, a dynamic font asset that reads the system font. Replace it to create
        /// fonts another way, or in a test that has no particular system fonts. Set it before <see cref="Install"/>.
        /// </summary>
        public Func<string, string, FontAsset> FontFactory { get; set; } = (family, style) => FontAsset.CreateFontAsset(family, style);

        /// <summary>The panel the fonts are installed in, or null before <see cref="Install"/>.</summary>
        public PanelSettings Panel => _panel;

        /// <summary>The locale the fonts are currently ordered for.</summary>
        public string LocaleCode { get; private set; }

        /// <summary>The fonts this instance added to the panel, in fallback order.</summary>
        public IReadOnlyList<FontAsset> Fonts => _installed;

        /// <summary>
        /// Families that were installed on the system but not used: they failed to load, or the text generator cannot shape them
        /// (see the type summary). For diagnostics.
        /// </summary>
        public IReadOnlyList<string> Rejected => _rejected;

        /// <summary>
        /// Gives <paramref name="document"/> its own copy of its <see cref="PanelSettings"/> (made once; a later call returns
        /// the same copy), because the asset is shared project data and a change would be written into it in the Editor.
        /// Play Mode only: the copy is assigned to the component, never saved.
        /// </summary>
        /// <param name="document">The document whose panel settings to copy.</param>
        /// <returns>The panel settings the document uses now, or null when it has none.</returns>
        public static PanelSettings UseRuntimePanelSettings(UIDocument document)
        {
            if (!document || !document.panelSettings) return null;

            var source = document.panelSettings;
            if (source.name != null && source.name.EndsWith(DsFonts.RuntimeCopySuffix, StringComparison.Ordinal)) return source;

            var copy = Object.Instantiate(source);
            copy.name = source.name + DsFonts.RuntimeCopySuffix;
            copy.hideFlags = HideFlags.HideAndDontSave;
            document.panelSettings = copy;
            return copy;
        }

        /// <summary>
        /// Installs the system fonts for <paramref name="localeCode"/> into <paramref name="panel"/>. Call it once per panel; call
        /// <see cref="Reorder"/> when the language changes. Installing into another panel first takes the fonts out of the first.
        /// </summary>
        /// <param name="panel">The panel settings; use a runtime copy of a shared asset (<see cref="UseRuntimePanelSettings"/>).</param>
        /// <param name="localeCode">The active language, for example <c>ja</c>, <c>zh-Hant</c>, <c>ar</c>.</param>
        /// <returns>How many system fonts were added; 0 when none of the candidates is installed.</returns>
        public int Install(PanelSettings panel, string localeCode)
        {
            if (!panel) return 0;

            if (_panel != null && _panel != panel) RemoveInstalled();

            _panel = panel;
            _settings = DsFonts.PrepareRuntimeTextSettings(panel);
            ReadSystemFonts();
            return Apply(localeCode);
        }

        /// <summary>Re-orders the system fonts for another language (the Han typefaces differ, and the shaping script changes).</summary>
        /// <param name="localeCode">The new active language.</param>
        /// <returns>How many system fonts are installed now; 0 before <see cref="Install"/>.</returns>
        public int Reorder(string localeCode) => _panel == null ? 0 : Apply(localeCode);

        /// <summary>Takes the fonts this instance added out of the panel and destroys them. The instance can be installed again.</summary>
        public void Dispose()
        {
            RemoveInstalled();

            foreach (var font in _byFamily.Values) DestroyFont(font);
            _byFamily.Clear();
            _rejected.Clear();
            _panel = null;
            _settings = null;
        }

        private int Apply(string localeCode)
        {
            LocaleCode = localeCode;
            RemoveInstalled();

            var wanted = new List<FontAsset>();

            foreach (var family in DsOsFontOrder.HanCandidates(localeCode, _families))
            {
                var font = Get(family);
                if (font) wanted.Add(font);
            }

            var script = DsOsFontOrder.ShapingScriptOf(localeCode);
            if (script != null) AddShapingFont(script, wanted);

            var list = _settings.fallbackFontAssets;
            foreach (var font in wanted)
                if (!list.Contains(font)) list.Add(font);

            _installed.AddRange(wanted);
            DsFonts.RegisterPanelFallbacks(_panel, wanted);
            return wanted.Count;
        }

        // The first installed family that the advanced generator accepts. Families that fail are remembered so the next
        // locale change does not try them again.
        private void AddShapingFont(string script, List<FontAsset> wanted)
        {
            foreach (var family in DsOsFontOrder.ShapingCandidates(script, _families))
            {
                if (_rejected.Contains(family)) continue;

                var font = Get(family);
                if (!font) continue;

                if (DsFonts.CanShape(font) || DsFonts.TryEnableShaping(font))
                {
                    if (!wanted.Contains(font)) wanted.Add(font);
                    return;
                }

                _rejected.Add(family);
            }
        }

        private void RemoveInstalled()
        {
            if (_installed.Count == 0) return;

            if (_settings != null && _settings.fallbackFontAssets != null)
                foreach (var font in _installed)
                    _settings.fallbackFontAssets.Remove(font);

            if (_panel != null) DsFonts.UnregisterPanelFallbacks(_panel, _installed);
            _installed.Clear();
        }

        private FontAsset Get(string family)
        {
            if (_byFamily.TryGetValue(family, out var cached)) return cached;
            if (!_styles.TryGetValue(family, out var styles)) return null;

            FontAsset font = null;
            try
            {
                font = FontFactory(family, DsOsFontOrder.PickStyle(styles));
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[DsOsFonts] The system font '" + family + "' could not be loaded: " + ex.Message);
                _rejected.Add(family);
            }

            _byFamily[family] = font;
            return font;
        }

        private void ReadSystemFonts()
        {
            _styles = DsOsFontOrder.GroupByFamily(SystemFontNames());
            _families = new string[_styles.Count];
            _styles.Keys.CopyTo(_families, 0);
        }

        private IEnumerable<string> SystemFontNames()
        {
            try
            {
                return _systemFontNames != null ? _systemFontNames() : FontEngine.GetSystemFontNames() ?? Array.Empty<string>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[DsOsFonts] The list of system fonts is not available: " + ex.Message);
                return Array.Empty<string>();
            }
        }

        // A dynamic font asset made at runtime owns its atlas textures and material; Unity does not free them with the asset.
        private static void DestroyFont(FontAsset font)
        {
            if (!font) return;

            var textures = font.atlasTextures;
            if (textures != null)
                foreach (var texture in textures)
                    if (texture) Destroy(texture);

            if (font.material) Destroy(font.material);
            Destroy(font);
        }

        private static void Destroy(Object obj)
        {
            if (Application.isPlaying) Object.Destroy(obj);
            else Object.DestroyImmediate(obj);
        }
    }
}
