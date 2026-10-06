using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// A container that pads its content by the device safe area (notch, Dynamic Island, home indicator, rounded corners, television overscan): put screen
    /// content inside it, and put full-bleed backgrounds and scrims next to it, not inside. UXML:
    /// <c>&lt;fdp:SafeAreaPanel edges="top,bottom"&gt;</c> with <c>xmlns:fdp="FutureDigitalPlus.UIKit.Foundation"</c>.
    /// <c>edges</c> is <c>all</c> (default), <c>none</c> or a list of <c>left</c>, <c>right</c>, <c>top</c>, <c>bottom</c>, <c>horizontal</c>, <c>vertical</c>.
    /// The panel itself never takes pointer input (picking mode Ignore), so a full-screen panel in the HUD layer lets touches through to the game; its
    /// children pick as usual. The padding is written inline and overrides USS padding of the same element: give the inner content its own padding.
    /// The insets come from the <see cref="SafeAreaProvider"/> of the shell above it (or <see cref="SafeAreaProvider.Default"/>), so panels that are not the
    /// screen (render textures, world space) get no padding.
    /// </summary>
    [UxmlElement]
    public partial class SafeAreaPanel : VisualElement
    {
        /// <summary>The USS class every safe-area panel carries.</summary>
        public const string UssClassName = "fdp-safe-area";

        string _edgesText = "all";
        SafeAreaEdges _edges = SafeAreaEdges.All;
        SafeAreaProvider _provider;
        SafeAreaProvider _override;

        /// <summary>Creates a safe-area panel that pads on all edges.</summary>
        public SafeAreaPanel()
        {
            AddToClassList(UssClassName);
            pickingMode = PickingMode.Ignore;
            RegisterCallback<AttachToPanelEvent>(OnAttach);
            RegisterCallback<DetachFromPanelEvent>(OnDetach);
        }

        /// <summary>The edges that take the inset, as text (see the class summary).</summary>
        [UxmlAttribute("edges")]
        public string Edges
        {
            get { return _edgesText; }
            set
            {
                _edgesText = value;
                _edges = SafeAreaEdgesParser.Parse(value);
                if (_provider != null)
                    Apply(_provider.Insets);
            }
        }

        /// <summary>The provider this panel follows. Setting it pins the panel to a provider instead of the one found above it; null goes back to the search.</summary>
        public SafeAreaProvider Provider
        {
            get { return _provider; }
            set
            {
                _override = value;
                Rebind();
            }
        }

        void OnAttach(AttachToPanelEvent evt)
        {
            Rebind();
        }

        void OnDetach(DetachFromPanelEvent evt)
        {
            Unbind();
        }

        void Rebind()
        {
            Unbind();
            if (panel == null)
                return;
            _provider = _override ?? SafeAreaProvider.For(this);
            _provider.Changed += Apply;
            Apply(_provider.Insets);
        }

        void Unbind()
        {
            if (_provider == null)
                return;
            _provider.Changed -= Apply;
            _provider = null;
        }

        void Apply(SafeAreaInsets insets)
        {
            SafeAreaInsets masked = SafeAreaMath.Mask(insets, _edges);
            style.paddingLeft = masked.Left;
            style.paddingRight = masked.Right;
            style.paddingTop = masked.Top;
            style.paddingBottom = masked.Bottom;
        }
    }
}
