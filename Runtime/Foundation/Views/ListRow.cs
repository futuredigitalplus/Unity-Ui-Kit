using System;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// A tappable list row: optional icon, title, subtitle, trailing value and a chevron. The row is focusable and activates once on a click, a tap, the
    /// navigation Submit event (Enter, gamepad A) alike, so it works with mouse, touch, keyboard and gamepad. UXML:
    /// <c>&lt;fdp:ListRow title="Language" value="English" show-chevron="true"/&gt;</c> with <c>xmlns:fdp="FutureDigitalPlus.UIKit.Foundation"</c>.
    /// Bind localized text to <see cref="TitleLabel"/>, <see cref="SubtitleLabel"/> and <see cref="ValueLabel"/> with <see cref="UiContext.SetText"/>.
    /// </summary>
    [UxmlElement]
    public partial class ListRow : VisualElement
    {
        /// <summary>USS class of the row.</summary>
        public const string UssClassName = "fdp-list-row";

        /// <summary>Added while <see cref="Selected"/> is true.</summary>
        public const string SelectedClassName = "fdp-list-row--selected";

        bool _selected;
        bool _showChevron = true;

        /// <summary>Creates an empty row.</summary>
        public ListRow()
        {
            AddToClassList(UssClassName);
            focusable = true;
            tabIndex = 0;

            Icon = new VisualElement { pickingMode = PickingMode.Ignore };
            Icon.AddToClassList("ds-icon");
            Icon.AddToClassList("fdp-list-row__icon");
            Icon.style.display = DisplayStyle.None;
            VisualElement body = new VisualElement { pickingMode = PickingMode.Ignore };
            body.AddToClassList("fdp-list-row__body");
            TitleLabel = new Label { pickingMode = PickingMode.Ignore };
            TitleLabel.AddToClassList("fdp-list-row__title");
            SubtitleLabel = new Label { pickingMode = PickingMode.Ignore };
            SubtitleLabel.AddToClassList("fdp-list-row__subtitle");
            SubtitleLabel.style.display = DisplayStyle.None;
            body.Add(TitleLabel);
            body.Add(SubtitleLabel);
            // The value sits in a box that is as wide as the value up to half the row: Yoga measures a wrapping label with the room it has and a max-width on
            // the label itself would leave it narrower than its words; a box around it is laid out after its own width is known.
            VisualElement valueBox = new VisualElement { pickingMode = PickingMode.Ignore };
            valueBox.AddToClassList("fdp-list-row__value-box");
            ValueLabel = new Label { pickingMode = PickingMode.Ignore };
            ValueLabel.AddToClassList("fdp-list-row__value");
            valueBox.Add(ValueLabel);
            Chevron = new VisualElement { pickingMode = PickingMode.Ignore };
            Chevron.AddToClassList("ds-icon");
            Chevron.AddToClassList("ds-icon--chevron-right");
            Chevron.AddToClassList("fdp-list-row__chevron");
            Add(Icon);
            Add(body);
            Add(valueBox);
            Add(Chevron);

            Clickable clickable = new Clickable(OnActivated);
            this.AddManipulator(clickable);
        }

        /// <summary>Raised once per activation (click, tap, Submit).</summary>
        public event Action Clicked;

        /// <summary>The element before the title; give it a <c>ds-icon--*</c> class. Hidden until <see cref="HasIcon"/> is set.</summary>
        public VisualElement Icon { get; }

        /// <summary>The title label.</summary>
        public Label TitleLabel { get; }

        /// <summary>The second line under the title; hidden while empty.</summary>
        public Label SubtitleLabel { get; }

        /// <summary>The trailing value label.</summary>
        public Label ValueLabel { get; }

        /// <summary>The chevron at the end.</summary>
        public VisualElement Chevron { get; }

        /// <summary>The title as literal text.</summary>
        [UxmlAttribute("title")]
        public string Title
        {
            get { return TitleLabel.text; }
            set { TitleLabel.text = value; }
        }

        /// <summary>The subtitle as literal text; an empty value hides the line.</summary>
        [UxmlAttribute("subtitle")]
        public string Subtitle
        {
            get { return SubtitleLabel.text; }
            set
            {
                SubtitleLabel.text = value;
                SubtitleLabel.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        /// <summary>The trailing value as literal text.</summary>
        [UxmlAttribute("value")]
        public string Value
        {
            get { return ValueLabel.text; }
            set { ValueLabel.text = value; }
        }

        /// <summary>Whether the chevron is shown. Default true.</summary>
        [UxmlAttribute("show-chevron")]
        public bool ShowChevron
        {
            get { return _showChevron; }
            set
            {
                _showChevron = value;
                Chevron.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        /// <summary>Whether the row is drawn as the selected one.</summary>
        [UxmlAttribute("selected")]
        public bool Selected
        {
            get { return _selected; }
            set
            {
                _selected = value;
                EnableInClassList(SelectedClassName, value);
            }
        }

        /// <summary>Shows or hides the icon slot.</summary>
        public bool HasIcon
        {
            get { return Icon.style.display != DisplayStyle.None; }
            set { Icon.style.display = value ? DisplayStyle.Flex : DisplayStyle.None; }
        }

        void OnActivated()
        {
            Action handler = Clicked;
            if (handler != null)
                handler();
        }
    }
}
