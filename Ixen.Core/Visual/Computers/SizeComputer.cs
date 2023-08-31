using Ixen.Core.Visual.Styles.Descriptors;
using System;

namespace Ixen.Core.Visual.Computers
{
    internal class SizeComputer
    {
        internal void Compute(VisualElement element)
        {
            var widthContainerElement = GetWidthContainerElement(element);
            var heightContainerElement = GetHeightContainerElement(element);

            float computedWidth;
            float computedHeight;
            float remainingWidth = widthContainerElement.ActualWidth;
            float remainingHeight = heightContainerElement.ActualHeight;

            var layoutStyle = element.StylesHandlers.Layout.Descriptor;

            if (!element.IsTotalWeightSet)
            {
                ComputeTotalWeight(element, layoutStyle);
            }

            foreach (VisualElement child in element.Children)
            {
                computedWidth = ComputeWidth(widthContainerElement, child, remainingWidth);
                computedHeight = ComputeHeight(heightContainerElement, child, remainingHeight);

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

            if (element.TotalWidthWeight == 0 && element.TotalHeightWeight == 0)
            {
                foreach (VisualElement child in element.Children)
                {
                    Compute(child);
                }

                return;
            }

            foreach (VisualElement child in element.Children)
            {
                if (element.TotalWidthWeight > 0)
                {
                    ComputeFilledWidth(widthContainerElement, child, remainingWidth);
                }

                if (element.TotalHeightWeight > 0)
                {
                    ComputeFilledHeight(heightContainerElement, child, remainingHeight);
                }

                Compute(child);
            }
        }

        private void ComputeTotalWeight(VisualElement element, LayoutStyleDescriptor layout)
        {
            element.TotalWidthWeight = 0;
            element.TotalHeightWeight = 0;

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
            
            element.IsTotalWeightSet = true;
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

        private VisualElement GetWidthContainerElement(VisualElement element)
        {
            while (!element.IsWidthComputed)
            {
                element = element.Parent;
            }

            return element;
        }

        private float ComputeWidth(VisualElement element, VisualElement child, float remainingWidth)
        {
            SizeStyleDescriptor sizeStyle = GetSizeStyleDescriptor(child, SizeStyleDescriptorType.Width);
            float width = 0;
            bool computed = false;

            switch (sizeStyle.Unit)
            {
                case SizeUnit.Pixels:
                    ComputeHorizontalMargin(element, child);
                    ComputeHorizontalPadding(element, child);
                    width = sizeStyle.Value;
                    computed = true;
                    break;

                case SizeUnit.Percents:
                    ComputeHorizontalMargin(element, child);
                    ComputeHorizontalPadding(element, child);
                    width = (element.ActualWidth / 100) * sizeStyle.Value;
                    computed = true;
                    break;
            }

            child.Width = Math.Max(0, Math.Min(width, remainingWidth));
            child.IsWidthComputed = computed;

            return child.BoxWidth;
        }

        private float ComputeFilledWidth(VisualElement element, VisualElement child, float remainingWidth)
        {
            SizeStyleDescriptor sizeStyle = GetSizeStyleDescriptor(child, SizeStyleDescriptorType.Width);

            if (sizeStyle.Unit == SizeUnit.Weight || sizeStyle.Unit == SizeUnit.Unset)
            {
                float margin = ComputeHorizontalMargin(element, child);
                child.Width = ((remainingWidth - margin) / element.TotalWidthWeight) * sizeStyle.Value;
                child.IsWidthComputed = true;

                return child.BoxWidth;
            }

            return 0;
        }

        private float ComputeHorizontalMargin(VisualElement element, VisualElement child)
        {
            MarginStyleDescriptor marginStyle = child.StylesHandlers.Margin.Descriptor;

            switch (marginStyle.Left.Unit)
            {
                case SizeUnit.Pixels:
                    child.MarginLeft = marginStyle.Left.Value;
                    break;
                case SizeUnit.Percents:
                    child.MarginLeft = (element.ActualWidth / 100) * marginStyle.Left.Value;
                    break;
            }

            switch (marginStyle.Right.Unit)
            {
                case SizeUnit.Pixels:
                    child.MarginRight = marginStyle.Right.Value;
                    break;
                case SizeUnit.Percents:
                    child.MarginRight = (element.ActualWidth / 100) * marginStyle.Right.Value;
                    break;
            }

            return child.HorizontalMargin;
        }

        private float ComputeHorizontalPadding(VisualElement element, VisualElement child)
        {
            MarginStyleDescriptor paddingStyle = child.StylesHandlers.Padding.Descriptor;

            switch (paddingStyle.Left.Unit)
            {
                case SizeUnit.Pixels:
                    child.PaddingLeft = paddingStyle.Left.Value;
                    break;
                case SizeUnit.Percents:
                    child.PaddingLeft = (element.ActualWidth / 100) * paddingStyle.Left.Value;
                    break;
            }

            switch (paddingStyle.Right.Unit)
            {
                case SizeUnit.Pixels:
                    child.PaddingRight = paddingStyle.Right.Value;
                    break;
                case SizeUnit.Percents:
                    child.PaddingRight = (element.ActualWidth / 100) * paddingStyle.Right.Value;
                    break;
            }

            return child.HorizontalPadding;
        }

        private VisualElement GetHeightContainerElement(VisualElement element)
        {
            while (!element.IsHeightComputed)
            {
                element = element.Parent;
            }

            return element;
        }

        private float ComputeHeight(VisualElement element, VisualElement child, float remainingHeight)
        {
            SizeStyleDescriptor sizeStyle = GetSizeStyleDescriptor(child, SizeStyleDescriptorType.Height);
            float height = 0;
            bool computed = false;

            switch (sizeStyle.Unit)
            {
                case SizeUnit.Pixels:
                    ComputeVerticalMargin(element, child);
                    ComputeVerticalPadding(element, child);
                    height = sizeStyle.Value;
                    computed = true;
                    break;

                case SizeUnit.Percents:
                    ComputeVerticalMargin(element, child);
                    ComputeVerticalPadding(element, child);
                    height = (element.ActualHeight / 100) * sizeStyle.Value;
                    computed = true;
                    break;
            }

            child.Height = Math.Max(0, Math.Min(height, remainingHeight));
            child.IsHeightComputed = computed;

            return child.BoxHeight;
        }

        private float ComputeFilledHeight(VisualElement element, VisualElement child, float remainingHeight)
        {
            SizeStyleDescriptor heightStyle = GetSizeStyleDescriptor(child, SizeStyleDescriptorType.Height);

            if (heightStyle.Unit == SizeUnit.Weight || heightStyle.Unit == SizeUnit.Unset)
            {
                float margin = ComputeVerticalMargin(element, child);
                child.Height = ((remainingHeight - margin) / element.TotalHeightWeight) * heightStyle.Value;
                child.IsHeightComputed = true;

                return child.BoxHeight;
            }

            return 0;
        }

        private float ComputeVerticalMargin(VisualElement element, VisualElement child)
        {
            MarginStyleDescriptor marginStyle = child.StylesHandlers.Margin.Descriptor;

            switch (marginStyle.Top.Unit)
            {
                case SizeUnit.Pixels:
                    child.MarginTop = marginStyle.Top.Value;
                    break;
                case SizeUnit.Percents:
                    child.MarginTop = (element.ActualHeight / 100) * marginStyle.Top.Value;
                    break;
            }

            switch (marginStyle.Bottom.Unit)
            {
                case SizeUnit.Pixels:
                    child.MarginBottom = marginStyle.Bottom.Value;
                    break;
                case SizeUnit.Percents:
                    child.MarginBottom = (element.ActualHeight / 100) * marginStyle.Bottom.Value;
                    break;
            }

            return child.VerticalMargin;
        }

        private float ComputeVerticalPadding(VisualElement element, VisualElement child)
        {
            MarginStyleDescriptor paddingStyle = child.StylesHandlers.Padding.Descriptor;

            switch (paddingStyle.Top.Unit)
            {
                case SizeUnit.Pixels:
                    child.PaddingTop = paddingStyle.Top.Value;
                    break;
                case SizeUnit.Percents:
                    child.PaddingTop = (element.ActualHeight / 100) * paddingStyle.Top.Value;
                    break;
            }

            switch (paddingStyle.Bottom.Unit)
            {
                case SizeUnit.Pixels:
                    child.PaddingBottom = paddingStyle.Bottom.Value;
                    break;
                case SizeUnit.Percents:
                    child.PaddingBottom = (element.ActualHeight / 100) * paddingStyle.Bottom.Value;
                    break;
            }

            return child.VerticalPadding;
        }
    }
}
