using Ixen.Rendering;

namespace Ixen.Visual
{
    public abstract class AbstractVisualElement : DimensionalElement
    {
        private float _boxWidth;
        private float _boxHeight;
        private float _boxX;
        private float _boxY;

        private float _contentWidth;
        private float _contentHeight;
        private float _contentX;
        private float _contentY;

        private bool _isPositioned;
        private bool _isDimensioned;
        private bool _isRendered;

        protected ViewPort _viewPort = new();

        internal abstract void Compute(VisualElement container, DimensionalElement targetZone);
        internal abstract void Render(RendererContext context, ViewPort viewPort);

        public VisualElementStyles Styles { get; set; } = new();

        internal float BoxWidth
        {
            get => _boxWidth;
            set => _boxWidth = value;
        }

        internal float BoxHeight
        {
            get => _boxHeight;
            set => _boxHeight = value;
        }

        internal float BoxX
        {
            get => _boxX;
            set => _boxX = value;
        }

        internal float BoxY
        {
            get => _boxY;
            set => _boxY = value;
        }

        internal float ContentWidth
        {
            get => _contentWidth;
            set => _contentWidth = value;
        }

        internal float ContentHeight
        {
            get => _contentHeight;
            set => _contentHeight = value;
        }

        internal float ContentX
        {
            get => _contentX;
            set => _contentX = value;
        }

        internal float ContentY
        {
            get => _contentY;
            set => _contentY = value;
        }

        internal bool IsPositioned
        {
            get => _isPositioned;
            set => _isPositioned = value;
        }

        internal bool IsDimensioned
        {
            get => _isDimensioned;
            set => _isDimensioned = value;
        }

        internal bool IsRendered
        {
            get => _isRendered;
            set => _isRendered = value;
        }

        internal void Invalidate()
        {
            IsPositioned = false;
            IsDimensioned = false;
            IsRendered = false;
        }
    }
}
