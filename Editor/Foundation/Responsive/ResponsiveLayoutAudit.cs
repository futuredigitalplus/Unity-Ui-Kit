using System.Collections.Generic;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation.Responsive
{
    /// <summary>
    /// Looks for layout faults in a laid-out panel: text that does not fit its box (<see cref="ResponsiveIssueKind.TextTruncated"/>,
    /// <see cref="ResponsiveIssueKind.TextClipped"/>), text past the panel's side edges, and controls smaller than a touch target. Only shown elements count
    /// (display, visibility and opacity of every ancestor). Runs on the panel as it is: the caller lets a frame pass after a change.
    /// </summary>
    public static class ResponsiveLayoutAudit
    {
        /// <summary>The smallest comfortable touch target side in units (44 points on iOS, 48 dp on Android rounds to about the same physical size).</summary>
        public const float DefaultMinTargetUnits = 44f;

        const float Slack = 1.5f;

        /// <summary>The faults of the shown elements below <paramref name="panelRoot"/> (the panel's visual tree).</summary>
        public static List<ResponsiveLayoutIssue> Run(VisualElement panelRoot, float minTargetUnits = DefaultMinTargetUnits)
        {
            var issues = new List<ResponsiveLayoutIssue>();
            if (panelRoot == null)
                return issues;
            float panelWidth = panelRoot.layout.width;
            panelRoot.Query<TextElement>().ForEach(text => CheckText(text, panelWidth, issues));
            panelRoot.Query<VisualElement>().ForEach(element => CheckTarget(element, minTargetUnits, issues));
            return issues;
        }

        /// <summary>The largest shown <see cref="ScrollView"/> of the panel (the screen's main list), or null.</summary>
        public static ScrollView MainScrollView(VisualElement panelRoot)
        {
            ScrollView best = null;
            float bestArea = 0f;
            if (panelRoot == null)
                return null;
            panelRoot.Query<ScrollView>().ForEach(view =>
            {
                if (!IsShown(view))
                    return;
                float area = view.worldBound.width * view.worldBound.height;
                if (area > bestArea)
                {
                    bestArea = area;
                    best = view;
                }
            });
            return best;
        }

        /// <summary>Scrolls the main list of the panel by <paramref name="pages"/> viewports (negative scrolls up). False when there is no list to scroll.</summary>
        public static bool ScrollPages(VisualElement panelRoot, int pages)
        {
            ScrollView view = MainScrollView(panelRoot);
            if (view == null)
                return false;
            float viewport = view.contentViewport.layout.height;
            float max = System.Math.Max(0f, view.contentContainer.layout.height - viewport);
            if (max <= 1f)
                return false;
            float y = System.Math.Max(0f, System.Math.Min(max, view.scrollOffset.y + pages * viewport * 0.85f));
            view.scrollOffset = new UnityEngine.Vector2(view.scrollOffset.x, y);
            return true;
        }

        /// <summary>True when <paramref name="element"/> and all its ancestors are displayed, visible and not transparent, and it has a size.</summary>
        public static bool IsShown(VisualElement element)
        {
            if (element == null || element.worldBound.width < 1f)
                return false;
            for (VisualElement current = element; current != null; current = current.hierarchy.parent)
            {
                IResolvedStyle style = current.resolvedStyle;
                if (style.display == DisplayStyle.None || style.visibility == Visibility.Hidden || style.opacity <= 0.01f)
                    return false;
            }
            return true;
        }

        static void CheckText(TextElement text, float panelWidth, List<ResponsiveLayoutIssue> issues)
        {
            if (string.IsNullOrEmpty(text.text) || !IsShown(text))
                return;
            UnityEngine.Rect box = text.contentRect;
            if (box.width < 1f)
                return;
            bool singleLine = text.resolvedStyle.whiteSpace == WhiteSpace.NoWrap;
            // a wrapped text is measured with a little more than its box: a label sized to its own text can wrap at exactly that width from rounding alone
            UnityEngine.Vector2 need = text.MeasureTextSize(text.text, singleLine ? 0f : box.width + Slack,
                singleLine ? VisualElement.MeasureMode.Undefined : VisualElement.MeasureMode.Exactly, 0f, VisualElement.MeasureMode.Undefined);
            if (singleLine && need.x > box.width + Slack)
                issues.Add(new ResponsiveLayoutIssue(ResponsiveIssueKind.TextTruncated, Describe(text), text.text, "needs " + need.x.ToString("0") + " has " + box.width.ToString("0")));
            else if (!singleLine && need.y > box.height + Slack)
                issues.Add(new ResponsiveLayoutIssue(ResponsiveIssueKind.TextClipped, Describe(text), text.text,
                    "needs " + need.y.ToString("0") + " high, has " + box.height.ToString("0") + " (box " + box.width.ToString("0") + " wide: squeezed by its row, it wraps or spills)"));
            UnityEngine.Rect bounds = text.worldBound;
            if (panelWidth > 0f && (bounds.xMax > panelWidth + 1f || bounds.xMin < -1f))
                issues.Add(new ResponsiveLayoutIssue(ResponsiveIssueKind.OffScreen, Describe(text), text.text, "x " + bounds.xMin.ToString("0") + " to " + bounds.xMax.ToString("0") + " of " + panelWidth.ToString("0")));
        }

        static void CheckTarget(VisualElement element, float minTargetUnits, List<ResponsiveLayoutIssue> issues)
        {
            if (!IsControl(element) || element.pickingMode == PickingMode.Ignore || !element.enabledInHierarchy || !IsShown(element))
                return;
            UnityEngine.Rect bounds = element.worldBound;
            float min = minTargetUnits - 0.5f;
            if (bounds.height >= min && bounds.width >= min)
                return;
            string label = element is TextElement own ? own.text : element.Q<Label>()?.text;
            issues.Add(new ResponsiveLayoutIssue(ResponsiveIssueKind.SmallTarget, Describe(element), label, bounds.width.ToString("0") + "x" + bounds.height.ToString("0")));
        }

        static bool IsControl(VisualElement element)
        {
            return element is Button || element is Toggle || element is Slider || element is SliderInt || element is DropdownField || element is TextField;
        }

        static string Describe(VisualElement element)
        {
            return string.IsNullOrEmpty(element.name) ? element.GetType().Name : element.GetType().Name + "#" + element.name;
        }
    }
}
