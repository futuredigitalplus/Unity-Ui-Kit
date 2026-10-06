// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
using UnityEditor;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Components.Tests.Editor.Interfaces
{
    public class InterfacesTestEditorWindow : EditorWindow
    {
        private void CreateGUI()
        {
            rootVisualElement.Add(new Label("Hello world"));    
        }

        public void AddTestComponent(UIComponent component)
        {
            rootVisualElement.Add(component);
        }
    }
}