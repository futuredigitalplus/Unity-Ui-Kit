using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// A small all-caps style heading above a group of rows (a settings group). It is a <see cref="Label"/>, so text binds like any label. UXML:
    /// <c>&lt;fdp:SectionHeader text="Sound"/&gt;</c> with <c>xmlns:fdp="FutureDigitalPlus.UIKit.Foundation"</c>.
    /// </summary>
    [UxmlElement]
    public partial class SectionHeader : Label
    {
        /// <summary>USS class of the header.</summary>
        public const string UssClassName = "fdp-section-header";

        /// <summary>Creates an empty header.</summary>
        public SectionHeader()
        {
            AddToClassList(UssClassName);
            pickingMode = PickingMode.Ignore;
        }
    }
}
