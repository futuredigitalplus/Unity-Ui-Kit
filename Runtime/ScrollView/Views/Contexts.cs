/*
 * FancyScrollView (https://github.com/setchi/FancyScrollView)
 * Copyright (c) 2020 setchi
 * Licensed under MIT (https://github.com/setchi/FancyScrollView/blob/master/LICENSE)
 */

// FutureDigitalPlus: the five context files of Sources/Runtime/ScrollRect and Sources/Runtime/GridView in one file. Additions for
// UI Toolkit, where a cell places itself from the viewport's top-left corner instead of its centre: IFancyScrollRectContext.GetViewportSize
// and GetScrollAxisCellSize. IFancyCellGroupContext.CellTemplate (a GameObject to instantiate) became CreateCell (a factory).

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace FutureDigitalPlus.UIKit.ScrollView
{
    /// <summary>The context a <see cref="FancyScrollRect{TItemData, TContext}"/> shares with its cells.</summary>
    public interface IFancyScrollRectContext
    {
        /// <summary>The scroll axis.</summary>
        ScrollDirection ScrollDirection { get; set; }

        /// <summary>Length of the scroll range (viewport, one cell and both reuse margins) and of one reuse margin, in panel units.</summary>
        Func<(float ScrollSize, float ReuseMargin)> CalculateScrollSize { get; set; }

        /// <summary>The viewport's size in panel units.</summary>
        Func<Vector2> GetViewportSize { get; set; }

        /// <summary>The cell length along the scroll axis in panel units (the view's <c>CellSize</c>).</summary>
        Func<float> GetScrollAxisCellSize { get; set; }
    }

    /// <summary>The default context of a <see cref="FancyScrollRect{TItemData, TContext}"/>; derive from it to add your own state.</summary>
    public class FancyScrollRectContext : IFancyScrollRectContext
    {
        ScrollDirection IFancyScrollRectContext.ScrollDirection { get; set; }
        Func<(float ScrollSize, float ReuseMargin)> IFancyScrollRectContext.CalculateScrollSize { get; set; }
        Func<Vector2> IFancyScrollRectContext.GetViewportSize { get; set; }
        Func<float> IFancyScrollRectContext.GetScrollAxisCellSize { get; set; }
    }

    /// <summary>The context a <see cref="FancyCellGroup{TItemData, TContext}"/> shares with its cells.</summary>
    public interface IFancyCellGroupContext
    {
        /// <summary>Creates one cell of a group (it must be a <c>FancyCell&lt;TItemData, TContext&gt;</c>).</summary>
        Func<VisualElement> CreateCell { get; set; }

        /// <summary>The number of cells in a group.</summary>
        Func<int> GetGroupCount { get; set; }
    }

    /// <summary>The context a <see cref="FancyGridView{TItemData, TContext}"/> shares with its cells.</summary>
    public interface IFancyGridViewContext : IFancyScrollRectContext, IFancyCellGroupContext
    {
        /// <summary>Gap between cells across the scroll axis (the view's <c>StartAxisSpacing</c>).</summary>
        Func<float> GetStartAxisSpacing { get; set; }

        /// <summary>The cell length across the scroll axis.</summary>
        Func<float> GetCellSize { get; set; }
    }

    /// <summary>The default context of a <see cref="FancyGridView{TItemData, TContext}"/>; derive from it to add your own state.</summary>
    public class FancyGridViewContext : IFancyGridViewContext
    {
        ScrollDirection IFancyScrollRectContext.ScrollDirection { get; set; }
        Func<(float ScrollSize, float ReuseMargin)> IFancyScrollRectContext.CalculateScrollSize { get; set; }
        Func<Vector2> IFancyScrollRectContext.GetViewportSize { get; set; }
        Func<float> IFancyScrollRectContext.GetScrollAxisCellSize { get; set; }
        Func<VisualElement> IFancyCellGroupContext.CreateCell { get; set; }
        Func<int> IFancyCellGroupContext.GetGroupCount { get; set; }
        Func<float> IFancyGridViewContext.GetStartAxisSpacing { get; set; }
        Func<float> IFancyGridViewContext.GetCellSize { get; set; }
    }
}
