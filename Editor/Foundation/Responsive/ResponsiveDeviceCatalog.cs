using System;
using System.Collections.Generic;
using System.IO;
using FutureDigitalPlus.UIKit.Location;
using UnityEngine;

namespace FutureDigitalPlus.UIKit.Foundation.Responsive
{
    /// <summary>
    /// The devices the Responsive Manager previews on: the kit's catalog (<see cref="KitCatalogPath"/>: current phones and tablets, and the cover and main or inner
    /// screens of flip and fold phones with their DPI, safe areas and hinge) plus the project's own devices (<see cref="ProjectCatalogPath"/>, same JSON format; an
    /// entry with the id of a kit device replaces it). Sizes are in pixels held upright, safe areas in pixels as top, bottom, left, right.
    /// </summary>
    public static class ResponsiveDeviceCatalog
    {
        /// <summary>The kit's catalog, relative to the kit folder.</summary>
        public const string KitCatalogPath = "Editor/Foundation/Responsive/ResponsiveDevices.json";

        /// <summary>The project's extra devices, relative to the project folder. Optional.</summary>
        public const string ProjectCatalogPath = "ProjectSettings/FdpResponsiveDevices.json";

        /// <summary>The kit's catalog merged with the project's. Problems (unreadable files, invalid entries) go to <paramref name="problems"/>; the usable entries are kept.</summary>
        public static ResponsiveDeviceCatalogData Load(List<string> problems = null)
        {
            ResponsiveDeviceCatalogData catalog = ReadFile(KitLocation.FullPathOf(KitCatalogPath), problems) ?? new ResponsiveDeviceCatalogData();
            string projectFile = Path.Combine(Directory.GetParent(Application.dataPath).FullName, ProjectCatalogPath);
            if (File.Exists(projectFile))
            {
                ResponsiveDeviceCatalogData extra = ReadFile(projectFile, problems);
                if (extra != null)
                    Merge(catalog, extra);
            }
            if (problems != null)
                problems.AddRange(catalog.Validate());
            return catalog;
        }

        /// <summary>Reads a catalog from JSON. Returns null for text that is not a catalog.</summary>
        public static ResponsiveDeviceCatalogData Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;
            try
            {
                ResponsiveDeviceCatalogData catalog = JsonUtility.FromJson<ResponsiveDeviceCatalogData>(json);
                if (catalog == null || catalog.devices == null)
                    return null;
                return catalog;
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        /// <summary>Adds the devices of <paramref name="extra"/> to <paramref name="catalog"/>; a device with an id that is already there replaces it in place.</summary>
        public static void Merge(ResponsiveDeviceCatalogData catalog, ResponsiveDeviceCatalogData extra)
        {
            if (catalog == null || extra == null || extra.devices == null)
                return;
            if (catalog.devices == null)
                catalog.devices = new List<ResponsiveDeviceSpec>();
            foreach (ResponsiveDeviceSpec device in extra.devices)
            {
                if (device == null)
                    continue;
                int index = catalog.devices.FindIndex(d => d != null && !string.IsNullOrEmpty(d.id) && d.id == device.id);
                if (index >= 0)
                    catalog.devices[index] = device;
                else
                    catalog.devices.Add(device);
            }
        }

        static ResponsiveDeviceCatalogData ReadFile(string path, List<string> problems)
        {
            if (!File.Exists(path))
            {
                problems?.Add(path + ": not found");
                return null;
            }
            ResponsiveDeviceCatalogData catalog = Parse(File.ReadAllText(path));
            if (catalog == null)
                problems?.Add(path + ": not a device catalog");
            return catalog;
        }
    }
}
