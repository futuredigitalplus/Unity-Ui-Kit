using System;
using System.Collections.Generic;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>One device of a responsive catalog: a phone, a tablet, or a foldable with several screens (<see cref="ResponsiveScreenSpec"/>). Plain serializable values.</summary>
    [Serializable]
    public sealed class ResponsiveDeviceSpec
    {
        /// <summary>Stable id, lower case with dashes (for example <c>galaxy-z-flip7</c>).</summary>
        public string id;

        /// <summary>Display name.</summary>
        public string name;

        /// <summary>Group shown in tools: Phone, Pad, Flip, Fold.</summary>
        public string group;

        /// <summary>The screens; the first one is the default.</summary>
        public List<ResponsiveScreenSpec> screens = new List<ResponsiveScreenSpec>();
    }
}
