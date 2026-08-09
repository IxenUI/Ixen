using Ixen.Core.Visual.Styles.Descriptors;
using System;

namespace Ixen.Core.Visual.Computers
{
    internal class MeasureComputer
    {
        internal void Measure(VisualElement element, float availableWidth, float availableHeight, bool widthIsDefinite, bool heightIsDefinite)
        {
            float contentWidth = Math.Max(0, availableWidth - element.HorizontalPadding);
            float contentHeight = Math.Max(0, availableHeight - element.VerticalPadding);

            MeasureChildren(element, contentWidth, contentHeight);

            element.Width = widthIsDefinite
                ? availableWidth
                : AggregateWidth(element) + element.HorizontalPadding;

            element.Height = heightIsDefinite
                ? availableHeight
                : AggregateHeight(element) + element.VerticalPadding;
        }

        private void MeasureChildren(VisualElement element, float contentWidth, float contentHeight)
        {
            if (element.Children.Count == 0)
            {
                return;
            }

            bool isRow = IsRow(element);

            foreach (VisualElement child in element.Children)
            {
                ResolveHorizontalSpacing(child, contentWidth);
                ResolveVerticalSpacing(child, contentHeight);
            }

            float pool = isRow ? contentWidth : contentHeight;
            float totalWeight = 0;

            foreach (VisualElement child in element.Children)
            {
                SizeStyleDescriptor mainStyle = GetSizeStyleDescriptor(child, isRow
                    ? SizeStyleDescriptorType.Width
                    : SizeStyleDescriptorType.Height);

                if (IsFilling(mainStyle))
                {
                    totalWeight += mainStyle.Value;
                    pool -= isRow ? child.HorizontalMargin : child.VerticalMargin;
                    continue;
                }

                MeasureChild(child, contentWidth, contentHeight, isRow, 0, false);
                pool -= isRow ? child.BoxWidth : child.BoxHeight;
            }

            if (totalWeight <= 0)
            {
                return;
            }

            pool = Math.Max(0, pool);

            foreach (VisualElement child in element.Children)
            {
                SizeStyleDescriptor mainStyle = GetSizeStyleDescriptor(child, isRow
                    ? SizeStyleDescriptorType.Width
                    : SizeStyleDescriptorType.Height);

                if (!IsFilling(mainStyle))
                {
                    continue;
                }

                float share = (pool / totalWeight) * mainStyle.Value;
                MeasureChild(child, contentWidth, contentHeight, isRow, share, true);
            }
        }

        private void MeasureChild(VisualElement child, float contentWidth, float contentHeight, bool isRow, float mainShare, bool useMainShare)
        {
            SizeStyleDescriptor widthStyle = GetSizeStyleDescriptor(child, SizeStyleDescriptorType.Width);
            SizeStyleDescriptor heightStyle = GetSizeStyleDescriptor(child, SizeStyleDescriptorType.Height);

            ResolveAxis(widthStyle, contentWidth, child.HorizontalMargin, useMainShare && isRow, mainShare,
                out float availableWidth, out bool widthIsDefinite);

            ResolveAxis(heightStyle, contentHeight, child.VerticalMargin, useMainShare && !isRow, mainShare,
                out float availableHeight, out bool heightIsDefinite);

            Measure(child, availableWidth, availableHeight, widthIsDefinite, heightIsDefinite);
        }

        private void ResolveAxis(SizeStyleDescriptor style, float contentAvailable, float margin, bool useShare, float share,
            out float available, out bool isDefinite)
        {
            if (useShare)
            {
                available = share;
                isDefinite = true;
                return;
            }

            switch (style.Unit)
            {
                case SizeUnit.Pixels:
                    available = style.Value;
                    isDefinite = true;
                    return;

                case SizeUnit.Percents:
                    available = (contentAvailable / 100) * style.Value;
                    isDefinite = true;
                    return;

                case SizeUnit.Content:
                    available = Math.Max(0, contentAvailable - margin);
                    isDefinite = false;
                    return;

                default:
                    available = Math.Max(0, contentAvailable - margin) * style.Value;
                    isDefinite = true;
                    return;
            }
        }

