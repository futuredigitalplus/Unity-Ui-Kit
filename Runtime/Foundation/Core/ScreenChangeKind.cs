namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>Kind of a stack operation.</summary>
    public enum ScreenChangeKind
    {
        /// <summary>Nothing happened (pop at the root, push of the id that is already on top, unknown id for PopTo).</summary>
        None = 0,
        /// <summary>A screen was added on top.</summary>
        Push = 1,
        /// <summary>The top screen was replaced.</summary>
        Replace = 2,
        /// <summary>Screens were removed from the top.</summary>
        Pop = 3,
        /// <summary>The stack was cleared and one screen put in.</summary>
        Reset = 4
    }
}
