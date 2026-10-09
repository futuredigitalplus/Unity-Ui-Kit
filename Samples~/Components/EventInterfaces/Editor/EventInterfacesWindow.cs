// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
using UnityEditor;
using UnityEngine;

namespace FutureDigitalPlus.UIKit.Components.Samples.EventInterfaces.Editor
{
    public class EventInterfacesWindow : EditorWindow
    {
        [MenuItem("Tools/FDP UI Kit/Components Examples/Event Interfaces")]
        private static void ShowWindow()
        {
            var window = GetWindow<EventInterfacesWindow>();
            window.titleContent = new GUIContent("Event Interfaces");
            window.minSize = new Vector2(360, 100);
            window.Show();
        }

        private void CreateGUI()
        {
            rootVisualElement.Add(new EventInterfacesView());
        }
    }
}

