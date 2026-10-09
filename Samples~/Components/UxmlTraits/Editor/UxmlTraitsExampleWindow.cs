// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Components.Samples.UxmlTraits.Editor
{
    public class UxmlTraitsExampleWindow : UnityEditor.EditorWindow
    {
        [UnityEditor.MenuItem("Tools/FDP UI Kit/Components Examples/UxmlTraits and UxmlName")]
        private static void ShowWindow()
        {
            var window = GetWindow<UxmlTraitsExampleWindow>();
            window.titleContent = new GUIContent("UxmlTraits and UxmlName");
            window.minSize = new Vector2(470, 250);
            window.Show();
        }

        private void CreateGUI()
        {
            var layout = Resources.Load<VisualTreeAsset>("FutureDigitalPlus/UIKit/Samples/Components/UxmlTraits/UxmlTraitsExampleWindow");

            layout.CloneTree(rootVisualElement);

            var styles = Resources.Load<StyleSheet>("FutureDigitalPlus/UIKit/Samples/Components/UxmlTraits/UxmlTraitsExampleWindow.style");
            
            rootVisualElement.styleSheets.Add(styles);
        }
    }
}