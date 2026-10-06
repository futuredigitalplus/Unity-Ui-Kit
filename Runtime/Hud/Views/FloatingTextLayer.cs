using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.Hud
{
    /// <summary>
    /// A transparent layer that shows floating combat text (damage numbers, heals, "+10"): each text rises and fades
    /// by the curves of a <see cref="FloatingTextModel"/>. The labels are pooled (one per slot, created on first use),
    /// so after warm-up spawning and animating allocate nothing. The layer is driven by the element scheduler only
    /// while a text is alive. Spawn positions are in panel space; use <see cref="WorldAnchorLayer"/> to project from the world.
    /// </summary>
    [UxmlElement]
    public partial class FloatingTextLayer : VisualElement
    {
        /// <summary>USS class of the element.</summary>
        public const string UssClassName = "fdp-floating-text-layer";

        private readonly FloatingTextModel _model;
        private readonly Label[] _labels;
        private readonly TickDriver _driver;
        private int _frameStamp = -1;

        /// <summary>Creates a layer with 24 slots.</summary>
        public FloatingTextLayer() : this(24)
        {
        }

        /// <summary>Creates a layer with <paramref name="capacity"/> slots.</summary>
        public FloatingTextLayer(int capacity)
        {
            _model = new FloatingTextModel(capacity);
            _labels = new Label[_model.Capacity];
            AddToClassList(UssClassName);
            pickingMode = PickingMode.Ignore;
            _driver = new TickDriver(this, Step);
        }

        /// <summary>The timing and look parameters (rise distance, lifetime, overlap handling, per-frame cap).</summary>
        public FloatingTextModel Model => _model;

        /// <summary>Shows a text at <paramref name="position"/> (panel space, the text is centred on it). Returns false when the per-frame limit dropped it.</summary>
        public bool Spawn(Vector2 position, string text, FloatingTextKind kind = FloatingTextKind.Normal)
        {
            if (_frameStamp != Time.frameCount)
            {
                _frameStamp = Time.frameCount;
                _model.BeginFrame();
            }

            var local = this.WorldToLocal(position);
            var slot = _model.Spawn(local.x, local.y, text, kind);
            if (slot < 0) return false;

            var label = _labels[slot];
            if (label == null)
            {
                label = new Label { pickingMode = PickingMode.Ignore };
                label.AddToClassList("fdp-floating-text");
                _labels[slot] = label;
                Add(label);
            }

            label.text = text;
            label.EnableInClassList("fdp-floating-text--critical", kind == FloatingTextKind.Critical);
            label.EnableInClassList("fdp-floating-text--heal", kind == FloatingTextKind.Heal);
            label.EnableInClassList("fdp-floating-text--info", kind == FloatingTextKind.Info);
            Apply(slot);
            label.style.display = DisplayStyle.Flex;
            _driver.Request();
            return true;
        }

        /// <summary>Removes every text at once.</summary>
        public void ClearTexts()
        {
            _model.Clear();
            for (var i = 0; i < _labels.Length; i++)
                if (_labels[i] != null) _labels[i].style.display = DisplayStyle.None;
            _driver.Stop();
        }

        internal bool Step(float deltaSeconds)
        {
            var alive = _model.Tick(deltaSeconds);
            for (var i = 0; i < _labels.Length; i++)
            {
                var label = _labels[i];
                if (label == null) continue;
                if (_model.Get(i).Active) Apply(i);
                else label.style.display = DisplayStyle.None;
            }

            return alive;
        }

        private void Apply(int slot)
        {
            ref readonly var item = ref _model.Get(slot);
            var label = _labels[slot];
            label.style.translate = new StyleTranslate(new Translate(item.X, item.Y));
            label.style.opacity = item.Alpha;
            label.style.scale = new StyleScale(new Scale(new Vector3(item.Scale, item.Scale, 1f)));
        }
    }
}
