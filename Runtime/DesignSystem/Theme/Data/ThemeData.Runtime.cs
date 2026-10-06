using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Applier;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Palette;
using UnityEngine;

namespace FutureDigitalPlus.UIKit.DesignSystem.Theme.Data
{
    /// <summary>The kind of value an extra token holds.</summary>
    public enum ThemeTokenKind
    {
        /// <summary>A colour.</summary>
        Color = 0,

        /// <summary>A length in pixels.</summary>
        Px = 1,

        /// <summary>A duration in milliseconds.</summary>
        Ms = 2,
    }

    /// <summary>
    /// A token a game adds to a theme beyond the built-in ones (a health colour, a suit colour, a HUD size). It is emitted
    /// into the same USS block, so it is baked, switchable with the theme and, when the base theme was baked with it,
    /// patched at runtime like any built-in token.
    /// </summary>
    [Serializable]
    public struct ThemeExtraToken
    {
        /// <summary>The USS custom property name, starting with <c>--</c> (for example <c>--mygame-color-health</c>).</summary>
        public string name;

        /// <summary>Whether the value is a colour, a pixel length or a millisecond duration.</summary>
        public ThemeTokenKind kind;

        /// <summary>The value when <see cref="kind"/> is <see cref="ThemeTokenKind.Color"/>.</summary>
        public Color color;

        /// <summary>The value when <see cref="kind"/> is <see cref="ThemeTokenKind.Px"/> or <see cref="ThemeTokenKind.Ms"/>.</summary>
        public float number;
    }

    // FutureDigitalPlus: new file. Runtime themes, access to tokens by name, extra tokens, revision and bake hash.
    public partial class ThemeData
    {
        /// <summary>Tokens of the game beyond the built-in ones (see <see cref="ThemeExtraToken"/>).</summary>
        [Header("Extra tokens")]
        [Tooltip("Tokens of your game (names start with --). They are baked and switched with the theme. A theme made at runtime " +
                 "can only change extra tokens its base theme was baked with.")]
        public List<ThemeExtraToken> extraTokens = new();

        [HideInInspector, SerializeField]
        private string bakedHash;

        [NonSerialized]
        private ThemeData runtimeTemplate;

        // ------------------------------------------------------------------ runtime themes

        /// <summary>
        /// True for a theme made by <see cref="CreateRuntime"/>: it has no baked stylesheet of its own and
        /// <see cref="ThemeRuntime.Apply"/> builds one from <see cref="RuntimeTemplate"/>.
        /// </summary>
        public bool IsRuntime => runtimeTemplate != null;

        /// <summary>The baked theme a runtime theme patches, or null for a baked theme.</summary>
        public ThemeData RuntimeTemplate => runtimeTemplate;

        /// <summary>
        /// Makes a theme that exists only in memory: a copy of <paramref name="template"/>'s values that
        /// <see cref="ThemeRuntime.Apply"/> turns into a stylesheet by patching the template's baked one. Change its
        /// fields (or call <see cref="SetToken(string, Color)"/>) and apply again to see the result; no Editor and no bake are
        /// involved. The scope selector is the template's and cannot change. Destroy the theme when done.
        /// </summary>
        /// <param name="template">A baked theme (for example the built-in Dark or Light), or another runtime theme.</param>
        /// <returns>The new theme, or null when <paramref name="template"/> is null.</returns>
        public static ThemeData CreateRuntime(ThemeData template)
        {
            if (!template) return null;

            var baseTheme = template.IsRuntime ? template.runtimeTemplate : template;
            var theme = CreateInstance<ThemeData>();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(template), theme);

            theme.styleSheet = null;
            theme.bakedHash = null;
            theme.runtimeTemplate = baseTheme;
            theme.scopeSelector = baseTheme.scopeSelector;
            theme.hideFlags = HideFlags.HideAndDontSave;
            theme.name = baseTheme.name + " (runtime)";
            return theme;
        }

        /// <summary>
        /// A runtime theme for a brand: the template's accents, spacing and type, with the primary family, surfaces, borders,
        /// text, focus ring and selection derived from two colours (see <see cref="ThemeDerivation.FromBrand"/>).
        /// </summary>
        /// <param name="primary">The brand colour (never altered).</param>
        /// <param name="background">The page background.</param>
        /// <param name="dark">True for a dark theme; picks the built-in Dark template, otherwise Light.</param>
        /// <param name="template">A baked theme to start from; null picks the built-in Dark or Light by <paramref name="dark"/>.</param>
        /// <returns>The theme, or null when no template could be loaded.</returns>
        public static ThemeData FromBrand(Color primary, Color background, bool dark, ThemeData template = null)
        {
            template = template ? template : Resources.Load<ThemeData>(dark ? DarkResource : LightResource);
            var theme = CreateRuntime(template);
            if (!theme) return null;

            theme.ApplyColorMap(ThemeDerivation.FromBrand(ToRgba(primary), ToRgba(background), dark));
            return theme;
        }

