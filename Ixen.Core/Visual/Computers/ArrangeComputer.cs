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
                VisualElement anchor = _root?.FindByName(layer.StylesHandlers.Anchor.Descriptor.Name);

                if (anchor == null || anchor == layer)
                {
                    ArrangePlaced(layer, 0, 0);
                    continue;
                }

                AnchorPlacementStyleDescriptor placement = layer.StylesHandlers.AnchorPlacement.Descriptor;

                ArrangePlaced(layer,
                    AnchorOriginX(layer, anchor, placement),
                    AnchorOriginY(layer, anchor, placement));
            }
        }

        private float AnchorOriginX(VisualElement layer, VisualElement anchor,
            AnchorPlacementStyleDescriptor placement)
        {
            float extent = PlacedExtentWidth(layer);
            float origin;

            if (placement.Side == AnchorSide.Left)
            {
                origin = anchor.X - extent;

                if (!placement.NoFlip && origin < 0 && anchor.X + anchor.ActualWidth + extent <= _viewportWidth)
                {
                    origin = anchor.X + anchor.ActualWidth;
                }
            }
            else if (placement.Side == AnchorSide.Right)
            {
                origin = anchor.X + anchor.ActualWidth;

                if (!placement.NoFlip && origin + extent > _viewportWidth && anchor.X - extent >= 0)
                {
                    origin = anchor.X - extent;
                }
            }
            else
            {
                origin = Aligned(anchor.X, anchor.ActualWidth, extent, placement.Align);
            }

            return placement.NoFlip ? origin : Clamped(origin, extent, _viewportWidth);
        }

        private float AnchorOriginY(VisualElement layer, VisualElement anchor,
            AnchorPlacementStyleDescriptor placement)
        {
            float extent = PlacedExtentHeight(layer);
            float origin;

            if (placement.Side == AnchorSide.Below)
            {
                origin = anchor.Y + anchor.ActualHeight;

                if (!placement.NoFlip && origin + extent > _viewportHeight && anchor.Y - extent >= 0)
                {
                    origin = anchor.Y - extent;
                }
            }
            else if (placement.Side == AnchorSide.Above)
            {
                origin = anchor.Y - extent;

                if (!placement.NoFlip && origin < 0 && anchor.Y + anchor.ActualHeight + extent <= _viewportHeight)
                {
                    origin = anchor.Y + anchor.ActualHeight;
                }
            }
            else
            {
                origin = Aligned(anchor.Y, anchor.ActualHeight, extent, placement.Align);
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

            foreach (VisualElement child in element.Children)
            {
                Arrange(child, childX, childY);

                if (isRow)
                {
                    childX += child.BoxWidth;
                }
                else
                {
                    childY += child.BoxHeight;
                }
            }
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

        private static float ContentOriginX(VisualElement element)
            => element.X + element.PaddingLeft + element.BorderInsideLeft - element.ScrollX;

        private static float ContentOriginY(VisualElement element)
            => element.Y + element.PaddingTop + element.BorderInsideTop - element.ScrollY;

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

            foreach (VisualElement child in element.Children)
            {
                if (child.GridColumn >= columns.Length || child.GridRow >= rows.Length)
                {
                    continue;
                }

                Arrange(child,
                    originX + Offset(columns, child.GridColumn),
                    originY + Offset(rows, child.GridRow));
            }
        }

        private static float Offset(float[] tracks, int index)
        {
            float offset = 0;

            for (int i = 0; i < index && i < tracks.Length; i++)
            {
                offset += tracks[i];
            }

            return offset;
        }
    }
}
