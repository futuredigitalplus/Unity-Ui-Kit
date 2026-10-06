// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
using UnityEditor;
using UnityEngine;

namespace FutureDigitalPlus.UIKit.Components.Samples.Counter.Editor
{
    public class CounterComponentWindow : EditorWindow
    {
        [MenuItem("Tools/Future Digital Plus UI Kit/Components Examples/Counter")]
        private static void ShowWindow()
        {
            var window = GetWindow<CounterComponentWindow>();
            window.titleContent = new GUIContent("Counter");
            window.Show();
        }

        private void CreateGUI()
        {
            rootVisualElement.style.minWidth = 120;
            rootVisualElement.style.minHeight = 40;
            rootVisualElement.Add(new CounterComponent());
        }
    }
}