using Ixen.Core.Visual.Styles.Descriptors;
using System.Collections.Generic;

namespace Ixen.Core.Visual.Computers
{
    internal class ArrangeComputer
    {
        private readonly List<VisualElement> _anchored = new();
        private VisualElement _root;
        private float _viewportWidth;
        private float _viewportHeight;

        internal void Arrange(VisualElement element, float x, float y, float viewportWidth, float viewportHeight)
        {
            _root = element;
            _viewportWidth = viewportWidth;
            _viewportHeight = viewportHeight;
            _anchored.Clear();

            Arrange(element, x, y);

            ResolveAnchored();

            _anchored.Clear();
            _root = null;
        }

        private void ResolveAnchored()
        {
            for (int i = 0; i < _anchored.Count; i++)
            {
                VisualElement layer = _anchored[i];
                VisualElement anchor = layer.AnchorElement
                    ?? _root?.FindByName(layer.StylesHandlers.Anchor.Descriptor.Name);

                float anchorX;
                float anchorY;
                float anchorWidth;
                float anchorHeight;

                if (anchor != null && anchor != layer)
                {
                    anchorX = anchor.X;
                    anchorY = anchor.Y;
                    anchorWidth = anchor.ActualWidth;
                    anchorHeight = anchor.ActualHeight;
                }
                else if (layer.HasAnchorPoint)
                {
                    anchorX = layer.AnchorPointX;
                    anchorY = layer.AnchorPointY;
                    anchorWidth = 0;
                    anchorHeight = 0;
                }
                else
                {
                    ArrangePlaced(layer, 0, 0);
                    continue;
                }

                AnchorPlacementStyleDescriptor placement = layer.StylesHandlers.AnchorPlacement.Descriptor;

                ArrangePlaced(layer,
                    AnchorOriginX(layer, anchorX, anchorWidth, placement),
                    AnchorOriginY(layer, anchorY, anchorHeight, placement));
            }
        }

        private float AnchorOriginX(VisualElement layer, float anchorX, float anchorWidth,
            AnchorPlacementStyleDescriptor placement)
        {
            float extent = PlacedExtentWidth(layer);
            float origin;

            if (placement.Side == AnchorSide.Left)
            {
                origin = anchorX - extent;

                if (!placement.NoFlip && origin < 0 && anchorX + anchorWidth + extent <= _viewportWidth)
                {
                    origin = anchorX + anchorWidth;
                }
            }
            else if (placement.Side == AnchorSide.Right)
            {
                origin = anchorX + anchorWidth;

                if (!placement.NoFlip && origin + extent > _viewportWidth && anchorX - extent >= 0)
                {
                    origin = anchorX - extent;
                }
            }
            else
            {
                origin = Aligned(anchorX, anchorWidth, extent, placement.Align);
            }

            return placement.NoFlip ? origin : Clamped(origin, extent, _viewportWidth);
        }

        private float AnchorOriginY(VisualElement layer, float anchorY, float anchorHeight,
            AnchorPlacementStyleDescriptor placement)
        {
            float extent = PlacedExtentHeight(layer);
            float origin;

            if (placement.Side == AnchorSide.Below)
            {
                origin = anchorY + anchorHeight;

                if (!placement.NoFlip && origin + extent > _viewportHeight && anchorY - extent >= 0)
                {
                    origin = anchorY - extent;
                }
            }
            else if (placement.Side == AnchorSide.Above)
            {
                origin = anchorY - extent;

                if (!placement.NoFlip && origin < 0 && anchorY + anchorHeight + extent <= _viewportHeight)
                {
                    origin = anchorY + anchorHeight;
                }
            }
            else
            {
                origin = Aligned(anchorY, anchorHeight, extent, placement.Align);
            }

            return placement.NoFlip ? origin : Clamped(origin, extent, _viewportHeight);
        }

        private static float Aligned(float anchorStart, float anchorSize, float extent, AnchorAlign align)
        {
            if (align == AnchorAlign.Center)
            {
                return anchorStart + (anchorSize - extent) / 2;
            }

            if (align == AnchorAlign.End)
            {
                return anchorStart + anchorSize - extent;
            }

            return anchorStart;
        }

        private static float Clamped(float origin, float extent, float viewport)
        {
            if (extent >= viewport)
            {
                return 0;
            }

            if (origin + extent > viewport)
            {
                origin = viewport - extent;
            }

            return origin < 0 ? 0 : origin;
        }

        private static float PlacedExtentWidth(VisualElement element)
        {
            float extent = 0;

            foreach (VisualElement child in element.Children)
            {
                float edge = child.LayoutOffsetX + child.BoxWidth;

                if (edge > extent)
                {
                    extent = edge;
                }
            }

            return extent;
        }

        private static float PlacedExtentHeight(VisualElement element)
        {
            float extent = 0;

            foreach (VisualElement child in element.Children)
            {
                float edge = child.LayoutOffsetY + child.BoxHeight;

                if (edge > extent)
                {
                    extent = edge;
                }
            }

            return extent;
        }

