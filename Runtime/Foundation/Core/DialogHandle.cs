namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>Reference to one request inside a <see cref="DialogQueue"/>. Created by the queue; callers only keep it to cancel.</summary>
    public sealed class DialogHandle
    {
        /// <summary>Arrival number, unique inside one queue.</summary>
        public int Sequence { get; }

        /// <summary>The request this handle stands for.</summary>
        public DialogRequest Request { get; }

        /// <summary>Whether the request is waiting, showing or closed.</summary>
        public DialogHandleState State { get; internal set; }

        /// <summary>The result once <see cref="State"/> is Closed.</summary>
        public DialogOutcome Result { get; internal set; }

        internal DialogHandle(int sequence, DialogRequest request)
        {
            Sequence = sequence;
            Request = request;
            State = DialogHandleState.Queued;
        }
    }
}
