﻿using Ixen.Core.Visual.Styles.Descriptors;
using System;

namespace Ixen.Core.Visual.Computers
{
    internal class SizeComputer
    {
        internal void Compute(VisualElement element, DimensionalElement container)
        {
            float computedWidth;
            float computedHeight;
            float remainingWidth = element.Width;
            float remainingHeight = element.Height;

            var layoutStyle = element.StylesHandlers.Layout?.Descriptor;

            if (!element.TotalWeightSet)
            {
                ComputeTotalWeight(element, layoutStyle);
            }

            foreach (VisualElement child in element.Children)
            {
                computedWidth = ComputeWidth(element, child, container, remainingWidth);
                computedHeight = ComputeHeight(element, child, container, remainingHeight);

                if (layoutStyle != null)
                {
                    if (layoutStyle != null)
                    {
                        if (layoutStyle.Type == LayoutType.Column)
                        {
                            remainingHeight -= computedHeight;
                        }
                        else if (layoutStyle.Type == LayoutType.Row)
                        {
                            remainingWidth -= computedWidth;
                        }
                    }
                }
            }

            if (element.TotalWidthWeight == 0 && element.TotalHeightWeight == 0)
            {
                foreach (VisualElement child in element.Children)
                {
                    Compute(child, element);
                }

                return;
            }

            foreach (VisualElement child in element.Children)
            {
                if (element.TotalWidthWeight > 0)
                {
                    ComputeFilledWidth(element, child, container, remainingWidth);
                }

                if (element.TotalHeightWeight > 0)
                {
                    ComputeFilledHeight(element, child, container, remainingHeight);
                }

                Compute(child, element);
            }
        }

        private void ComputeTotalWeight(VisualElement element, LayoutStyleDescriptor layout)
        {
            element.TotalWidthWeight = 0;
            element.TotalHeightWeight = 0;

            if (layout != null)
            {
                if (layout.Type == LayoutType.Column)
                {
                    ComputeTotalHeightWeight(element);
                    element.TotalWidthWeight = 1;
                }
                else if (layout.Type == LayoutType.Row)
                {
                    ComputeTotalWidthWeight(element);
                    element.TotalHeightWeight = 1;
                }
            }
            else
            {
                ComputeTotalWidthWeight(element);
                ComputeTotalHeightWeight(element);
            }
            
            element.TotalWeightSet = true;
        }

        private void ComputeTotalWidthWeight(VisualElement element)
        {
            foreach (VisualElement child in element.Children)
            {
                if (child.StylesHandlers.Width?.Descriptor.Unit == SizeUnit.Weight)
                {
                    element.TotalWidthWeight += child.StylesHandlers.Width.Descriptor.Value;
                }
            }
        }

        private void ComputeTotalHeightWeight(VisualElement element)
        {
            foreach (VisualElement child in element.Children)
            {
                if (child.StylesHandlers.Height?.Descriptor.Unit == SizeUnit.Weight)
                {
                    element.TotalHeightWeight += child.StylesHandlers.Height.Descriptor.Value;
                }
            }
        }

        private float ComputeWidth(VisualElement element, VisualElement child, DimensionalElement container, float remainingWidth)
        {
            SizeStyleDescriptor widthStyle = child.StylesHandlers.Width?.Descriptor;
            MaskStyleDescriptor maskStyle = element.StylesHandlers.Mask?.Descriptor;
            float width = 0;

            if (widthStyle != null)
            {
                switch (widthStyle?.Unit)
                {
                    case SizeUnit.Pixels:
                        ComputeHorizontalMargin(element, child, container);
                        ComputeHorizontalPadding(element, child, container);
                        width = widthStyle.Value;
                        break;

                    case SizeUnit.Percents:
                        ComputeHorizontalMargin(element, child, container);
                        ComputeHorizontalPadding(element, child, container);
                        width = (container.Width / 100) * widthStyle.Value;
                        break;
                }

                child.Width = maskStyle.Right ? Math.Min(width, remainingWidth) : width;
                return child.BoxWidth;
            }

            return 0;
        }

        private float ComputeFilledWidth(VisualElement element, VisualElement child, DimensionalElement container, float remainingWidth)
        {
            SizeStyleDescriptor widthStyle = child.StylesHandlers.Width?.Descriptor;
            float margin;

            if (widthStyle != null)
            {
                switch (widthStyle.Unit)
                {
                    case SizeUnit.Weight:
                        margin = ComputeHorizontalMargin(element, child, container);
                        child.Width = ((remainingWidth - margin) / element.TotalWidthWeight) * widthStyle.Value;
                        return child.BoxWidth;
                }
            }

            return 0;
        }

