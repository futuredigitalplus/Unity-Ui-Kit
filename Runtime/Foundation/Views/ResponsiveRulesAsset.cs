using UnityEngine;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// A project asset that holds the <see cref="ResponsiveRules"/> of a game: the scale multiplier and content width per form factor and orientation. The Responsive
    /// Manager (<c>Tools &gt; FDP UI Kit &gt; Responsive Manager</c>) edits it; the game hands <see cref="Rules"/> to its <see cref="PanelScaleDriver"/>, so a device build
    /// uses exactly what was set in the Editor.
    /// <code>
    /// var asset = Resources.Load&lt;ResponsiveRulesAsset&gt;("ResponsiveRules");
    /// if (asset != null) driver.Rules = asset.Rules;
    /// </code>
    /// </summary>
    [CreateAssetMenu(menuName = "FDP UI Kit/Responsive Rules", fileName = "ResponsiveRules")]
    public sealed class ResponsiveRulesAsset : ScriptableObject
    {
        [SerializeField] ResponsiveRules _rules = new ResponsiveRules();

        /// <summary>The rules. Edit them in place; a driver that holds them notices the change on its next tick.</summary>
        public ResponsiveRules Rules
        {
            get
            {
                if (_rules == null)
                    _rules = new ResponsiveRules();
                return _rules;
            }
        }
    }
}