        private float AggregateWidth(VisualElement element)
        {
            float total = 0;
            bool isRow = IsRow(element);

            foreach (VisualElement child in element.Children)
            {
                if (isRow)
                {
                    total += child.BoxWidth;
                }
                else if (child.BoxWidth > total)
                {
                    total = child.BoxWidth;
                }
            }

            return total;
        }

        private float AggregateHeight(VisualElement element)
        {
            float total = 0;
            bool isRow = IsRow(element);

            foreach (VisualElement child in element.Children)
            {
                if (!isRow)
                {
                    total += child.BoxHeight;
                }
                else if (child.BoxHeight > total)
                {
                    total = child.BoxHeight;
                }
            }

            return total;
        }

        private static bool IsRow(VisualElement element)
        {
            LayoutStyleDescriptor layoutStyle = element.StylesHandlers.Layout.Descriptor;
            return layoutStyle != null && layoutStyle.Type == LayoutType.Row;
        }

        private static bool IsFilling(SizeStyleDescriptor style)
            => style.Unit == SizeUnit.Weight || style.Unit == SizeUnit.Unset;

        private SizeStyleDescriptor GetSizeStyleDescriptor(VisualElement element, SizeStyleDescriptorType sizeType)
        {
            // Direct size style has priority
            SizeStyleDescriptor sizeStyle = (sizeType == SizeStyleDescriptorType.Width)
                ? element.StylesHandlers.Width.Descriptor
                : element.StylesHandlers.Height.Descriptor;

            // Get the templated size if any
            if (sizeStyle.Unit == SizeUnit.Unset && element.Parent != null)
            {
                LayoutStyleDescriptor layoutStyle = element.Parent.StylesHandlers.Layout.Descriptor;
                SizeTemplateStyleDescriptor sizeTemplateStyle = (sizeType == SizeStyleDescriptorType.Width)
                    ? element.Parent.StylesHandlers.RowTemplate.Descriptor
                    : element.Parent.StylesHandlers.ColumnTemplate.Descriptor;

                if (sizeTemplateStyle.Value.Count > 0
                    && (layoutStyle.Type == LayoutType.Grid
                    || (layoutStyle.Type == LayoutType.Column && sizeType == SizeStyleDescriptorType.Height)
                    || (layoutStyle.Type == LayoutType.Row && sizeType == SizeStyleDescriptorType.Width)))
                {
                    int index = element.ChildIndex % sizeTemplateStyle.Value.Count;
                    sizeStyle = sizeTemplateStyle.Value[index];
                }
            }

            return sizeStyle;
        }

        private void ResolveHorizontalSpacing(VisualElement element, float contentWidth)
        {
            MarginStyleDescriptor marginStyle = element.StylesHandlers.Margin.Descriptor;
            element.MarginLeft = ResolveSpace(marginStyle.Left, contentWidth);
            element.MarginRight = ResolveSpace(marginStyle.Right, contentWidth);

            MarginStyleDescriptor paddingStyle = element.StylesHandlers.Padding.Descriptor;
            element.PaddingLeft = ResolveSpace(paddingStyle.Left, contentWidth);
            element.PaddingRight = ResolveSpace(paddingStyle.Right, contentWidth);
        }

        private void ResolveVerticalSpacing(VisualElement element, float contentHeight)
        {
            MarginStyleDescriptor marginStyle = element.StylesHandlers.Margin.Descriptor;
            element.MarginTop = ResolveSpace(marginStyle.Top, contentHeight);
            element.MarginBottom = ResolveSpace(marginStyle.Bottom, contentHeight);

            MarginStyleDescriptor paddingStyle = element.StylesHandlers.Padding.Descriptor;
            element.PaddingTop = ResolveSpace(paddingStyle.Top, contentHeight);
            element.PaddingBottom = ResolveSpace(paddingStyle.Bottom, contentHeight);
        }

        private float ResolveSpace(SizeStyleDescriptor style, float contentAvailable)
        {
            switch (style.Unit)
            {
                case SizeUnit.Pixels:
                    return style.Value;

                case SizeUnit.Percents:
                    return (contentAvailable / 100) * style.Value;

                default:
                    return 0;
            }
        }
    }
}
