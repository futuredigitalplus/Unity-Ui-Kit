/*
 * FancyScrollView (https://github.com/setchi/FancyScrollView)
 * Copyright (c) 2020 setchi
 * Licensed under MIT (https://github.com/setchi/FancyScrollView/blob/master/LICENSE)
 */

// FutureDigitalPlus: from Sources/Runtime/Core/FancyCell.cs. A UI Toolkit VisualElement instead of a MonoBehaviour: visibility is
// style.display instead of GameObject.SetActive, and the view creates cells with CreateCell instead of instantiating a prefab.
// Namespace FutureDigitalPlus.UIKit.ScrollView; doc comments translated to English.

using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.ScrollView
{
    /// <summary>
    /// Base class of the cells of a <see cref="FancyScrollView{TItemData, TContext}"/>. The view positions nothing itself: it calls
    /// <see cref="UpdatePosition"/> with the cell's place in the viewport (0..1) and the cell moves, scales or fades itself
    /// (<c>style.translate</c>, <c>style.scale</c>, <c>style.opacity</c>). Cells are absolutely positioned in the view's cell container.
    /// Use <see cref="FancyCell{TItemData}"/> when no <see cref="Context"/> is needed.
    /// </summary>
    /// <typeparam name="TItemData">The item data type.</typeparam>
    /// <typeparam name="TContext">The type of <see cref="Context"/>.</typeparam>
    public abstract class FancyCell<TItemData, TContext> : VisualElement where TContext : class, new()
    {
        /// <summary>USS class of every cell.</summary>
        public const string UssClassName = "fdp-fancy-cell";

        /// <summary>Adds <see cref="UssClassName"/>.</summary>
        protected FancyCell()
        {
            AddToClassList(UssClassName);
        }

        /// <summary>The index of the item this cell shows (-1 before the first).</summary>
        public int Index { get; set; } = -1;

        /// <summary>True when the cell is shown.</summary>
        public virtual bool IsVisible => style.display.value != DisplayStyle.None;

        /// <summary>
        /// The view's <see cref="FancyScrollView{TItemData, TContext}.Context"/>: one instance shared by the view and all its cells,
        /// to pass information and keep state (the selected index, a click callback).
        /// </summary>
        protected TContext Context { get; private set; }

        /// <summary>Sets <see cref="Context"/>. Called once, before <see cref="Initialize"/>.</summary>
        /// <param name="context">The shared context.</param>
        public virtual void SetContext(TContext context) => Context = context;

        /// <summary>Called once after the cell is created and has its context.</summary>
        public virtual void Initialize() { }

        /// <summary>Shows or hides the cell.</summary>
        /// <param name="visible">True to show.</param>
        public virtual void SetVisible(bool visible) => style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;

        /// <summary>Shows <paramref name="itemData"/>. Called when the cell gets a new item or the view refreshes.</summary>
        /// <param name="itemData">The item.</param>
        public abstract void UpdateContent(TItemData itemData);

        /// <summary>Places the cell for its position in the viewport. Called every time the scroll position changes.</summary>
        /// <param name="position">The cell's place in the viewport, normalized: 0 at the start, 1 at the end.</param>
        public abstract void UpdatePosition(float position);
    }

    /// <summary>Base class of the cells of a <see cref="FancyScrollView{TItemData}"/>, a view without a context.</summary>
    /// <typeparam name="TItemData">The item data type.</typeparam>
    /// <seealso cref="FancyCell{TItemData, TContext}"/>
    public abstract class FancyCell<TItemData> : FancyCell<TItemData, NullContext>
    {
        /// <inheritdoc/>
        public sealed override void SetContext(NullContext context) => base.SetContext(context);
    }

    /// <summary>The context of a view that needs none (<see cref="FancyScrollView{TItemData}"/>).</summary>
    public sealed class NullContext { }
}
