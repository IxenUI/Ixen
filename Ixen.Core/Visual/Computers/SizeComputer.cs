using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using System;

namespace Ixen.Core.Visual.Computers
{
    internal class SizeComputer
    {
        internal void Compute(VisualElement element, DimensionalElement container)
        {
            float usedWidth = 0;
            float usedHeight = 0;
            float computedWidth;
            float computedHeight;
            float remainingWidth = element.Width;
            float remainingHeight = element.Height;

            if (!element.TotalWeightSet)
            {
                GetTotalWeight(element);
            }

            foreach (VisualElement child in element.Children)
            {
                computedWidth = ComputeWidth(element, child, container, remainingWidth);
                computedHeight = ComputeHeight(element, child, container, remainingHeight);

                usedWidth += computedWidth;
                usedHeight += computedHeight;
                remainingWidth -= computedWidth;
                remainingHeight -= computedHeight;
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

        private void GetTotalWeight(VisualElement element)
        {
            element.TotalWidthWeight = 0;
            element.TotalHeightWeight = 0;

            foreach (VisualElement child in element.Children)
            {
                if (child.Styles.Width?.Unit == SizeUnit.Weight)
                {
                    element.TotalWidthWeight += child.Styles.Width.Value;
                }

                if (child.Styles.Height?.Unit == SizeUnit.Weight)
                {
                    element.TotalHeightWeight += child.Styles.Height.Value;
                }
            }

            element.TotalWeightSet = true;
        }

        private float ComputeWidth(VisualElement element, VisualElement child, DimensionalElement container, float remainingWidth)
        {
            SizeStyle widthStyle = child.Styles.Width;
            MaskStyle maskStyle = element.Styles.Mask;
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
            SizeStyle widthStyle = child.Styles.Width;
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
            MarginStyle marginStyle = child.Styles.Margin;
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
            MarginStyle paddingStyle = child.Styles.Padding;
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
            SizeStyle heightStyle = child.Styles.Height;
            MaskStyle maskStyle = element.Styles.Mask;
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
            SizeStyle heightStyle = child.Styles.Height;
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
            MarginStyle marginStyle = child.Styles.Margin;
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
            MarginStyle paddingStyle = child.Styles.Padding;
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
