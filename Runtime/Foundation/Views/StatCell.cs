using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// One caption and value pair of a stats bar (score, moves, time, level). Cells share the width of their row equally; when a cell is too narrow the caption
    /// gives way and ends in an ellipsis while the value keeps its width. UXML: <c>&lt;fdp:StatCell caption="Score" value="0"/&gt;</c>.
    /// </summary>
    [UxmlElement]
    public partial class StatCell : VisualElement
    {
        /// <summary>USS class of the cell.</summary>
        public const string UssClassName = "fdp-stat-cell";

        /// <summary>Creates an empty cell.</summary>
        public StatCell()
        {
            AddToClassList(UssClassName);
            pickingMode = PickingMode.Ignore;
            CaptionLabel = new Label { pickingMode = PickingMode.Ignore };
            CaptionLabel.AddToClassList("fdp-stat-cell__caption");
            ValueLabel = new Label { pickingMode = PickingMode.Ignore };
            ValueLabel.AddToClassList("fdp-stat-cell__value");
            Add(CaptionLabel);
            Add(ValueLabel);
        }

        /// <summary>The caption label (bind localized text with <see cref="UiContext.SetText"/>).</summary>
        public Label CaptionLabel { get; }

        /// <summary>The value label.</summary>
        public Label ValueLabel { get; }

        /// <summary>The caption as literal text.</summary>
        [UxmlAttribute("caption")]
        public string Caption
        {
            get { return CaptionLabel.text; }
            set { CaptionLabel.text = value; }
        }

        /// <summary>The value as text.</summary>
        [UxmlAttribute("value")]
        public string Value
        {
            get { return ValueLabel.text; }
            set { ValueLabel.text = value; }
        }
    }
}
