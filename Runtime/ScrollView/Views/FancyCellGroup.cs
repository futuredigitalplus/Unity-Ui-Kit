/*
 * FancyScrollView (https://github.com/setchi/FancyScrollView)
 * Copyright (c) 2020 setchi
 * Licensed under MIT (https://github.com/setchi/FancyScrollView/blob/master/LICENSE)
 */

// FutureDigitalPlus: from Sources/Runtime/GridView/FancyCellGroup.cs. The group is a UI Toolkit element that fills the view and lets
// pointer input through; it creates its cells with the context's CreateCell factory instead of instantiating a template GameObject, and
// says which type was wrong when the factory returns another kind of element. Namespace FutureDigitalPlus.UIKit.ScrollView; doc comments translated.

using System;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.ScrollView
{
    /// <summary>
    /// Base class of a cell that holds several cells, one row (or column) of a <see cref="FancyGridView{TItemData, TContext}"/>.
    /// The group fills the view and ignores the pointer; each of its cells places itself.
    /// </summary>
    /// <typeparam name="TItemData">The item data type.</typeparam>
    /// <typeparam name="TContext">The context type.</typeparam>
    public abstract class FancyCellGroup<TItemData, TContext> : FancyCell<TItemData[], TContext>
        where TContext : class, IFancyCellGroupContext, new()
    {
        /// <summary>USS class of a cell group.</summary>
        public const string GroupUssClassName = "fdp-fancy-cell-group";

        /// <summary>Makes the group fill the view and let the pointer through to its cells.</summary>
        protected FancyCellGroup()
        {
            AddToClassList(GroupUssClassName);
            pickingMode = PickingMode.Ignore;
            style.left = 0;
            style.top = 0;
            style.right = 0;
            style.bottom = 0;
        }

        /// <summary>The cells of this group.</summary>
        protected virtual FancyCell<TItemData, TContext>[] Cells { get; private set; }

        /// <summary>Creates the cells of this group with the context's <see cref="IFancyCellGroupContext.CreateCell"/> and adds them.</summary>
        /// <returns>The cells of this group.</returns>
        protected virtual FancyCell<TItemData, TContext>[] InstantiateCells()
        {
            var cells = new FancyCell<TItemData, TContext>[Context.GetGroupCount()];
            for (var i = 0; i < cells.Length; i++)
            {
                var element = Context.CreateCell();
                cells[i] = element as FancyCell<TItemData, TContext> ?? throw new InvalidOperationException(string.Format(
                    "The grid's cell factory returned {0}; it must return a FancyCell<{1}, {2}>.",
                    element == null ? "null" : element.GetType().FullName, typeof(TItemData).FullName, typeof(TContext).FullName));
                cells[i].style.position = Position.Absolute;
                Add(cells[i]);
            }

            return cells;
        }

        /// <inheritdoc/>
        public override void Initialize()
        {
            Cells = InstantiateCells();

            for (var i = 0; i < Cells.Length; i++)
            {
                Cells[i].SetContext(Context);
                Cells[i].Initialize();
            }
        }

        /// <inheritdoc/>
        public override void UpdateContent(TItemData[] contents)
        {
            var firstCellIndex = Index * Context.GetGroupCount();

            for (var i = 0; i < Cells.Length; i++)
            {
                Cells[i].Index = i + firstCellIndex;
                Cells[i].SetVisible(i < contents.Length);

                if (Cells[i].IsVisible)
                {
                    Cells[i].UpdateContent(contents[i]);
                }
            }
        }

        /// <inheritdoc/>
        public override void UpdatePosition(float position)
        {
            for (var i = 0; i < Cells.Length; i++)
            {
                Cells[i].UpdatePosition(position);
            }
        }
    }
}
