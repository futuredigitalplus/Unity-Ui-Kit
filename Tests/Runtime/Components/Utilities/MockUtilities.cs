// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
using System.Threading.Tasks;
using NSubstitute;
using FutureDigitalPlus.UIKit.Components.DependencyInjection;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Components.Tests.Utilities
{
    public static class MockUtilities
    {
        public static IAssetSource CreateMockSource()
        {
            var source = Substitute.For<IAssetSource>();
            source.LoadAsset<VisualTreeAsset>(Arg.Any<string>())
                .Returns(Task.FromResult(ScriptableObject.CreateInstance<VisualTreeAsset>()));
            source.LoadAsset<StyleSheet>(Arg.Any<string>())
                .Returns(Task.FromResult(ScriptableObject.CreateInstance<StyleSheet>()));
            return source;
        }

        public static IDependencyConsumer CreateDependencyConsumer(IDependency[] dependencies)
        {
            var consumer = Substitute.For<IDependencyConsumer>();
            consumer.GetDependencies().Returns(dependencies);
            return consumer;
        }
    }
}
