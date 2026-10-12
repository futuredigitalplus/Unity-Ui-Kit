using System;
using System.Collections.Generic;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>A responsive device catalog: the list the Responsive Manager previews on. Plain serializable values (JSON).</summary>
    [Serializable]
    public sealed class ResponsiveDeviceCatalogData
    {
        /// <summary>The devices, in display order.</summary>
        public List<ResponsiveDeviceSpec> devices = new List<ResponsiveDeviceSpec>();

        /// <summary>The problems that make an entry unusable (empty when the catalog is sound): missing id or name, duplicate id, no screen, a size or DPI that is not positive.</summary>
        public List<string> Validate()
        {
            var problems = new List<string>();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (devices == null)
            {
                problems.Add("no device list");
                return problems;
            }
            for (int i = 0; i < devices.Count; i++)
            {
                ResponsiveDeviceSpec d = devices[i];
                string label = d == null ? "#" + i : (string.IsNullOrEmpty(d.id) ? "#" + i : d.id);
                if (d == null)
                {
                    problems.Add(label + ": null entry");
                    continue;
                }
                if (string.IsNullOrEmpty(d.id))
                    problems.Add(label + ": no id");
                else if (!ids.Add(d.id))
                    problems.Add(label + ": duplicate id");
                if (string.IsNullOrEmpty(d.name))
                    problems.Add(label + ": no name");
                if (d.screens == null || d.screens.Count == 0)
                {
                    problems.Add(label + ": no screen");
                    continue;
                }
                for (int s = 0; s < d.screens.Count; s++)
                {
                    ResponsiveScreenSpec screen = d.screens[s];
                    if (screen == null || screen.width <= 0 || screen.height <= 0 || !(screen.dpi > 0f))
                        problems.Add(label + ": screen " + s + " has no size or DPI");
                }
            }
            return problems;
        }
    }
}
