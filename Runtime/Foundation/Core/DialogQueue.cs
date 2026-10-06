using System;
using System.Collections.Generic;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// The modal queue behind <c>DialogService</c>: exactly one dialog is current at a time, the rest wait ordered by priority (higher first),
    /// then arrival. A dialog on screen is never pre-empted. Pure C#; the view subscribes to <see cref="Showing"/> and <see cref="Closed"/> and
    /// calls <see cref="Complete"/>, <see cref="RequestBack"/> or <see cref="RequestScrimTap"/> from its buttons.
    ///
    /// Callbacks run after the dialog has left the current slot and before the next one is shown. A callback may enqueue more dialogs; they are
    /// ordered with the waiting ones by priority, so a follow-up that must come next uses <see cref="DialogPriority.High"/>.
    /// A callback, a Showing handler or a Closed handler that throws is logged and does not stop the queue (a dialog whose Showing handler throws is closed as Programmatic).
    /// </summary>
    public sealed class DialogQueue
    {
        readonly List<DialogHandle> _waiting = new List<DialogHandle>();
        int _sequence;
        int _dispatching;
        bool _publishedModal;
        bool _shutDown;

        /// <summary>The dialog that is showing now, or null.</summary>
        public DialogHandle Current { get; private set; }

        /// <summary>True after <see cref="Shutdown"/>: every new request is dropped.</summary>
        public bool IsShutDown
        {
            get { return _shutDown; }
        }

        /// <summary>Number of dialogs waiting behind the current one.</summary>
        public int WaitingCount
        {
            get { return _waiting.Count; }
        }

        /// <summary>True while a dialog is showing. A game pauses its own timers while this is true.</summary>
        public bool IsModalOpen
        {
            get { return Current != null; }
        }

        /// <summary>A dialog became current; the view builds and shows it.</summary>
        public event Action<DialogHandle> Showing;

        /// <summary>A dialog closed (after its callback ran); the view tears it down.</summary>
        public event Action<DialogHandle> Closed;

        /// <summary>Raised when <see cref="IsModalOpen"/> flips. It does not flicker between two queued dialogs.</summary>
        public event Action<bool> ModalStateChanged;

        /// <summary>Adds a request. Returns the handle (an existing one when the request has a dedupe key that is already queued or showing). Shows it at once when nothing else is showing.</summary>
        public DialogHandle Enqueue(DialogRequest request)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!string.IsNullOrEmpty(request.DedupeKey))
            {
                DialogHandle existing = FindByKey(request.DedupeKey);
                if (existing != null)
                    return existing;
            }

            DialogHandle handle = new DialogHandle(++_sequence, request);
            if (_shutDown)
            {
                // Nobody will ever see this dialog; closing it here keeps the handle honest without running a callback that might ask for yet another one.
                handle.State = DialogHandleState.Closed;
                handle.Result = new DialogOutcome(-1, DialogCloseReason.Cleared);
                return handle;
            }
            int index = _waiting.Count;
            while (index > 0 && _waiting[index - 1].Request.Priority < request.Priority)
                index--;
            _waiting.Insert(index, handle);

            if (_dispatching == 0)
                Advance();
            return handle;
        }

        /// <summary>
        /// Closes the dialog with a button. Returns false when <paramref name="handle"/> is not the current dialog (already closed, still
        /// waiting) or the index is out of range.
        /// </summary>
        public bool Complete(DialogHandle handle, int buttonIndex, DialogCloseReason reason = DialogCloseReason.Button)
        {
            if (handle == null || !ReferenceEquals(handle, Current))
                return false;
            int count = handle.Request.Buttons.Count;
            if (reason == DialogCloseReason.Button && (buttonIndex < 0 || buttonIndex >= count))
                return false;
            Finish(handle, new DialogOutcome(buttonIndex, reason));
            return true;
        }

        /// <summary>
        /// Back, Escape or navigation-cancel. Returns true when a dialog is open (back is consumed even by a dialog that cannot be dismissed).
        /// A dismissible dialog closes with its cancel button's index, or -1 when it has none.
        /// </summary>
        public bool RequestBack()
        {
            DialogHandle current = Current;
            if (current == null)
                return false;
            if (current.Request.DismissOnBack)
                Finish(current, new DialogOutcome(current.Request.CancelIndex, DialogCloseReason.Back));
            return true;
        }

        /// <summary>A tap on the scrim. Returns true when it closed the dialog.</summary>
        public bool RequestScrimTap()
        {
            DialogHandle current = Current;
            if (current == null || !current.Request.DismissOnScrim)
                return false;
            Finish(current, new DialogOutcome(current.Request.CancelIndex, DialogCloseReason.Scrim));
            return true;
        }

        /// <summary>Closes one dialog, showing or waiting, as <see cref="DialogCloseReason.Programmatic"/>.</summary>
        public bool Cancel(DialogHandle handle)
        {
            if (handle == null || handle.State == DialogHandleState.Closed)
                return false;
            if (ReferenceEquals(handle, Current))
            {
                Finish(handle, new DialogOutcome(-1, DialogCloseReason.Programmatic));
                return true;
            }
            if (!_waiting.Remove(handle))
                return false;
            Discard(handle, DialogCloseReason.Programmatic);
            return true;
        }

        /// <summary>Closes the current dialog and drops every waiting one, each callback once with <see cref="DialogCloseReason.Cleared"/>.</summary>
        public void CloseAll()
        {
            CloseWhere(null, DialogCloseReason.Cleared);
        }

        /// <summary>
        /// The owner is going away: like <see cref="CloseAll"/>, and from now on every request is dropped (its handle is closed as <see cref="DialogCloseReason.Cleared"/>
        /// and its callback does not run). A callback that shows a follow-up dialog while the queue is being closed therefore cannot leave a modal behind.
        /// </summary>
        public void Shutdown()
        {
            _shutDown = true;
            CloseAll();
        }

        /// <summary>The app became active: closes every dialog whose request asked for it, with <see cref="DialogCloseReason.AppResumed"/>.</summary>
        public void DismissOnResume()
        {
            CloseWhere(request => request.DismissOnAppResume, DialogCloseReason.AppResumed);
        }

        void CloseWhere(Func<DialogRequest, bool> match, DialogCloseReason reason)
        {
            _dispatching++;
            try
            {
                List<DialogHandle> discarded = new List<DialogHandle>();
                for (int i = 0; i < _waiting.Count; i++)
                {
                    if (match == null || match(_waiting[i].Request))
                        discarded.Add(_waiting[i]);
                }
                for (int i = 0; i < discarded.Count; i++)
                    _waiting.Remove(discarded[i]);
                for (int i = 0; i < discarded.Count; i++)
                    Discard(discarded[i], reason);
                DialogHandle current = Current;
                if (current != null && (match == null || match(current.Request)))
                    FinishCore(current, new DialogOutcome(-1, reason));
            }
            finally
            {
                _dispatching--;
            }
            if (_dispatching == 0)
                Advance();
        }

        void Finish(DialogHandle handle, DialogOutcome result)
        {
            _dispatching++;
            try
            {
                FinishCore(handle, result);
            }
            finally
            {
                _dispatching--;
            }
            if (_dispatching == 0)
                Advance();
        }

        void FinishCore(DialogHandle handle, DialogOutcome result)
        {
            Current = null;
            handle.State = DialogHandleState.Closed;
            handle.Result = result;
            Invoke(handle.Request.Callback, result);
            RaiseClosed(handle);
        }

        void Discard(DialogHandle handle, DialogCloseReason reason)
        {
            DialogOutcome result = new DialogOutcome(-1, reason);
            handle.State = DialogHandleState.Closed;
            handle.Result = result;
            Invoke(handle.Request.Callback, result);
        }

        void Advance()
        {
            if (Current == null && _waiting.Count > 0)
            {
                DialogHandle next = _waiting[0];
                _waiting.RemoveAt(0);
                next.State = DialogHandleState.Showing;
                Current = next;
                PublishModalState();
                Action<DialogHandle> handler = Showing;
                if (handler != null)
                {
                    try
                    {
                        handler(next);
                    }
                    catch (Exception ex)
                    {
                        // A view that cannot build the dialog must not leave a modal open that nobody can see or answer: close it and go on.
                        UiLog.Error(ex);
                        if (ReferenceEquals(Current, next))
                            Finish(next, new DialogOutcome(-1, DialogCloseReason.Programmatic));
                    }
                }
                return;
            }
            PublishModalState();
        }

        void PublishModalState()
        {
            bool open = Current != null;
            if (open == _publishedModal)
                return;
            _publishedModal = open;
            Action<bool> handler = ModalStateChanged;
            if (handler != null)
                handler(open);
        }

        void RaiseClosed(DialogHandle handle)
        {
            Action<DialogHandle> handler = Closed;
            if (handler == null)
                return;
            try
            {
                handler(handle);
            }
            catch (Exception ex)
            {
                // A throwing view teardown must not stop the next dialog from showing.
                UiLog.Error(ex);
            }
        }

        static void Invoke(Action<DialogOutcome> callback, DialogOutcome result)
        {
            if (callback == null)
                return;
            try
            {
                callback(result);
            }
            catch (Exception ex)
            {
                UiLog.Error(ex);
            }
        }

        DialogHandle FindByKey(string key)
        {
            if (Current != null && string.Equals(Current.Request.DedupeKey, key, StringComparison.Ordinal))
                return Current;
            for (int i = 0; i < _waiting.Count; i++)
            {
                if (string.Equals(_waiting[i].Request.DedupeKey, key, StringComparison.Ordinal))
                    return _waiting[i];
            }
            return null;
        }
    }
}
