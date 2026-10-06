// FutureDigitalPlus: renamed by Tools~/rename/rename.py (namespaces, resource paths); see SOURCE.md "Rename record".
using System;
using UnityEngine;

namespace FutureDigitalPlus.UIKit.Animation
{
    [Serializable]
    public class AnimTime
    {
        public int startValue;
        public int duration;
        public int delay;
        [SerializeField] private Easing easing;
        
        public Func<float, float> Easings => GetEasing.Get(easing);

        public AnimTime(int duration = default, int delay = default, Easing easing = Easing.InOutCubic)
        {
            this.duration = duration;
            this.delay = delay;
            this.easing = easing;
        }
    }
}
