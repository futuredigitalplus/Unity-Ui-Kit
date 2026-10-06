namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>Role of a dialog button.</summary>
    public enum DialogButtonRole
    {
        /// <summary>A normal action; the view styles the first Default button as primary.</summary>
        Default = 0,

        /// <summary>Dismisses without action. Back and scrim taps report this button's index.</summary>
        Cancel = 1,

        /// <summary>An action that cannot be undone (reset, surrender); styled in the danger colour.</summary>
        Destructive = 2
    }
}
