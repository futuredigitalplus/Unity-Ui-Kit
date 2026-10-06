// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
using UnityEditor;
using UnityEngine;

namespace FutureDigitalPlus.UIKit.Components.Samples.Addressables.Editor
{
    public class AddressablesExampleComponentWindow : EditorWindow
    {
        [MenuItem("Tools/Future Digital Plus UI Kit/Components Examples/Loading Assets/Addressables")]
        private static void ShowWindow()
        {
            var window = GetWindow<AddressablesExampleComponentWindow>();
            window.titleContent = new GUIContent("Loading from Addressables");
            window.Show();
        }

        private void CreateGUI()
        {
            rootVisualElement.style.minWidth = 150;
            rootVisualElement.style.minHeight = 40;
            rootVisualElement.Add(new AddressablesExampleComponent());
        }
    }
}
