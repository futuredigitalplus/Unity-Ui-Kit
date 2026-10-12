namespace FutureDigitalPlus.UIKit.Foundation.Responsive
{
    /// <summary>What <see cref="ResponsiveLayoutAudit"/> found wrong with one element.</summary>
    public enum ResponsiveIssueKind
    {
        /// <summary>A single-line text is wider than its box (cut or ellipsized).</summary>
        TextTruncated = 0,

        /// <summary>A wrapping text is taller than its box: lines are cut off, or the box was squeezed narrower than the text and the text spills out of it.</summary>
        TextClipped = 1,

        /// <summary>A text reaches past the left or right edge of the panel.</summary>
        OffScreen = 2,

        /// <summary>A control is smaller than the minimum touch target on a side.</summary>
        SmallTarget = 3,
    }

    /// <summary>One finding of <see cref="ResponsiveLayoutAudit"/>.</summary>
    public readonly struct ResponsiveLayoutIssue
    {
        /// <summary>Creates a finding.</summary>
        public ResponsiveLayoutIssue(ResponsiveIssueKind kind, string element, string text, string detail)
        {
            Kind = kind;
            Element = element ?? "";
            Text = text ?? "";
            Detail = detail ?? "";
        }

        /// <summary>What is wrong.</summary>
        public ResponsiveIssueKind Kind { get; }

        /// <summary>The element: its type and name.</summary>
        public string Element { get; }

        /// <summary>The text of the element or of its label.</summary>
        public string Text { get; }

        /// <summary>Sizes: what was needed and what there was, in units.</summary>
        public string Detail { get; }

        /// <inheritdoc />
        public override string ToString()
        {
            return Kind + " " + Element + (Text.Length > 0 ? " \"" + Text + "\"" : "") + (Detail.Length > 0 ? " " + Detail : "");
        }
    }
}
