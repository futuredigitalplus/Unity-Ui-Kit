using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// UI Toolkit view of the <see cref="DialogQueue"/>: builds the dialog frame into the dialogs layer, fills title, message, content and buttons, absorbs
    /// input behind it, traps focus, and blocks game input while a dialog is open. The frame is built in code on the design system's <c>ds-backdrop</c>,
    /// <c>ds-dialog</c> (or <c>ds-modal</c> when custom content is present) and <c>ds-btn</c> classes; a UXML template named <c>DialogFrame</c> in the asset
    /// source replaces it (element names in <see cref="UiNames"/>). Button presses, back and scrim taps go back into the queue, which invokes the request's
    /// callback. Back (Escape, Android back, navigation-cancel) reaches the service through the shell's back dispatcher, not through a handler of its own, so
    /// one key press can never close a dialog and pop a screen. The common cases have presets: see <see cref="DialogPresets"/>.
    /// </summary>
    public sealed class DialogService : IDialogService, IDisposable
    {
        readonly VisualElement _layer;
        readonly IUiAssetSource _assets;
        readonly InputBlockCounter _blocks;
        readonly UiTextSource _text;
        readonly DialogQueue _queue = new DialogQueue();
        VisualElement _frame;
        FocusTrap _trap;
        int _blockToken;

        /// <summary>Creates the service. <paramref name="assets"/> and <paramref name="text"/> may be null (no template override, default text source).</summary>
        public DialogService(VisualElement dialogsLayer, IUiAssetSource assets, InputBlockCounter blocks, UiTextSource text = null)
        {
            if (dialogsLayer == null)
                throw new ArgumentNullException(nameof(dialogsLayer));
            if (blocks == null)
                throw new ArgumentNullException(nameof(blocks));
            _layer = dialogsLayer;
            _assets = assets;
            _blocks = blocks;
            _text = text ?? new UiTextSource();
            _queue.Showing += OnShowing;
            _queue.Closed += OnClosed;
            _queue.ModalStateChanged += OnModalStateChanged;
        }

        /// <inheritdoc />
        public bool IsModalOpen
        {
            get { return _queue.IsModalOpen; }
        }

        /// <inheritdoc />
        public event Action<bool> ModalStateChanged;

        /// <inheritdoc />
        public DialogHandle Show(DialogRequest request)
        {
            return _queue.Enqueue(request);
        }

        /// <summary>Shows a kept dialog template. <paramref name="content"/> is the custom body (a <c>VisualElement</c>) for templates that have one.</summary>
        public DialogHandle Show(DialogTemplate template, Action<DialogOutcome> callback = null, object content = null)
        {
            if (template == null)
                throw new ArgumentNullException(nameof(template));
            return _queue.Enqueue(template.CreateRequest(callback, content));
        }

        /// <inheritdoc />
        public bool Cancel(DialogHandle handle)
        {
            return _queue.Cancel(handle);
        }

        /// <inheritdoc />
        public void CloseAll()
        {
            _queue.CloseAll();
        }

        /// <inheritdoc />
        public bool HandleBack()
        {
            return _queue.RequestBack();
        }

        /// <inheritdoc />
        public void OnAppResumed()
        {
            _queue.DismissOnResume();
        }

        /// <summary>Closes every dialog (each callback once, as cleared) and drops every request that arrives afterwards, so a callback cannot open a dialog in a UI that is going away. The shell calls it on dispose.</summary>
        public void Dispose()
        {
            _queue.Shutdown();
        }

        void OnShowing(DialogHandle handle)
        {
            DialogRequest request = handle.Request;
            bool hasContent = request.Content is VisualElement;
            VisualElement container = null;
            VisualTreeAsset tree = _assets == null ? null : _assets.LoadUxml(UiNames.TemplateDialogFrame);
            if (tree != null)
            {
                TemplateContainer instance = tree.Instantiate();
                FoundationLayout.Fill(instance);
                _layer.Add(instance);
                container = instance;
            }
            VisualElement scrim = container == null ? null : container.Q<VisualElement>(UiNames.DialogScrim);
            if (scrim == null)
            {
                if (container != null)
                    container.RemoveFromHierarchy();
                container = null;
                scrim = BuildFrame(hasContent);
                _layer.Add(scrim);
            }

            _frame = container ?? scrim;
            FoundationLayout.Fill(scrim);
            FoundationLayout.AbsorbPointer(scrim);
            scrim.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.target == scrim)
                    _queue.RequestScrimTap();
            });

            Label title = scrim.Q<Label>(UiNames.DialogTitle);
            Label message = scrim.Q<Label>(UiNames.DialogMessage);
            VisualElement content = scrim.Q<VisualElement>(UiNames.DialogContent);
            VisualElement buttons = scrim.Q<VisualElement>(UiNames.DialogButtons);
            Fill(title, request.Title);
            Fill(message, request.Message);

            if (content != null)
            {
                VisualElement body = request.Content as VisualElement;
                if (body != null)
                    content.Add(body);
                content.style.display = body == null ? DisplayStyle.None : DisplayStyle.Flex;
            }

            if (buttons != null)
            {
                buttons.Clear();
                buttons.style.display = request.Buttons.Count == 0 ? DisplayStyle.None : DisplayStyle.Flex;
                for (int i = 0; i < request.Buttons.Count; i++)
                    buttons.Add(CreateButton(handle, i, request.Buttons[i]));
                KeepButtonsReadable(buttons);
            }

            if (UiMotion.Scale(1) > 0)
                scrim.schedule.Execute(() => scrim.AddToClassList(UiNames.ClassVisible)).StartingIn(16);
            else
                scrim.AddToClassList(UiNames.ClassVisible);
            _trap = new FocusTrap(scrim);
        }

        static VisualElement BuildFrame(bool hasContent)
        {
            VisualElement scrim = new VisualElement { name = UiNames.DialogScrim };
            scrim.AddToClassList("ds-backdrop");
            scrim.AddToClassList(UiNames.ClassScrim);
            SafeAreaPanel safe = new SafeAreaPanel { name = UiNames.DialogSafe };
            safe.AddToClassList(UiNames.ClassDialogSafe);
            VisualElement panel = new VisualElement { name = UiNames.DialogPanel };
            panel.AddToClassList(hasContent ? "ds-modal" : "ds-dialog");
            panel.AddToClassList(UiNames.ClassDialog);
            if (hasContent)
                panel.AddToClassList(UiNames.ClassDialogWithContent);
            UnityEngine.UIElements.ScrollView body = new UnityEngine.UIElements.ScrollView(ScrollViewMode.Vertical) { name = UiNames.DialogBody, focusable = false };
            body.AddToClassList("fdp-dialog__body");
            Label title = new Label { name = UiNames.DialogTitle };
            title.AddToClassList(hasContent ? "ds-modal__title" : "ds-dialog__title");
            title.AddToClassList("fdp-dialog__title");
            Label message = new Label { name = UiNames.DialogMessage };
            message.AddToClassList(hasContent ? "ds-modal__body-text" : "ds-dialog__message");
            message.AddToClassList("fdp-dialog__message");
            VisualElement content = new VisualElement { name = UiNames.DialogContent };
            content.AddToClassList("fdp-dialog__content");
            VisualElement buttons = new VisualElement { name = UiNames.DialogButtons };
            buttons.AddToClassList(hasContent ? "ds-modal__actions" : "ds-dialog__actions");
            buttons.AddToClassList("fdp-dialog__buttons");
            body.Add(title);
            body.Add(message);
            body.Add(content);
            panel.Add(body);
            panel.Add(buttons);
            safe.Add(panel);
            scrim.Add(safe);
            return scrim;
        }

        /// <summary>
        /// In a button row (landscape phones, tablets) the buttons get equal widths only while every caption fits on one line in an equal share of the row;
        /// otherwise the row wraps and each button is as wide as its caption (see <c>fdp-dialog__buttons</c> in Foundation.uss). The check depends only on the
        /// row width and the caption widths, never on the current layout of the buttons, so toggling the class cannot make it flip back and forth. It runs again
        /// when the row or a button changes size, which also covers a caption that arrives later from a string table or after a language change.
        /// </summary>
        static void KeepButtonsReadable(VisualElement row)
        {
            row.RegisterCallback<GeometryChangedEvent>(evt => UpdateEqualWidth(row));
            for (int i = 0; i < row.childCount; i++)
                row[i].RegisterCallback<GeometryChangedEvent>(evt => UpdateEqualWidth(row));
        }

        static void UpdateEqualWidth(VisualElement row)
        {
            float width = row.layout.width;
            int count = row.childCount;
            if (count == 0 || float.IsNaN(width) || width <= 0f)
                return;
            if (row.resolvedStyle.flexDirection != FlexDirection.Row)
            {
                row.RemoveFromClassList(UiNames.ClassDialogButtonsEqual);
                return;
            }

            bool fits = true;
            for (int i = 0; i < count && fits; i++)
            {
                Button button = row[i] as Button;
                if (button == null)
                    continue;
                IResolvedStyle style = button.resolvedStyle;
                float share = width / count - style.marginLeft - style.marginRight;
                float chrome = style.paddingLeft + style.paddingRight + style.borderLeftWidth + style.borderRightWidth;
                Vector2 text = button.MeasureTextSize(button.text ?? string.Empty, 0f, VisualElement.MeasureMode.Undefined, 0f, VisualElement.MeasureMode.Undefined);
                fits = text.x + chrome <= share;
            }
            row.EnableInClassList(UiNames.ClassDialogButtonsEqual, fits);
        }

        void Fill(Label label, UiText text)
        {
            if (label == null)
                return;
            bool has = text != null && !text.IsEmpty;
            label.style.display = has ? DisplayStyle.Flex : DisplayStyle.None;
            if (has)
                _text.Bind(label, text);
        }

        Button CreateButton(DialogHandle handle, int index, DialogButton spec)
        {
            Button button = new Button();
            button.AddToClassList("ds-btn");
            switch (spec.Role)
            {
                case DialogButtonRole.Cancel:
                    button.AddToClassList("ds-btn--secondary");
                    break;
                case DialogButtonRole.Destructive:
                    button.AddToClassList("ds-btn--danger");
                    break;
                default:
                    button.AddToClassList("ds-btn--primary");
                    break;
            }
            _text.Bind(button, spec.Label);
            button.clicked += () => _queue.Complete(handle, index);
            return button;
        }

        void OnClosed(DialogHandle handle)
        {
            if (_trap != null)
            {
                _trap.Dispose();
                _trap = null;
            }
            if (_frame != null)
            {
                _frame.RemoveFromHierarchy();
                _frame = null;
            }
        }

        void OnModalStateChanged(bool open)
        {
            if (open)
            {
                if (_blockToken == 0)
                    _blockToken = _blocks.Acquire("dialog");
            }
            else if (_blockToken != 0)
            {
                _blocks.Release(_blockToken);
                _blockToken = 0;
            }
            Action<bool> handler = ModalStateChanged;
            if (handler != null)
                handler(open);
        }
    }
}
