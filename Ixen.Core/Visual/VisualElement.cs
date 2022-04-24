using Ixen.Core.Rendering;
using Ixen.Core.Visual.Styles;
using System;
using System.Collections.Generic;

namespace Ixen.Core.Visual
{
    public sealed class VisualElement : BoxedElement
    {
        private List<VisualElement> _children = new();
        private ViewPort _viewPort = new();
        private float _totalWidthWeight;
        private float _totalHeightWeight;
        private bool _totalWeightSet = false;

        internal VisualElement Parent { get; private set; }
        internal bool IsRendered { get; private set; }

        public string Id { get; set; }
        public string Name { get; set; }
        public VisualElementStyles Styles { get; set; } = new();

        private void GetTotalWeight()
        {
            _totalWidthWeight = 0;
            _totalHeightWeight = 0;

            foreach (VisualElement element in _children)
            {
                if (element.Styles.Width?.Unit == SizeUnit.Weight)
                {
                    _totalWidthWeight += element.Styles.Width.Value;
                }

                if (element.Styles.Height?.Unit == SizeUnit.Weight)
                {
                    _totalHeightWeight += element.Styles.Height.Value;
                }
            }

            _totalWeightSet = true;
        }

        internal void ComputeSizes(VisualElement container)
        {
            float usedWidth = 0;
            float usedHeight = 0;
            
            if (!_totalWeightSet)
            {
                GetTotalWeight();
            }

            foreach (VisualElement element in _children)
            {
                usedWidth  += ComputeWidth(element, container);
                usedHeight += ComputeHeight(element, container);
            }

            if (_totalWidthWeight == 0 && _totalHeightWeight == 0)
            {
                foreach (VisualElement element in _children)
                {
                    element.ComputeSizes(this);
                }

                return;
            }

            float remainingWidth = Math.Max(0, Width - usedWidth);
            float remainingHeight = Math.Max(0, Height - usedHeight);

            foreach (VisualElement element in _children)
            {
                if (_totalWidthWeight > 0)
                {
                    ComputeFilledWidth(element, container, remainingWidth);
                }

                if (_totalHeightWeight > 0)
                {
                    ComputeFilledHeight(element, container, remainingHeight);
                }

                element.ComputeSizes(this);
            }
        }

        private float ComputeWidth(VisualElement element, VisualElement container)
        {
            SizeStyle widthStyle = element.Styles.Width;
            if (widthStyle != null)
            {
                switch (widthStyle?.Unit)
                {
                    case SizeUnit.Pixels:
                        ComputeHorizontalMargin(element, container);
                        element.Width = widthStyle.Value;
                        return element.BoxWidth;

                    case SizeUnit.Percents:
                        ComputeHorizontalMargin(element, container);
                        element.Width = (container.Width / 100) * widthStyle.Value;
                        return element.BoxWidth;
                }
            }

            return 0;
        }

        private float ComputeFilledWidth(VisualElement element, VisualElement container, float remainingWidth)
        {
            SizeStyle widthStyle = element.Styles.Width;
            float margin;
            if (widthStyle != null)
            {
                switch (widthStyle.Unit)
                {
                    case SizeUnit.Weight:
                        margin = ComputeHorizontalMargin(element, container);
                        element.Width = ((remainingWidth - margin) / _totalWidthWeight) * widthStyle.Value;
                        return element.BoxWidth;
                }
            }

            return 0;
        }

        private float ComputeHorizontalMargin(VisualElement element, VisualElement container)
        {
            MarginStyle marginStyle = element.Styles.Margin;
            if (marginStyle != null)
            {
                switch (marginStyle.Left.Unit)
                {
                    case SizeUnit.Pixels:
                        element.MarginLeft = marginStyle.Left.Value;
                        break;
                    case SizeUnit.Percents:
                        element.MarginLeft = (container.Width / 100) * marginStyle.Left.Value;
                        break;
                }

                switch (marginStyle.Right.Unit)
                {
                    case SizeUnit.Pixels:
                        element.MarginRight = marginStyle.Right.Value;
                        break;
                    case SizeUnit.Percents:
                        element.MarginRight = (container.Width / 100) * marginStyle.Right.Value;
                        break;
                }
            }

            return element.HorizontalMargin;
        }

