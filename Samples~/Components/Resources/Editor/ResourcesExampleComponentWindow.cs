// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
using UnityEditor;
using UnityEngine;

namespace FutureDigitalPlus.UIKit.Components.Samples.Resources.Editor
{
    public class ResourcesExampleComponentWindow : EditorWindow
    {
        [MenuItem("Tools/Future Digital Plus UI Kit/Components Examples/Loading Assets/Resources")]
        private static void ShowWindow()
        {
            var window = GetWindow<ResourcesExampleComponentWindow>();
            window.titleContent = new GUIContent("Loading from Resources");
            window.Show();
        }

        private void CreateGUI()
        {
            rootVisualElement.style.minWidth = 150;
            rootVisualElement.style.minHeight = 40;
            rootVisualElement.Add(new ResourcesExampleComponent());
        }
    }
}