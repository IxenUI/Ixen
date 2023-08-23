using Ixen.Core.Visual.Styles.Descriptors;
using System;

namespace Ixen.Core.Visual.Computers
{
    internal class SizeComputer
    {
        internal void Compute(VisualElement element, DimensionalElement container)
        {
            float computedWidth;
            float computedHeight;
            float remainingWidth = element.RenderWidth;
            float remainingHeight = element.RenderHeight;

            if (!element.Renderable)
            {
                return;
            }

            var layoutStyle = element.StylesHandlers.Layout.Descriptor;

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
            SizeStyleDescriptor sizeStyle;

            foreach (VisualElement child in element.Children)
            {
                sizeStyle = GetSizeStyleDescriptor(child, SizeStyleDescriptorType.Width);
                if (sizeStyle.Unit == SizeUnit.Weight || sizeStyle.Unit == SizeUnit.Unset)
                {
                    element.TotalWidthWeight += sizeStyle.Value;
                }
            }
        }

        private void ComputeTotalHeightWeight(VisualElement element)
        {
            SizeStyleDescriptor sizeStyle;

            foreach (VisualElement child in element.Children)
            {
                sizeStyle = GetSizeStyleDescriptor(child, SizeStyleDescriptorType.Height);
                if (sizeStyle.Unit == SizeUnit.Weight || sizeStyle.Unit == SizeUnit.Unset)
                {
                    element.TotalHeightWeight += sizeStyle.Value;
                }
            }
        }

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

        private float ComputeWidth(VisualElement element, VisualElement child, DimensionalElement container, float remainingWidth)
        {
            SizeStyleDescriptor sizeStyle = GetSizeStyleDescriptor(child, SizeStyleDescriptorType.Width);
            float width = 0;

            switch (sizeStyle.Unit)
            {
                case SizeUnit.Pixels:
                    ComputeHorizontalMargin(element, child, container);
                    ComputeHorizontalPadding(element, child, container);
                    width = sizeStyle.Value;
                    break;

                case SizeUnit.Percents:
                    ComputeHorizontalMargin(element, child, container);
                    ComputeHorizontalPadding(element, child, container);
                    width = (element.Width / 100) * sizeStyle.Value;
                    break;
            }

            child.Width = width;
            child.RenderWidth = Math.Max(0, Math.Min(child.ActualWidth, remainingWidth - child.MarginLeft));
            
            return child.BoxWidth;
        }

        private float ComputeFilledWidth(VisualElement element, VisualElement child, DimensionalElement container, float remainingWidth)
        {
            SizeStyleDescriptor widthStyle = GetSizeStyleDescriptor(child, SizeStyleDescriptorType.Width);

            if (widthStyle.Unit == SizeUnit.Weight || widthStyle.Unit == SizeUnit.Unset)
            {
                float margin = ComputeHorizontalMargin(element, child, container);
                child.Width = ((remainingWidth - margin) / element.TotalWidthWeight) * widthStyle.Value;
                child.RenderWidth = Math.Max(0, Math.Min(child.ActualWidth, remainingWidth - child.MarginLeft));

                return child.BoxWidth;
            }

            return 0;
        }

        private float ComputeHorizontalMargin(VisualElement element, VisualElement child, DimensionalElement container)
        {
            MarginStyleDescriptor marginStyle = child.StylesHandlers.Margin.Descriptor;

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

            return child.HorizontalMargin;
        }

        private float ComputeHorizontalPadding(VisualElement element, VisualElement child, DimensionalElement container)
        {
            MarginStyleDescriptor paddingStyle = child.StylesHandlers.Padding.Descriptor;

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

            return child.HorizontalPadding;
        }

        private float ComputeHeight(VisualElement element, VisualElement child, DimensionalElement container, float remainingHeight)
        {
            SizeStyleDescriptor heightStyle = GetSizeStyleDescriptor(child, SizeStyleDescriptorType.Height);
            float height = 0;

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
                    height = (element.Height / 100) * heightStyle.Value;
                    break;
            }

            child.Height = height;
            child.RenderHeight = Math.Max(0, Math.Min(child.ActualHeight, remainingHeight - child.MarginTop));

            return child.BoxHeight;
        }

        private float ComputeFilledHeight(VisualElement element, VisualElement child, DimensionalElement container, float remainingHeight)
        {
            SizeStyleDescriptor heightStyle = GetSizeStyleDescriptor(child, SizeStyleDescriptorType.Height);

            if (heightStyle.Unit == SizeUnit.Weight || heightStyle.Unit == SizeUnit.Unset)
            {
                float margin = ComputeVerticalMargin(element, child, container);
                child.Height = ((remainingHeight - margin) / element.TotalHeightWeight) * heightStyle.Value;
                child.RenderHeight = Math.Max(0, Math.Min(child.ActualHeight, remainingHeight - child.MarginTop));

                return child.BoxHeight;
            }

            return 0;
        }

        private float ComputeVerticalMargin(VisualElement element, VisualElement child, DimensionalElement container)
        {
            MarginStyleDescriptor marginStyle = child.StylesHandlers.Margin.Descriptor;

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

            return child.VerticalMargin;
        }

        private float ComputeVerticalPadding(VisualElement element, VisualElement child, DimensionalElement container)
        {
            MarginStyleDescriptor paddingStyle = child.StylesHandlers.Padding.Descriptor;

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

            return child.VerticalPadding;
        }
    }
}