        internal void Arrange(VisualElement element, float x, float y)
        {
            element.SetPosition(x, y);

            ResetTextSlot(element);

            ArrangeChrome(element);

            LayoutType type = MeasureComputer.LayoutTypeOf(element);

            if (type == LayoutType.Grid)
            {
                ArrangeGrid(element);
                return;
            }

            if (type == LayoutType.Absolute || type == LayoutType.Dock)
            {
                ArrangePlaced(element, ContentOriginX(element), ContentOriginY(element));
                return;
            }

            if (type == LayoutType.Fixed)
            {
                if (element.IsAnchored)
                {
                    _anchored.Add(element);
                    return;
                }

                ArrangePlaced(element, 0, 0);
                return;
            }

            if (type != LayoutType.Row && type != LayoutType.Column)
            {
                return;
            }

            bool isRow = type == LayoutType.Row;
            float childX = ContentOriginX(element);
            float childY = ContentOriginY(element);
            float gap = MeasureComputer.GapOf(element, isRow);
            ContentAlignStyleDescriptor align = element.StylesHandlers.ContentAlign.Descriptor;
            bool aligned = align.IsDeclared;

            float slack = aligned || element.TextInFlow ? MainSlack(element, isRow, gap) : 0;

            if (element.TextInFlow)
            {
                float extent = element.TextExtent + (slack > 0 ? slack : 0);

                if (isRow)
                {
                    element.TextSlotX = childX;
                    element.TextSlotWidth = extent;
                    childX += extent + gap;
                }
                else
                {
                    element.TextSlotY = childY;
                    element.TextSlotHeight = extent;
                    childY += extent + gap;
                }
            }
            else if (aligned)
            {
                if (isRow)
                {
                    childX += Slide(slack, align.Horizontal);
                }
                else
                {
                    childY += Slide(slack, align.Vertical);
                }
            }

            foreach (VisualElement child in element.Children)
            {
                float atX = childX;
                float atY = childY;

                if (aligned)
                {
                    if (isRow)
                    {
                        atY += Slide(element.ContentHeight - child.BoxHeight, align.Vertical);
                    }
                    else
                    {
                        atX += Slide(element.ContentWidth - child.BoxWidth, align.Horizontal);
                    }
                }

                Arrange(child, atX, atY);

                if (isRow)
                {
                    childX += child.BoxWidth + gap;
                }
                else
                {
                    childY += child.BoxHeight + gap;
                }
            }
        }

        private static float MainSlack(VisualElement element, bool isRow, float gap)
        {
            float used = 0;
            int count = 0;

            foreach (VisualElement child in element.Children)
            {
                used += isRow ? child.BoxWidth : child.BoxHeight;
                count++;
            }

            if (element.TextInFlow)
            {
                used += element.TextExtent;
                count++;
            }

            if (count > 1)
            {
                used += gap * (count - 1);
            }

            return (isRow ? element.ContentWidth : element.ContentHeight) - used;
        }

        private static void ResetTextSlot(VisualElement element)
        {
            element.TextSlotX = element.X + element.PaddingLeft + element.BorderInsideLeft;
            element.TextSlotY = element.Y + element.PaddingTop + element.BorderInsideTop;
            element.TextSlotWidth = element.ContentWidth;
            element.TextSlotHeight = element.ContentHeight;
        }

        private static float Slide(float slack, ContentAlign align)
        {
            if (slack <= 0 || align == ContentAlign.Unset || align == ContentAlign.Left)
            {
                return 0;
            }

            return align == ContentAlign.Center ? slack / 2 : slack;
        }

        private static float Slide(float slack, ContentVAlign align)
        {
            if (slack <= 0 || align == ContentVAlign.Unset || align == ContentVAlign.Top)
            {
                return 0;
            }

            return align == ContentVAlign.Middle ? slack / 2 : slack;
        }

        private void ArrangeChrome(VisualElement element)
        {
            if (!element.HasChrome)
            {
                return;
            }

            foreach (VisualElement chrome in element.Chrome)
            {
                Arrange(chrome, element.X + chrome.LayoutOffsetX, element.Y + chrome.LayoutOffsetY);
            }
        }

        private void ArrangePlaced(VisualElement element, float originX, float originY)
        {
            foreach (VisualElement child in element.Children)
            {
                Arrange(child, originX + child.LayoutOffsetX, originY + child.LayoutOffsetY);
            }
        }

        private static float ContentOriginX(VisualElement element) => element.ContentX;

        private static float ContentOriginY(VisualElement element) => element.ContentY;

        private void ArrangeGrid(VisualElement element)
        {
            float[] columns = element.GridColumns;
            float[] rows = element.GridRows;

            if (columns == null || rows == null || columns.Length == 0 || rows.Length == 0)
            {
                return;
            }

            float originX = ContentOriginX(element);
            float originY = ContentOriginY(element);
            float columnGap = MeasureComputer.GapOf(element, true);
            float rowGap = MeasureComputer.GapOf(element, false);
            ContentAlignStyleDescriptor align = element.StylesHandlers.ContentAlign.Descriptor;

            foreach (VisualElement child in element.Children)
            {
                if (child.GridColumn >= columns.Length || child.GridRow >= rows.Length)
                {
                    continue;
                }

                float cellX = originX + Offset(columns, child.GridColumn, columnGap);
                float cellY = originY + Offset(rows, child.GridRow, rowGap);

                if (align.IsDeclared)
                {
                    cellX += Slide(MeasureComputer.Extent(columns, child.GridColumn, child.GridColumnSpan, columnGap)
                        - child.BoxWidth, align.Horizontal);

                    cellY += Slide(MeasureComputer.Extent(rows, child.GridRow, child.GridRowSpan, rowGap)
                        - child.BoxHeight, align.Vertical);
                }

                Arrange(child, cellX, cellY);
            }
        }


        private static float Offset(float[] tracks, int index, float gap)
        {
            float offset = 0;
            int counted = 0;

            for (int i = 0; i < index && i < tracks.Length; i++)
            {
                offset += tracks[i];
                counted++;
            }

            return offset + gap * counted;
        }
    }
}
