using System.Linq;
using NUnit.Framework;

namespace FutureDigitalPlus.UIKit.Lint.Tests
{
    /// <summary>The assembly definition rules on small inputs.</summary>
    public class AsmdefRulesTests
    {
        private static string Asmdef(string name, string references = "", string constraints = "", bool noEngine = false) =>
            "{ \"name\": \"" + name + "\", \"references\": [" + references + "], \"defineConstraints\": [" + constraints + "], \"noEngineReferences\": " + (noEngine ? "true" : "false") + " }";

        [Test]
        public void ParsesNameAndArrays()
        {
            string json = Asmdef("A.B", "\"X\", \"Y\"", "\"C1\"");
            Assert.AreEqual("A.B", AsmdefRules.NameOf(json));
            Assert.AreEqual(new[] { "X", "Y" }, AsmdefRules.StringArray(json, "references"));
            Assert.AreEqual(new[] { "C1" }, AsmdefRules.StringArray(json, "defineConstraints"));
            Assert.IsEmpty(AsmdefRules.StringArray(json, "missingField"));
        }

        [Test]
        public void OurOwnAndUnityReferencesAreFine()
        {
            string json = Asmdef("FutureDigitalPlus.UIKit.X", "\"FutureDigitalPlus.UIKit.Y\", \"UnityEngine.TestRunner\", \"Unity.Burst\", \"GUID:0123456789abcdef0123456789abcdef\"");
            Assert.IsEmpty(AsmdefRules.Check("x.asmdef", json));
        }

        [Test]
        public void AGameAssemblyIsForeign()
        {
            var findings = AsmdefRules.Check("x.asmdef", Asmdef("FutureDigitalPlus.UIKit.X", "\"FCS.Game\"")).ToList();
            Assert.AreEqual(1, findings.Count);
            Assert.AreEqual("asmdef-foreign-reference", findings[0].Rule);
        }

        [Test]
        public void AnOptionalPackageNeedsADefineConstraint()
        {
            var bad = AsmdefRules.Check("x.asmdef", Asmdef("FutureDigitalPlus.UIKit.X", "\"Unity.InputSystem\"")).ToList();
            Assert.AreEqual(1, bad.Count);
            Assert.AreEqual("asmdef-optional-unconstrained", bad[0].Rule);
            Assert.IsEmpty(AsmdefRules.Check("x.asmdef", Asmdef("FutureDigitalPlus.UIKit.X", "\"Unity.InputSystem\"", "\"FDP_UIKIT_INPUTSYSTEM\"")));
        }

        [Test]
        public void ACoreAssemblyHasNoEngineAndOnlyCoreReferences()
        {
            Assert.IsEmpty(AsmdefRules.Check("c.asmdef", Asmdef("FutureDigitalPlus.UIKit.A.Core", "\"FutureDigitalPlus.UIKit.B.Core\"", "", noEngine: true)));
            Assert.AreEqual(1, AsmdefRules.Check("c.asmdef", Asmdef("FutureDigitalPlus.UIKit.A.Core")).Count());
            Assert.AreEqual(1, AsmdefRules.Check("c.asmdef", Asmdef("FutureDigitalPlus.UIKit.A.Core", "\"FutureDigitalPlus.UIKit.A\"", "", noEngine: true)).Count());
        }
    }
}
