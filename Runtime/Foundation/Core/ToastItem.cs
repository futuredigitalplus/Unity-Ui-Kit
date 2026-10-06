using System;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>One toast: text, kind and how long it stays after it appeared.</summary>
    public sealed class ToastItem : IEquatable<ToastItem>
    {
        /// <summary>Default time a toast stays after it appeared.</summary>
        public const float DefaultSeconds = 2.5f;

        /// <summary>What the toast says.</summary>
        public UiText Text { get; }

        /// <summary>How the toast is styled.</summary>
        public ToastKind Kind { get; }

        /// <summary>Time the toast stays after it appeared, at least 0.2 seconds.</summary>
        public float Seconds { get; }

        /// <summary>Creates a toast.</summary>
        public ToastItem(UiText text, ToastKind kind = ToastKind.Info, float seconds = DefaultSeconds)
        {
            Text = text ?? UiText.Empty;
            Kind = kind;
            Seconds = seconds > 0.2f ? seconds : 0.2f;
        }

        /// <summary>Same text and kind (the duration does not matter); used to coalesce repeats.</summary>
        public bool Equals(ToastItem other)
        {
            return !ReferenceEquals(other, null) && Kind == other.Kind && Text.Equals(other.Text);
        }

        /// <summary>Same text and kind.</summary>
        public override bool Equals(object obj)
        {
            return Equals(obj as ToastItem);
        }

        /// <summary>Hash of text and kind.</summary>
        public override int GetHashCode()
        {
            return Text.GetHashCode() * 31 + (int)Kind;
        }
    }
}
