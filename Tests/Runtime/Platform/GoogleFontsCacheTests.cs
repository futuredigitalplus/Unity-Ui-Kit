#if DS_WEBREQUEST
using System.IO;
using System.Linq;
using FutureDigitalPlus.UIKit.DesignSystem.Typography;
using NUnit.Framework;
using UnityEngine;

namespace FutureDigitalPlus.UIKit.PlatformSmoke.Tests
{
    /// <summary>The runtime font download needs a writable cache folder; consoles and tvOS must say so up front instead of failing after the download.</summary>
    public class GoogleFontsCacheTests
    {
        [TearDown]
        public void Reset() => DsGoogleFonts.CacheDirectory = null;

        [Test]
        public void CacheDirectory_DefaultsToAFolderBelowThePersistentOrTemporaryPath()
        {
            DsGoogleFonts.CacheDirectory = null;
            string dir = DsGoogleFonts.CacheDirectory;

            Assert.IsNotEmpty(dir);
            Assert.That(dir.Replace('\\', '/'), Does.EndWith("/" + DsGoogleFonts.CacheSubfolder));

            string root = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(dir)));
            Assert.That(new[] { Application.persistentDataPath, Application.temporaryCachePath }.Select(p => p.Replace('\\', '/')),
                Contains.Item(root.Replace('\\', '/')));
        }

        [Test]
        public void CacheDirectory_CanBeSetAndReset()
        {
            string custom = Path.Combine(Path.GetTempPath(), "fdp-font-cache-test");

            DsGoogleFonts.CacheDirectory = custom;
            Assert.AreEqual(custom, DsGoogleFonts.CacheDirectory);

            DsGoogleFonts.CacheDirectory = "";
            Assert.AreNotEqual(custom, DsGoogleFonts.CacheDirectory, "an empty value returns to the default");
        }

        [TestCase(false, false, false, true, TestName = "desktop or mobile")]
        [TestCase(true, false, false, false, TestName = "console without a chosen folder")]
        [TestCase(false, true, false, false, TestName = "tvOS without a chosen folder")]
        [TestCase(true, false, true, true, TestName = "console with a chosen folder")]
        [TestCase(false, true, true, true, TestName = "tvOS with a chosen folder")]
        public void IsSupportedOn(bool console, bool tvOs, bool chosen, bool expected) =>
            Assert.AreEqual(expected, DsGoogleFonts.IsSupportedOn(console, tvOs, chosen));

        [Test]
        public void IsSupported_IsTrueOnTheMachineRunningTheTests()
        {
            if (Application.isConsolePlatform || Application.platform == RuntimePlatform.tvOS) Assert.Ignore("A console or tvOS run.");
            DsGoogleFonts.CacheDirectory = null;
            Assert.IsTrue(DsGoogleFonts.IsSupported);
        }
    }
}
#endif
