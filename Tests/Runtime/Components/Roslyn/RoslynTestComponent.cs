// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
using UnityEngine;

namespace FutureDigitalPlus.UIKit.Components.Tests.Roslyn
{
    public partial class RoslynTestComponent : UIComponent
    {
        public enum Greetings
        {
            Hello,
            Hi,
            Morning
        }

        [UxmlTrait]
        public Color TextColor = Color.cyan;

        [UxmlTrait(Name = "time")]
        public double CurrentTime;

        [UxmlTrait]
        public Greetings Greeting = Greetings.Morning;

        [UxmlTrait]
        public bool Enabled = true;
    }
}