        private float ComputeHorizontalMargin(VisualElement element, VisualElement child, DimensionalElement container)
        {
            MarginStyleDescriptor marginStyle = child.StylesHandlers.Margin?.Descriptor;

            if (marginStyle != null)
            {
                switch (marginStyle.Left.Unit)
                {
                    case SizeUnit.Pixels:
                        child.MarginLeft = marginStyle.Left.Value;
                        break;
                    case SizeUnit.Percents:
                        child.MarginLeft = (container.Width / 100) * marginStyle.Left.Value;
                        break;
                }

                switch (marginStyle.Right.Unit)
                {
                    case SizeUnit.Pixels:
                        child.MarginRight = marginStyle.Right.Value;
                        break;
                    case SizeUnit.Percents:
                        child.MarginRight = (container.Width / 100) * marginStyle.Right.Value;
                        break;
                }
            }

            return child.HorizontalMargin;
        }

        private float ComputeHorizontalPadding(VisualElement element, VisualElement child, DimensionalElement container)
        {
            MarginStyleDescriptor paddingStyle = child.StylesHandlers.Padding?.Descriptor;

            if (paddingStyle != null)
            {
                switch (paddingStyle.Left.Unit)
                {
                    case SizeUnit.Pixels:
                        child.PaddingLeft = paddingStyle.Left.Value;
                        break;
                    case SizeUnit.Percents:
                        child.PaddingLeft = (container.Width / 100) * paddingStyle.Left.Value;
                        break;
                }

                switch (paddingStyle.Right.Unit)
                {
                    case SizeUnit.Pixels:
                        child.PaddingRight = paddingStyle.Right.Value;
                        break;
                    case SizeUnit.Percents:
                        child.PaddingRight = (container.Width / 100) * paddingStyle.Right.Value;
                        break;
                }
            }

            return child.HorizontalPadding;
        }

        private float ComputeHeight(VisualElement element, VisualElement child, DimensionalElement container, float remainingHeight)
        {
            SizeStyleDescriptor heightStyle = child.StylesHandlers.Height?.Descriptor;
            MaskStyleDescriptor maskStyle = element.StylesHandlers.Mask?.Descriptor;
            float height = 0;

            if (heightStyle != null)
            {
                switch (heightStyle.Unit)
                {
                    case SizeUnit.Pixels:
                        ComputeVerticalMargin(element, child, container);
                        ComputeVerticalPadding(element, child, container);
                        height = heightStyle.Value;
                        break;

                    case SizeUnit.Percents:
                        ComputeVerticalMargin(element, child, container);
                        ComputeVerticalPadding(element, child, container);
                        height = (container.Height / 100) * heightStyle.Value;
                        break;
                }

                child.Height = maskStyle.Bottom ? Math.Min(height, remainingHeight) : height;
                return child.BoxHeight;
            }

            return 0;
        }

        private float ComputeFilledHeight(VisualElement element, VisualElement child, DimensionalElement container, float remainingHeight)
        {
            SizeStyleDescriptor heightStyle = child.StylesHandlers.Height?.Descriptor;
            float margin;

            if (heightStyle != null)
            {
                switch (heightStyle.Unit)
                {
                    case SizeUnit.Weight:
                        margin = ComputeVerticalMargin(element, child, container);
                        child.Height = ((remainingHeight - margin) / element.TotalHeightWeight) * heightStyle.Value;
                        return child.BoxHeight;
                }
            }

            return 0;
        }

        private float ComputeVerticalMargin(VisualElement element, VisualElement child, DimensionalElement container)
        {
            MarginStyleDescriptor marginStyle = child.StylesHandlers.Margin?.Descriptor;

            if (marginStyle != null)
            {
                switch (marginStyle.Top.Unit)
                {
                    case SizeUnit.Pixels:
                        child.MarginTop = marginStyle.Top.Value;
                        break;
                    case SizeUnit.Percents:
                        child.MarginTop = (container.Height / 100) * marginStyle.Top.Value;
                        break;
                }

                switch (marginStyle.Bottom.Unit)
                {
                    case SizeUnit.Pixels:
                        child.MarginBottom = marginStyle.Bottom.Value;
                        break;
                    case SizeUnit.Percents:
                        child.MarginBottom = (container.Height / 100) * marginStyle.Bottom.Value;
                        break;
                }
            }

            return child.VerticalMargin;
        }

        private float ComputeVerticalPadding(VisualElement element, VisualElement child, DimensionalElement container)
        {
            MarginStyleDescriptor paddingStyle = child.StylesHandlers.Padding?.Descriptor;

            if (paddingStyle != null)
            {
                switch (paddingStyle.Top.Unit)
                {
                    case SizeUnit.Pixels:
                        child.PaddingTop = paddingStyle.Top.Value;
                        break;
                    case SizeUnit.Percents:
                        child.PaddingTop = (container.Height / 100) * paddingStyle.Top.Value;
                        break;
                }

                switch (paddingStyle.Bottom.Unit)
                {
                    case SizeUnit.Pixels:
                        child.PaddingBottom = paddingStyle.Bottom.Value;
                        break;
                    case SizeUnit.Percents:
                        child.PaddingBottom = (container.Height / 100) * paddingStyle.Bottom.Value;
                        break;
                }
            }

            return child.VerticalPadding;
        }
    }
}
