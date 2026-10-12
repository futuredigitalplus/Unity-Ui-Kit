using System;
using System.Collections.Generic;

namespace FutureDigitalPlus.UIKit.Foundation
{
    /// <summary>
    /// The size and scale a game sets per form factor and orientation (<see cref="ResponsiveRule"/>), and how a screen finds its rule. <see cref="Resolve"/> looks for,
    /// in order: the rule of the form factor and the exact orientation; the rule of the form factor for <see cref="ResponsiveOrientation.Any"/>; the same two for the
    /// fallback form factor (<see cref="DeviceFormFactors.FallbackOf"/>: the covers fall back to the phone, a fold inner screen to the pad); then a rule that changes nothing.
    /// So an empty table keeps today's behaviour exactly, and a game only writes the rules it needs. Serializable: a rule asset holds one.
    /// </summary>
    [Serializable]
    public sealed class ResponsiveRules
    {
        /// <summary>The rules, in any order. When two rules have the same form factor and orientation, the first wins.</summary>
        public List<ResponsiveRule> rules = new List<ResponsiveRule>();

        /// <summary>The rule for <paramref name="form"/> held in <paramref name="landscape"/>, after the fallbacks described on the class. Never fails.</summary>
        public ResponsiveRule Resolve(DeviceFormFactor form, bool landscape)
        {
            ResponsiveOrientation exact = landscape ? ResponsiveOrientation.Landscape : ResponsiveOrientation.Portrait;
            ResponsiveRule rule;
            if (TryFind(form, exact, out rule) || TryFind(form, ResponsiveOrientation.Any, out rule))
                return rule;
            DeviceFormFactor fallback = DeviceFormFactors.FallbackOf(form);
            if (fallback != form && (TryFind(fallback, exact, out rule) || TryFind(fallback, ResponsiveOrientation.Any, out rule)))
                return rule;
            return ResponsiveRule.Identity(form, ResponsiveOrientation.Any);
        }

        /// <summary>The rule stored for exactly <paramref name="form"/> and <paramref name="orientation"/>, without fallbacks. False when there is none.</summary>
        public bool TryFind(DeviceFormFactor form, ResponsiveOrientation orientation, out ResponsiveRule rule)
        {
            if (rules != null)
            {
                for (int i = 0; i < rules.Count; i++)
                {
                    if (rules[i].form == form && rules[i].orientation == orientation)
                    {
                        rule = rules[i];
                        return true;
                    }
                }
            }
            rule = default;
            return false;
        }

        /// <summary>
        /// Stores <paramref name="rule"/>, replacing the rule with the same form factor and orientation. An identity rule is stored too: it stops the fallback (a flip
        /// cover at scale 1 while the phone rule scales down). <see cref="Clear"/> removes an entry.
        /// </summary>
        public void Set(ResponsiveRule rule)
        {
            if (rules == null)
                rules = new List<ResponsiveRule>();
            for (int i = 0; i < rules.Count; i++)
            {
                if (rules[i].form == rule.form && rules[i].orientation == rule.orientation)
                {
                    rules[i] = rule;
                    return;
                }
            }
            rules.Add(rule);
        }

        /// <summary>Removes the rule of <paramref name="form"/> and <paramref name="orientation"/>, if any.</summary>
        public void Clear(DeviceFormFactor form, ResponsiveOrientation orientation)
        {
            if (rules == null)
                return;
            for (int i = rules.Count - 1; i >= 0; i--)
            {
                if (rules[i].form == form && rules[i].orientation == orientation)
                    rules.RemoveAt(i);
            }
        }

        /// <summary>A copy that shares no list with this table.</summary>
        public ResponsiveRules Clone()
        {
            var copy = new ResponsiveRules();
            if (rules != null)
                copy.rules.AddRange(rules);
            return copy;
        }

        /// <summary>A number that changes whenever a rule changes (the driver compares it to know when to re-evaluate).</summary>
        public int ContentHash()
        {
            unchecked
            {
                int hash = 17;
                if (rules == null)
                    return hash;
                for (int i = 0; i < rules.Count; i++)
                {
                    ResponsiveRule r = rules[i];
                    hash = hash * 31 + (int)r.form;
                    hash = hash * 31 + (int)r.orientation;
                    hash = hash * 31 + r.Scale.GetHashCode();
                    hash = hash * 31 + r.MaxContentWidth.GetHashCode();
                }
                return hash;
            }
        }
    }
}