        private float ComputeHeight(VisualElement element, VisualElement container)
        {
            SizeStyle heightStyle = element.Styles.Height;
            if (heightStyle != null)
            {
                switch (heightStyle.Unit)
                {
                    case SizeUnit.Pixels:
                        ComputeVerticalMargin(element, container);
                        element.Height = heightStyle.Value;
                        return element.BoxHeight;

                    case SizeUnit.Percents:
                        ComputeVerticalMargin(element, container);
                        element.Height = (container.Height / 100) * heightStyle.Value;
                        return element.BoxHeight;
                }
            }

            return 0;
        }

        private float ComputeFilledHeight(VisualElement element, VisualElement container, float remainingHeight)
        {
            SizeStyle heightStyle = element.Styles.Height;
            float margin;
            if (heightStyle != null)
            {
                switch (heightStyle.Unit)
                {
                    case SizeUnit.Weight:
                        margin = ComputeVerticalMargin(element, container);
                        element.Height = ((remainingHeight - margin) / _totalHeightWeight) * heightStyle.Value;
                        return element.BoxHeight;
                }
            }

            return 0;
        }

        private float ComputeVerticalMargin(VisualElement element, VisualElement container)
        {
            MarginStyle marginStyle = element.Styles.Margin;
            if (marginStyle != null)
            {
                switch (marginStyle.Top.Unit)
                {
                    case SizeUnit.Pixels:
                        element.MarginTop = marginStyle.Top.Value;
                        break;
                    case SizeUnit.Percents:
                        element.MarginTop = (container.Height / 100) * marginStyle.Top.Value;
                        break;
                }

                switch (marginStyle.Bottom.Unit)
                {
                    case SizeUnit.Pixels:
                        element.MarginBottom = marginStyle.Bottom.Value;
                        break;
                    case SizeUnit.Percents:
                        element.MarginBottom = (container.Height / 100) * marginStyle.Bottom.Value;
                        break;
                }
            }

            return element.VerticalMargin;
        }

        internal void ComputeLayout(VisualElement container)
        {
            switch (Styles.Layout?.Type)
            {
                case LayoutType.Column:
                    ComputeColumnLayout();
                    break;

                case LayoutType.Row:
                    ComputeRowLayout();
                    break;

                case LayoutType.Grid:
                    break;

                case LayoutType.Absolute:
                    break;

                case LayoutType.Fixed:
                    break;

                case LayoutType.Dock:
                    break;
            }

            foreach (var element in _children)
            {
                element.ComputeLayout(this);
            }
        }

        private void ComputeColumnLayout()
        {
            float x = X;
            float y = Y;

            foreach (VisualElement element in _children)
            {
                element.SetPosition(x, y);
                y += element.BoxHeight;
            }
        }

        private void ComputeRowLayout()
        {
            float x = X;
            float y = Y;

            foreach (VisualElement element in _children)
            {
                element.SetPosition(x, y);
                x += element.BoxWidth;
            }
        }

        internal void Render(RendererContext context, ViewPort viewPort)
        {
            Styles.Render(this, context);

            foreach (VisualElement element in _children)
            {
                element.Render(context, viewPort);
            }
        }

        public void AddChild(VisualElement element)
        {
            element.Parent = this;
            _children.Add(element);
        }

        public void AddChildren(params VisualElement[] elements)
        {
            foreach (VisualElement element in elements)
            {
                AddChild(element);
            }
        }

        public void RemoveChild(VisualElement element)
        {
            if (_children.Remove(element))
            {
                element.Parent = null;
            }
        }

        public IEnumerable<VisualElement> GetChildren()
        {
            foreach (VisualElement element in _children)
            {
                yield return element;
            }
        }

        internal void Invalidate()
        {
            IsRendered = false;
        }
    }
}
