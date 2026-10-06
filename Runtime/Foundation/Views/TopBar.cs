using System;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// A screen's top bar: a start slot (holds the back button), a centred title and an end slot for action buttons. The bar pads itself by the safe area on
    /// top, left and right, so its background reaches under the notch while the content row keeps the token height. UXML:
    /// <c>&lt;fdp:TopBar title="Settings" show-back="true"/&gt;</c> with <c>xmlns:fdp="FutureDigitalPlus.UIKit.Foundation"</c>. Fill <see cref="End"/> with
    /// buttons from UXML or C#; bind a localized title with <c>ui.SetText(bar.TitleLabel, text)</c>.
    /// </summary>
    [UxmlElement]
    public partial class TopBar : SafeAreaPanel
    {
        /// <summary>USS class of the bar.</summary>
        public new const string UssClassName = "fdp-topbar";

        bool _showBack = true;

        /// <summary>Creates a bar with a back button and an empty title.</summary>
        public TopBar()
        {
            Edges = "top,left,right";
            pickingMode = PickingMode.Position;
            AddToClassList(UssClassName);

            VisualElement row = new VisualElement();
            row.AddToClassList("fdp-topbar__row");
            Start = new VisualElement { name = "topbar-start" };
            Start.AddToClassList("fdp-topbar__start");
            BackButton = new Button { name = "topbar-back" };
            BackButton.AddToClassList("ds-btn");
            BackButton.AddToClassList("ds-btn--icon");
            BackButton.AddToClassList("fdp-topbar__back");
            VisualElement icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("ds-icon");
            icon.AddToClassList("ds-icon--chevron-left");
            BackButton.Add(icon);
            BackButton.clicked += OnBackClicked;
            Start.Add(BackButton);
            TitleLabel = new Label { name = "topbar-title", pickingMode = PickingMode.Ignore };
            TitleLabel.AddToClassList("fdp-topbar__title");
            End = new VisualElement { name = "topbar-end" };
            End.AddToClassList("fdp-topbar__end");
            row.Add(Start);
            row.Add(TitleLabel);
            row.Add(End);
            Add(row);
        }

        /// <summary>Raised when the back button was pressed. The bar does not navigate by itself: call <c>ui.Router.Back()</c> from the handler.</summary>
        public event Action BackClicked;

        /// <summary>The slot at the start of the row (left in left-to-right layouts). Holds the back button; add more controls if needed.</summary>
        public VisualElement Start { get; }

        /// <summary>The slot at the end of the row for action buttons.</summary>
        public VisualElement End { get; }

        /// <summary>The title label (bind localized text with <see cref="UiContext.SetText"/>).</summary>
        public Label TitleLabel { get; }

        /// <summary>The back button inside <see cref="Start"/>.</summary>
        public Button BackButton { get; }

        /// <summary>The title as literal text.</summary>
        [UxmlAttribute("title")]
        public string Title
        {
            get { return TitleLabel.text; }
            set { TitleLabel.text = value; }
        }

        /// <summary>Whether the back button is shown. Default true.</summary>
        [UxmlAttribute("show-back")]
        public bool ShowBack
        {
            get { return _showBack; }
            set
            {
                _showBack = value;
                BackButton.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        void OnBackClicked()
        {
            Action handler = BackClicked;
            if (handler != null)
                handler();
        }
    }
}