        /// <summary>Resources path of the built-in Dark theme.</summary>
        public const string DarkResource = "FutureDigitalPlus/UIKit/DesignSystem/Themes/Dark";

        /// <summary>Resources path of the built-in Light theme.</summary>
        public const string LightResource = "FutureDigitalPlus/UIKit/DesignSystem/Themes/Light";

        private void OnDestroy()
        {
            if (runtimeTemplate != null) RuntimeThemeSheets.OnThemeDestroyed(this);
        }

        // ------------------------------------------------------------------ tokens by name

        /// <summary>The names of the built-in tokens, in the order they are emitted.</summary>
        public static IReadOnlyList<string> BuiltInTokenNames
        {
            get
            {
                var names = new string[Tokens.Length];
                for (var i = 0; i < Tokens.Length; i++) names[i] = Tokens[i].Token;
                return names;
            }
        }

        /// <summary>
        /// Reads a token by its USS name, built-in or extra.
        /// </summary>
        /// <param name="token">For example <c>--fdp-color-primary</c>.</param>
        /// <param name="kind">The kind of the token.</param>
        /// <param name="color">The value when <paramref name="kind"/> is <see cref="ThemeTokenKind.Color"/>.</param>
        /// <param name="number">The value otherwise (px or ms).</param>
        /// <returns>False when the theme has no such token.</returns>
        public bool TryGetToken(string token, out ThemeTokenKind kind, out Color color, out float number)
        {
            kind = ThemeTokenKind.Color;
            color = default;
            number = 0f;
            if (string.IsNullOrEmpty(token)) return false;

            foreach (var t in Tokens)
            {
                if (t.Token != token) continue;

                var field = Field(t.Field);
                if (field == null) return false;

                var value = field.GetValue(this);
                switch (t.Unit)
                {
                    case TokenUnit.Color when value is Color c:
                        kind = ThemeTokenKind.Color;
                        color = c;
                        return true;
                    case TokenUnit.Px when value is float p:
                        kind = ThemeTokenKind.Px;
                        number = p;
                        return true;
                    case TokenUnit.Ms when value is float m:
                        kind = ThemeTokenKind.Ms;
                        number = m;
                        return true;
                    default:
                        return false;
                }
            }

            if (extraTokens != null)
            {
                foreach (var e in extraTokens)
                {
                    if (e.name != token) continue;

                    kind = e.kind;
                    color = e.color;
                    number = e.number;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Sets a colour token by its USS name. A built-in token changes its field; any other valid name (starts with
        /// <c>--</c>) is added to or updated in <see cref="extraTokens"/>.
        /// </summary>
        /// <returns>False when the name is not a valid custom property name or names a built-in token that is not a colour.</returns>
        public bool SetToken(string token, Color value)
        {
            if (!IsValidTokenName(token)) return false;

            if (TryFindBuiltIn(token, out var binding))
            {
                if (binding.Unit != TokenUnit.Color) return false;
                Field(binding.Field)?.SetValue(this, value);
                return true;
            }

            UpsertExtra(new ThemeExtraToken { name = token, kind = ThemeTokenKind.Color, color = value });
            return true;
        }

        /// <summary>
        /// Sets a length (<see cref="ThemeTokenKind.Px"/>) or duration (<see cref="ThemeTokenKind.Ms"/>) token by its USS name.
        /// Built-in tokens ignore <paramref name="kind"/> when it matches theirs; other valid names go to <see cref="extraTokens"/>.
        /// </summary>
        /// <returns>False when the name is invalid, <paramref name="kind"/> is Color, or it names a built-in colour token.</returns>
        public bool SetToken(string token, float value, ThemeTokenKind kind = ThemeTokenKind.Px)
        {
            if (!IsValidTokenName(token) || kind == ThemeTokenKind.Color) return false;

            if (TryFindBuiltIn(token, out var binding))
            {
                if (binding.Unit == TokenUnit.Color) return false;
                Field(binding.Field)?.SetValue(this, value);
                return true;
            }

            UpsertExtra(new ThemeExtraToken { name = token, kind = kind, number = value });
            return true;
        }

        /// <summary>True when <paramref name="name"/> can be a USS custom property: starts with <c>--</c>, then letters, digits, <c>-</c> or <c>_</c>.</summary>
        public static bool IsValidTokenName(string name)
        {
            if (string.IsNullOrEmpty(name) || name.Length < 3 || name[0] != '-' || name[1] != '-') return false;

            for (var i = 2; i < name.Length; i++)
            {
                var ch = name[i];
                if (!((ch >= 'a' && ch <= 'z') || (ch >= 'A' && ch <= 'Z') || (ch >= '0' && ch <= '9') || ch == '-' || ch == '_'))
                    return false;
            }

            return true;
        }

        private static bool TryFindBuiltIn(string token, out TokenBinding binding)
        {
            foreach (var t in Tokens)
            {
                if (t.Token != token) continue;
                binding = t;
                return true;
            }

            binding = default;
            return false;
        }

        private void UpsertExtra(ThemeExtraToken token)
        {
            extraTokens ??= new List<ThemeExtraToken>();
            for (var i = 0; i < extraTokens.Count; i++)
            {
                if (extraTokens[i].name != token.name) continue;
                extraTokens[i] = token;
                return;
            }

            extraTokens.Add(token);
        }

        // ------------------------------------------------------------------ colour maps

        /// <summary>All built-in colour tokens as a pure palette (token name to colour), for the audit and the derivations.</summary>
        public Dictionary<string, Rgba> ToColorMap()
        {
            var map = new Dictionary<string, Rgba>();
            foreach (var t in Tokens)
            {
                if (t.Unit != TokenUnit.Color) continue;
                if (Field(t.Field)?.GetValue(this) is Color c) map[t.Token] = ToRgba(c);
            }

            return map;
        }

        /// <summary>Writes the colours of <paramref name="map"/> into the built-in colour fields; tokens the map lacks keep their value.</summary>
        public void ApplyColorMap(IReadOnlyDictionary<string, Rgba> map)
        {
            if (map == null) return;

            foreach (var t in Tokens)
            {
                if (t.Unit != TokenUnit.Color || !map.TryGetValue(t.Token, out var c)) continue;
                Field(t.Field)?.SetValue(this, new Color(c.R, c.G, c.B, c.A));
            }
        }

        private static Rgba ToRgba(Color c) => new Rgba(c.r, c.g, c.b, c.a);

        // ------------------------------------------------------------------ revision and bake hash

        /// <summary>
        /// A hash of everything that decides the generated stylesheet (scope, every token value, extra tokens, typeface). It
        /// changes whenever a value changes, so <see cref="ThemeRuntime"/> can tell whether a runtime theme needs a new sheet.
        /// Cheap enough to call on every apply.
        /// </summary>
        public int ComputeRevision()
        {
            unchecked
            {
                var h = 17;
                h = Mix(h, Scope);
                h = Mix(h, typefaceUss);

                foreach (var t in Tokens)
                {
                    var value = Field(t.Field)?.GetValue(this);
                    if (value is Color c)
                    {
                        h = Mix(h, c.r); h = Mix(h, c.g); h = Mix(h, c.b); h = Mix(h, c.a);
                    }
                    else if (value is float f)
                    {
                        h = Mix(h, f);
                    }
                }

                if (extraTokens != null)
                {
                    foreach (var e in extraTokens)
                    {
                        h = Mix(h, e.name);
                        h = Mix(h, (int)e.kind);
                        h = Mix(h, e.color.r); h = Mix(h, e.color.g); h = Mix(h, e.color.b); h = Mix(h, e.color.a);
                        h = Mix(h, e.number);
                    }
                }

                return h;
            }
        }

        private static int Mix(int h, int v) => unchecked((h * 16777619) ^ v);

        private static int Mix(int h, float v) => Mix(h, BitConverter.SingleToInt32Bits(v));

        private static int Mix(int h, string v)
        {
            if (v == null) return Mix(h, 0);
            unchecked
            {
                foreach (var ch in v) h = Mix(h, (int)ch);
                return h;
            }
        }

        /// <summary>The bake hash stored when the editor last baked this theme, or empty when it was never recorded.</summary>
        public string BakedHash => bakedHash;

        /// <summary>
        /// A hash of the stylesheet text this theme generates right now (FNV-1a, 64 bit, hex). The editor stores it at
        /// bake time; a later difference means the values changed after the bake and the baked sheet is stale.
        /// </summary>
        public string ComputeBakeHash()
        {
            var uss = GenerateUssString();
            unchecked
            {
                var hash = 14695981039346656037UL;
                foreach (var b in Encoding.UTF8.GetBytes(uss))
                {
                    hash ^= b;
                    hash *= 1099511628211UL;
                }

                return hash.ToString("x16", CultureInfo.InvariantCulture);
            }
        }

        /// <summary>Stores the bake hash. Called by the editor baker; do not call it from game code.</summary>
        public void SetBakedHash(string hash) => bakedHash = hash;

        // ------------------------------------------------------------------ extra tokens in the stylesheet

        private void AppendExtraTokens(StringBuilder sb)
        {
            if (extraTokens == null) return;

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var e in extraTokens)
            {
                if (!IsValidTokenName(e.name) || TryFindBuiltIn(e.name, out _) || !seen.Add(e.name)) continue;

                string rendered;
                switch (e.kind)
                {
                    case ThemeTokenKind.Color: rendered = ColorToUss(e.color); break;
                    case ThemeTokenKind.Px:    rendered = Num(e.number) + "px"; break;
                    default:                   rendered = Num(e.number) + "ms"; break;
                }

                sb.Append("    ").Append(e.name).Append(": ").Append(rendered).AppendLine(";");
            }
        }
    }
}
