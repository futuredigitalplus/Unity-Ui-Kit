using UnityEngine;
using UnityEngine.UIElements;
using FutureDigitalPlus.UIKit.Components;
using FutureDigitalPlus.UIKit.Animation;
using FutureDigitalPlus.UIKit.Extras;
namespace DocsCheck.GettingStartedInline
{
    [Layout("UI/MainMenu")] public partial class MainMenu : UIComponent { [Query("play")] Button play; }
    static class S
    {
        public static void Inline(VisualElement root, VisualElement element, Button button)
        {
            root.Add(new MainMenu());
            element.AnimateOpacity(0f, 1f, 250);
            element.AnimateWidth(0f, 300f, 600);
            element.HoverBackground(new StyleColor(Color.clear), new Color(0.2f, 0.35f, 0.6f), animate: true);
           
            var tooltip = new Tooltip(); root.Add(tooltip); button.AddManipulator(new TooltipManipulator(tooltip));
        }
    }
}

namespace DocsCheck.ComponentsDocs
{
    [Layout("UI/Counter")]             // Resources/UI/Counter.uxml (a bare [Layout] resolves by convention, see upstream wiki)
    [Stylesheet("UI/Counter.style")]   // Resources/UI/Counter.style.uss
    public partial class CounterComponent : UIComponent
    {
        [Query("count-label")] public Label CountLabel;
        public override void OnInit() => CountLabel.text = "0";
    }
}

