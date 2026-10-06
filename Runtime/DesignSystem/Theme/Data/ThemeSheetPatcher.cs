using System;
using System.Collections.Generic;
using FutureDigitalPlus.UIKit.DesignSystem.Theme.Applier;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace FutureDigitalPlus.UIKit.DesignSystem.Theme.Data
{
    /// <summary>
    /// Builds a stylesheet for a theme that was never baked, by cloning a baked stylesheet and overwriting the values of the
    /// tokens it contains. Unity has no public API that creates a stylesheet from text at runtime, but a
    /// <see cref="StyleSheet"/> serialises through the public <see cref="JsonUtility"/>, so the colour and dimension tables
    /// of a clone can be rewritten without any reflection or editor code.
    ///
    /// Only the <c>colors</c> and <c>dimensions</c> tables are written. The selector tables are rebuilt by Unity when a
    /// sheet is enabled, so a patched sheet keeps the scope selector of its template (which is why a runtime theme cannot
    /// change its scope). Slots are found by token NAME, never by position. Everything here relies on the serialised field
    /// names of <see cref="StyleSheet"/>; <see cref="TryCreate"/> checks them and fails softly (one warning, no sheet)
    /// when a Unity version lays the sheet out differently, so a runtime theme then simply does not apply.
    /// Never mutate a sheet that is attached to a panel: Unity caches resolved styles by sheet, so build a new clone for
    /// every change (<see cref="RuntimeThemeSheets"/> does).
    /// </summary>
    public sealed class ThemeSheetPatcher
    {
        // Mirror of the parts of the serialised StyleSheet that are read or written. The names are Unity's own.
#pragma warning disable CS0649 // filled by JsonUtility
        [Serializable] private class V4 { public float r, g, b, a; }
        [Serializable] private class Dim { public float value; public int unit; }
        [Serializable] private class Val { public int m_ValueType; public int valueIndex; }
        [Serializable] private class Prop { public string m_CustomName; public List<Val> m_Values; }
        [Serializable] private class Rule { public List<Prop> m_Properties; }
        [Serializable] private class Mirror { public List<Rule> m_Rules; public List<V4> colors; public List<float> floats; public List<Dim> dimensions; }
        [Serializable] private class Patch { public List<V4> colors; public List<float> floats; public List<Dim> dimensions; }
#pragma warning restore CS0649

        // StyleValueType.Dimension and StyleValueType.Color in the serialised form.
        private const int ValueTypeDimension = 3;
        private const int ValueTypeColor = 4;

        private static bool s_warned;

        private readonly StyleSheet _template;
        private readonly Mirror _mirror;
        private readonly Dictionary<string, (bool color, int slot)> _slots;

        private ThemeSheetPatcher(StyleSheet template, Mirror mirror, Dictionary<string, (bool color, int slot)> slots)
        {
            _template = template;
            _mirror = mirror;
            _slots = slots;
        }

        /// <summary>The token names the template sheet carries (and so the only ones a runtime theme can change).</summary>
        public IReadOnlyCollection<string> TokenNames => _slots.Keys;

        /// <summary>
        /// Reads <paramref name="template"/> and checks that its serialised layout is the one this class understands.
        /// </summary>
        /// <param name="template">A baked theme stylesheet.</param>
        /// <param name="patcher">The patcher, or null on failure.</param>
        /// <returns>False (after one warning per session) when the sheet is missing or laid out differently.</returns>
        public static bool TryCreate(StyleSheet template, out ThemeSheetPatcher patcher)
        {
            patcher = null;
            if (!template) return false;

            Mirror mirror;
            try
            {
                mirror = JsonUtility.FromJson<Mirror>(JsonUtility.ToJson(template));
            }
            catch (Exception ex)
            {
                return Fail("the stylesheet could not be serialised (" + ex.Message + ")");
            }

            if (mirror?.m_Rules == null || mirror.colors == null || mirror.dimensions == null)
                return Fail("the stylesheet has no m_Rules, colors or dimensions tables");

            var slots = new Dictionary<string, (bool color, int slot)>(StringComparer.Ordinal);
            var usedColors = new HashSet<int>();
            var usedDimensions = new HashSet<int>();

            foreach (var rule in mirror.m_Rules)
            {
                if (rule?.m_Properties == null) continue;

                foreach (var prop in rule.m_Properties)
                {
                    if (prop == null || string.IsNullOrEmpty(prop.m_CustomName)) continue;
                    if (prop.m_Values == null || prop.m_Values.Count != 1)
                        return Fail("token " + prop.m_CustomName + " does not have exactly one value");

                    var value = prop.m_Values[0];
                    bool isColor;
                    if (value.m_ValueType == ValueTypeColor) isColor = true;
                    else if (value.m_ValueType == ValueTypeDimension) isColor = false;
                    else continue;   // a keyword or a reference: not ours to patch

                    var table = isColor ? mirror.colors.Count : mirror.dimensions.Count;
                    if (value.valueIndex < 0 || value.valueIndex >= table)
                        return Fail("token " + prop.m_CustomName + " points outside its table");

                    // Two tokens sharing one slot (a deduplicating importer) would change together.
                    if (!(isColor ? usedColors : usedDimensions).Add(value.valueIndex))
                        return Fail("tokens share a value slot (" + prop.m_CustomName + ")");

                    slots[prop.m_CustomName] = (isColor, value.valueIndex);
                }
            }

            if (slots.Count == 0) return Fail("the stylesheet has no custom properties");

            patcher = new ThemeSheetPatcher(template, mirror, slots);
            return true;
        }

        /// <summary>
        /// Clones the template and writes the values of <paramref name="theme"/> into the clone. Tokens the theme does not
        /// define, or defines with another kind, keep the template's value. The caller owns the clone and must destroy it.
        /// </summary>
        /// <param name="theme">The theme whose values are written.</param>
        /// <param name="clone">The new stylesheet, or null on failure.</param>
        /// <returns>True when a stylesheet was built.</returns>
        public bool TryBuild(ThemeData theme, out StyleSheet clone)
        {
            clone = null;
            if (!theme) return false;

            var colors = new List<V4>(_mirror.colors.Count);
            foreach (var c in _mirror.colors) colors.Add(new V4 { r = c.r, g = c.g, b = c.b, a = c.a });

            var dimensions = new List<Dim>(_mirror.dimensions.Count);
            foreach (var d in _mirror.dimensions) dimensions.Add(new Dim { value = d.value, unit = d.unit });

            foreach (var pair in _slots)
            {
                if (!theme.TryGetToken(pair.Key, out var kind, out var color, out var number)) continue;

                if (pair.Value.color && kind == ThemeTokenKind.Color)
                    colors[pair.Value.slot] = new V4 { r = color.r, g = color.g, b = color.b, a = color.a };
                else if (!pair.Value.color && kind != ThemeTokenKind.Color)
                    dimensions[pair.Value.slot].value = number;   // the unit stays the template's
            }

            StyleSheet sheet = null;
            try
            {
                sheet = Object.Instantiate(_template);
                var patch = new Patch { colors = colors, floats = _mirror.floats, dimensions = dimensions };
                JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(patch), sheet);
                sheet.name = theme.name + " (runtime sheet)";
                sheet.hideFlags = HideFlags.HideAndDontSave;
            }
            catch (Exception ex)
            {
                if (sheet) RuntimeThemeSheets.DestroyObject(sheet);
                Fail("building the patched stylesheet threw " + ex.GetType().Name + ": " + ex.Message);
                return false;
            }

            clone = sheet;
            return true;
        }

        private static bool Fail(string reason)
        {
            if (s_warned) return false;

            s_warned = true;
            Debug.LogWarning("[ThemeSheetPatcher] Runtime themes are not available on Unity " + Application.unityVersion +
                             ": " + reason + ". ThemeRuntime.Apply returns false for themes without a baked stylesheet. " +
                             "Bake the theme in the Editor, or report this Unity version.");
            return false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => s_warned = false;
    }
}
