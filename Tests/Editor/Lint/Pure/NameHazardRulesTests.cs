using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Lint.Tests
{
    public class NameHazardRulesTests
    {
        private static readonly HashSet<string> UnityNames = new HashSet<string> { "Input", "Layout", "Console", "Animation", "Tooltip", "Button" };

        [Test]
        public void Find_FlagsNamespaceSegmentsAndTypesThatEqualUnityTypes()
        {
            var found = NameHazardRules.Find(
                new[] { "FutureDigitalPlus.UIKit.Input", "FutureDigitalPlus.UIKit.Hud.Layout", "FutureDigitalPlus.UIKit.Hud", "Other.Console" },
                new[] { "FutureDigitalPlus.UIKit.Hud.Button", "FutureDigitalPlus.UIKit.Hud.Meter", "FutureDigitalPlus.UIKit.Hud.Tooltip`1" },
                UnityNames, new HashSet<string>()).ToList();

            Assert.That(found.Select(f => f.Rule + " " + f.File), Is.EquivalentTo(new[]
            {
                "name-hazard-namespace FutureDigitalPlus.UIKit.Input",
                "name-hazard-namespace FutureDigitalPlus.UIKit.Hud.Layout",
                "name-hazard-type FutureDigitalPlus.UIKit.Hud.Button",
                "name-hazard-type FutureDigitalPlus.UIKit.Hud.Tooltip`1",
            }));
        }

        [Test]
        public void Find_AllowListSilencesKnownNamesOnlyWhereListed()
        {
            var allowed = new HashSet<string> { "FutureDigitalPlus.UIKit.Animation", "FutureDigitalPlus.UIKit.Extras.Tooltip" };

            var found = NameHazardRules.Find(
                new[] { "FutureDigitalPlus.UIKit.Animation", "FutureDigitalPlus.UIKit.Animation.Extensions", "FutureDigitalPlus.UIKit.Hud.Animation" },
                new[] { "FutureDigitalPlus.UIKit.Extras.Tooltip", "FutureDigitalPlus.UIKit.Hud.Tooltip" },
                UnityNames, allowed).ToList();

            Assert.That(found.Select(f => f.File), Is.EquivalentTo(new[] { "FutureDigitalPlus.UIKit.Hud.Animation", "FutureDigitalPlus.UIKit.Hud.Tooltip" }),
                "an accepted namespace covers its children; an accepted type covers only that type");
        }

        [Test]
        public void Find_IgnoresNamespacesOutsideTheKit()
        {
            Assert.IsEmpty(NameHazardRules.Find(new[] { "Game.Input", "FutureDigitalPlus.UIKit" }, new string[0], UnityNames, new HashSet<string>()).ToList());
        }
    }
}
