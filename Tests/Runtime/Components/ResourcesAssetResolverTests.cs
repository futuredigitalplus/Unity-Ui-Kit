// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Components.Tests
{
    [TestFixture]
    public class ResourcesAssetSourceTests : AssetSourceTestSuite<ResourcesAssetSource>
    {
        [UnityTest]
        public IEnumerator Should_Be_Able_To_Load_Existing_Asset()
        {
            yield return Assert_Loads_Existing_Asset<VisualTreeAsset>(
                "FutureDigitalPlus/UIKit/Tests/Components/LayoutAttributeTests"
            );
        }

        [UnityTest]
        public IEnumerator Should_Be_Able_To_Tell_If_Asset_Exists()
        {
            yield return Assert_Tells_If_Asset_Exists(
                "FutureDigitalPlus/UIKit/Tests/Components/LayoutAttributeTests"
            );
        }
    }
}
