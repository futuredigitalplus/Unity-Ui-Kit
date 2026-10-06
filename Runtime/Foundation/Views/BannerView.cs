using System;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// A persistent inline message (an offline notice, a "new version" hint) with a colour per <see cref="ToastKind"/>, an optional action button and an
    /// optional dismiss button. It uses the design system's toast skin (<c>ds-toast--success</c>, <c>info</c>, <c>warning</c>, <c>danger</c>) but stays until
    /// it is dismissed or hidden, unlike a toast. UXML: <c>&lt;fdp:BannerView message="You are offline" kind="Warning"/&gt;</c>.
    /// </summary>
    [UxmlElement]
    public partial class BannerView : VisualElement
    {
        /// <summary>USS class of the banner.</summary>
        public const string UssClassName = "fdp-banner";

        ToastKind _kind = ToastKind.Info;
        bool _dismissible;

        /// <summary>Creates an info banner without buttons.</summary>
        public BannerView()
        {
            AddToClassList("ds-toast");
            AddToClassList(UssClassName);
            AddToClassList(ToastService.KindClass(_kind));
            MessageLabel = new Label { pickingMode = PickingMode.Ignore };
            MessageLabel.AddToClassList("ds-toast__message");
            ActionButton = new Button();
            ActionButton.AddToClassList("ds-btn");
            ActionButton.AddToClassList("ds-btn--sm");
            ActionButton.AddToClassList("fdp-banner__action");
            ActionButton.style.display = DisplayStyle.None;
            ActionButton.clicked += OnActionClicked;
            DismissButton = new Button();
            DismissButton.AddToClassList("ds-toast__close");
            VisualElement closeIcon = new VisualElement { pickingMode = PickingMode.Ignore };
            closeIcon.AddToClassList("ds-icon");
            closeIcon.AddToClassList("ds-icon--close");
            DismissButton.Add(closeIcon);
            DismissButton.style.display = DisplayStyle.None;
            DismissButton.clicked += OnDismissClicked;
            Add(MessageLabel);
            Add(ActionButton);
            Add(DismissButton);
        }

        /// <summary>Raised when the action button was pressed.</summary>
        public event Action ActionClicked;

        /// <summary>Raised when the dismiss button was pressed. The banner hides itself; call <see cref="VisualElement.RemoveFromHierarchy"/> in the handler to drop it.</summary>
        public event Action Dismissed;

        /// <summary>The message label.</summary>
        public Label MessageLabel { get; }

        /// <summary>The optional action button (hidden while it has no text).</summary>
        public Button ActionButton { get; }

        /// <summary>The optional dismiss button (shown while <see cref="Dismissible"/> is true).</summary>
        public Button DismissButton { get; }

        /// <summary>The message as literal text.</summary>
        [UxmlAttribute("message")]
        public string Message
        {
            get { return MessageLabel.text; }
            set { MessageLabel.text = value; }
        }

        /// <summary>The action button's caption as literal text; empty hides the button.</summary>
        [UxmlAttribute("action-text")]
        public string ActionText
        {
            get { return ActionButton.text; }
            set
            {
                ActionButton.text = value;
                ActionButton.style.display = string.IsNullOrEmpty(value) ? DisplayStyle.None : DisplayStyle.Flex;
            }
        }

        /// <summary>The colour family of the banner.</summary>
        [UxmlAttribute("kind")]
        public ToastKind Kind
        {
            get { return _kind; }
            set
            {
                RemoveFromClassList(ToastService.KindClass(_kind));
                _kind = value;
                AddToClassList(ToastService.KindClass(_kind));
            }
        }

        /// <summary>Whether a close button is shown.</summary>
        [UxmlAttribute("dismissible")]
        public bool Dismissible
        {
            get { return _dismissible; }
            set
            {
                _dismissible = value;
                DismissButton.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        void OnActionClicked()
        {
            Action handler = ActionClicked;
            if (handler != null)
                handler();
        }

        void OnDismissClicked()
        {
            style.display = DisplayStyle.None;
            Action handler = Dismissed;
            if (handler != null)
                handler();
        }
    }
}
