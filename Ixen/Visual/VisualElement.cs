using Ixen.Rendering;
using Ixen.Visual.Styles;
using System;
using System.Collections.Generic;

namespace Ixen.Visual
{
    public class VisualElement : AbstractVisualElement
    {
        private List<VisualElement> _contents = new();

        private float _totalWidthWeight;
        private float _totalHeightWeight;
        private bool _totalWeightSet = false;

        internal VisualElement Parent { get; private set; }
        public string Id { get; set; }
        public string Name { get; set; }

        private void GetTotalWeight()
        {
            _totalWidthWeight = 0;
            _totalHeightWeight = 0;

            foreach (VisualElement element in _contents)
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

        internal override void ComputeSizes(VisualElement container)
        {
            float usedWidth = 0;
            float usedHeight = 0;
            
            if (!_totalWeightSet)
            {
                GetTotalWeight();
            }

            foreach (VisualElement element in _contents)
            {
                usedWidth  += ComputeWidth(element, container, element.Styles.Width);
                usedHeight += ComputeHeight(element, container, element.Styles.Height);
            }

            if (_totalWidthWeight == 0 && _totalHeightWeight == 0)
            {
                foreach (VisualElement element in _contents)
                {
                    element.ComputeSizes(this);
                }

                return;
            }

            float remainingWidth = Math.Max(0, Width - usedWidth);
            float remainingHeight = Math.Max(0, Height - usedHeight);

            foreach (VisualElement element in _contents)
            {
                if (_totalWidthWeight > 0)
                {
                    ComputeFilledWidth(element, remainingWidth, element.Styles.Width);
                }

                if (_totalHeightWeight > 0)
                {
                    ComputeFilledHeight(element, remainingHeight, element.Styles.Height);
                }

                element.ComputeSizes(this);
            }
        }

        private float ComputeWidth(VisualElement element, VisualElement container, SizeStyle widthStyle)
        {
            switch (widthStyle?.Unit)
            {
                case SizeUnit.Pixels:
                    return element.Width = widthStyle.Value;
                case SizeUnit.Percents:
                    return element.Width = (container.Width / 100) * widthStyle.Value;
                default:
                    return 0;
            }
        }

        private float ComputeHeight(VisualElement element, VisualElement container, SizeStyle heightStyle)
        {
            switch (heightStyle?.Unit)
            {
                case SizeUnit.Pixels:
                    return element.Height = heightStyle.Value;
                case SizeUnit.Percents:
                    return element.Height = (container.Height / 100) * heightStyle.Value;
                default:
                    return 0;
            }
        }

        private float ComputeFilledWidth(VisualElement element, float remainingWidth, SizeStyle widthStyle)
        {
            switch (widthStyle?.Unit)
            {
                case SizeUnit.Weight:
                    return element.Width = (remainingWidth / _totalWidthWeight) * widthStyle.Value;
                default:
                    return 0;
            }
        }

        private float ComputeFilledHeight(VisualElement element, float remainingHeight, SizeStyle heightStyle)
        {
            switch (heightStyle?.Unit)
            {
                case SizeUnit.Weight:
                    return element.Height = (remainingHeight / _totalHeightWeight) * heightStyle.Value;
                default:
                    return 0;
            }
        }

        internal override void ComputeLayout(VisualElement container)
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

            foreach (var element in _contents)
            {
                element.ComputeLayout(this);
            }
        }

        private void ComputeColumnLayout()
        {
            float x = X;
            float y = Y;

            foreach (VisualElement element in _contents)
            {
                element.SetPosition(x, y);
                y += element.Height;
            }
        }

        private void ComputeRowLayout()
        {
            float x = X;
            float y = Y;

            foreach (VisualElement element in _contents)
            {
                element.SetPosition(x, y);
                x += element.Width;
            }
        }

        internal override void Render(RendererContext context, ViewPort viewPort)
        {
            Styles.Render(this, context);

            foreach (VisualElement element in _contents)
            {
                element.Render(context, viewPort);
            }
        }

        public void AddContent(VisualElement element)
        {
            element.Parent = this;
            _contents.Add(element);
        }

        public void AddContent(params VisualElement[] elements)
        {
            foreach (VisualElement element in elements)
            {
                AddContent(element);
            }
        }

        public void RemoveContent(VisualElement element)
        {
            if (_contents.Remove(element))
            {
                element.Parent = null;
            }
        }

        public IEnumerable<VisualElement> GetElements()
        {
            foreach (VisualElement element in _contents)
            {
                yield return element;
            }
        }
    }
}
