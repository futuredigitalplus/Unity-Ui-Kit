using System;

namespace FutureDigitalPlus.UIKit.DesignSystem.Theme.Palette
{
    /// <summary>
    /// A colour as four floats in 0..1 (sRGB, straight alpha). Exists so the colour maths below has no
    /// dependency on UnityEngine and can be tested with plain <c>dotnet test</c>.
    /// </summary>
    public readonly struct Rgba : IEquatable<Rgba>
    {
        /// <summary>Red, 0..1.</summary>
        public readonly float R;
        /// <summary>Green, 0..1.</summary>
        public readonly float G;
        /// <summary>Blue, 0..1.</summary>
        public readonly float B;
        /// <summary>Alpha, 0..1 (1 is opaque).</summary>
        public readonly float A;

        /// <summary>Creates a colour; components are clamped to 0..1.</summary>
        public Rgba(float r, float g, float b, float a = 1f)
        {
            R = Clamp01(r);
            G = Clamp01(g);
            B = Clamp01(b);
            A = Clamp01(a);
        }

        /// <summary>Parses <c>#RRGGBB</c> or <c>#RRGGBBAA</c> (the leading # is optional). Throws <see cref="FormatException"/> otherwise.</summary>
        public static Rgba FromHex(string hex)
        {
            if (hex == null) throw new FormatException("hex is null");
            var s = hex.Trim();
            if (s.StartsWith("#", StringComparison.Ordinal)) s = s.Substring(1);
            if (s.Length != 6 && s.Length != 8) throw new FormatException("'" + hex + "' is not #RRGGBB or #RRGGBBAA");

            float Part(int i) => Convert.ToInt32(s.Substring(i * 2, 2), 16) / 255f;
            return new Rgba(Part(0), Part(1), Part(2), s.Length == 8 ? Part(3) : 1f);
        }

        /// <summary>The same colour with another alpha.</summary>
        public Rgba WithAlpha(float alpha) => new Rgba(R, G, B, alpha);

        /// <inheritdoc />
        public bool Equals(Rgba other) => R == other.R && G == other.G && B == other.B && A == other.A;

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is Rgba other && Equals(other);

        /// <inheritdoc />
        public override int GetHashCode() => HashCode.Combine(R, G, B, A);

        /// <inheritdoc />
        public override string ToString() => string.Format(System.Globalization.CultureInfo.InvariantCulture,
            "rgba({0:0.###}, {1:0.###}, {2:0.###}, {3:0.###})", R, G, B, A);

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }

    /// <summary>
    /// WCAG 2.x contrast maths plus the small colour operations the theme tools need (mix, ensure a ratio,
    /// pick black or white). Pure C#; no UnityEngine.
    /// </summary>
    public static class ColorMath
    {
        /// <summary>WCAG AA minimum for normal-size text.</summary>
        public const float TextAa = 4.5f;

        /// <summary>WCAG AAA minimum for normal-size text (what the high-contrast themes use).</summary>
        public const float TextAaa = 7f;

        /// <summary>WCAG 2.1 (1.4.11) minimum for the boundary of a UI component and for a focus indicator.</summary>
        public const float NonTextAa = 3f;

        /// <summary>Non-text minimum used by the high-contrast themes.</summary>
        public const float NonTextHighContrast = 4.5f;

        /// <summary>Relative luminance of an opaque sRGB colour (WCAG definition, 0 black to 1 white).</summary>
        public static float Luminance(Rgba c) =>
            0.2126f * Linear(c.R) + 0.7152f * Linear(c.G) + 0.0722f * Linear(c.B);

        /// <summary>Contrast ratio between two opaque colours, 1 to 21, symmetric.</summary>
        public static float Ratio(Rgba a, Rgba b)
        {
            var la = Luminance(a);
            var lb = Luminance(b);
            if (la < lb) { var t = la; la = lb; lb = t; }
            return (la + 0.05f) / (lb + 0.05f);
        }

        /// <summary>
        /// Contrast of <paramref name="fg"/> against <paramref name="bg"/> after compositing a translucent
        /// foreground over the background (what the user actually sees). The background is taken as opaque.
        /// </summary>
        public static float Ratio(Rgba fg, Rgba bg, bool compositeForeground) =>
            Ratio(compositeForeground ? Over(fg, bg) : fg, bg);

        /// <summary>Alpha-composites <paramref name="fg"/> over an opaque <paramref name="bg"/>; the result is opaque.</summary>
        public static Rgba Over(Rgba fg, Rgba bg) =>
            new Rgba(fg.R * fg.A + bg.R * (1f - fg.A),
                     fg.G * fg.A + bg.G * (1f - fg.A),
                     fg.B * fg.A + bg.B * (1f - fg.A), 1f);

        /// <summary>Linear interpolation in sRGB space, <paramref name="t"/> 0 gives <paramref name="a"/>, 1 gives <paramref name="b"/>.</summary>
        public static Rgba Mix(Rgba a, Rgba b, float t)
        {
            t = t < 0f ? 0f : (t > 1f ? 1f : t);
            return new Rgba(a.R + (b.R - a.R) * t, a.G + (b.G - a.G) * t, a.B + (b.B - a.B) * t, a.A + (b.A - a.A) * t);
        }

        /// <summary>Black or white, whichever has the higher contrast against <paramref name="fill"/>.</summary>
        public static Rgba BestInk(Rgba fill)
        {
            var black = new Rgba(0f, 0f, 0f);
            var white = new Rgba(1f, 1f, 1f);
            return Ratio(black, fill) >= Ratio(white, fill) ? black : white;
        }

        /// <summary>
        /// Moves <paramref name="fg"/> as little as possible towards black or white (whichever direction can reach the
        /// ratio) until its contrast against <paramref name="bg"/> is at least <paramref name="minRatio"/>. Returns
        /// <paramref name="fg"/> unchanged when it already passes; returns pure black or white when nothing passes.
        /// </summary>
        public static Rgba EnsureRatio(Rgba fg, Rgba bg, float minRatio)
        {
            if (Ratio(fg, bg) >= minRatio) return fg;

            var black = new Rgba(0f, 0f, 0f, fg.A);
            var white = new Rgba(1f, 1f, 1f, fg.A);
            var towards = Ratio(black, bg) >= Ratio(white, bg) ? black : white;

            if (Ratio(towards, bg) < minRatio) return towards;

            float lo = 0f, hi = 1f;
            for (var i = 0; i < 24; i++)
            {
                var mid = (lo + hi) * 0.5f;
                if (Ratio(Mix(fg, towards, mid), bg) >= minRatio) hi = mid;
                else lo = mid;
            }

            return Mix(fg, towards, hi);
        }

        private static float Linear(float c) =>
            c <= 0.04045f ? c / 12.92f : (float)Math.Pow((c + 0.055f) / 1.055f, 2.4f);
    }
}
