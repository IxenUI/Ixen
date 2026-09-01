using Ixen.Core.Rendering;
using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class BackgroundStyleHandler : RenderedStyleHandler
    {
        public BackgroundStyleDescriptor Descriptor { get; private set; }

        private Brush _brush;
        private Color _color = Color.Transparent;
        private readonly GradientShader _gradient;

        private readonly string _colorSource;
        private readonly Gradient _gradientSnapshot;

        public BackgroundStyleHandler()
            : this(new())
        { }

        public BackgroundStyleHandler(BackgroundStyleDescriptor descriptor)
        {
            Descriptor = descriptor;
            _color = new Color(descriptor.Color);
            _brush = new Brush(_color);

            _colorSource = descriptor.Color;
            _gradientSnapshot = descriptor.Gradient?.Snapshot();

            if (descriptor.Gradient != null)
            {
                _gradient = new GradientShader(descriptor.Gradient);
            }
        }

        internal static BackgroundStyleHandler For(BackgroundStyleDescriptor descriptor)
        {
            if (descriptor.Handler is BackgroundStyleHandler handler && handler.IsCurrent)
            {
                return handler;
            }

            handler = new BackgroundStyleHandler(descriptor);

            descriptor.Handler = handler;

            return handler;
        }

        private bool IsCurrent
        {
            get
            {
                if (_colorSource != Descriptor.Color)
                {
                    return false;
                }

                Gradient gradient = Descriptor.Gradient;

                if (_gradientSnapshot == null || gradient == null)
                {
                    return _gradientSnapshot == null && gradient == null;
                }

                return _gradientSnapshot.SameAs(gradient);
            }
        }

        internal Color Color => _color;

        internal override void Render(VisualElement element, RendererContext context)
        {
            CornerRadiusStyleDescriptor radius = element.StylesHandlers.CornerRadius.Descriptor;
            Brush brush = element.AnimatedBrush(StyleIdentifier.BACKGROUND) ?? _brush;

            if (_gradient != null && element.AnimatedBrush(StyleIdentifier.BACKGROUND) == null)
            {
                context.FillGradient(element.X, element.Y, element.ActualWidth, element.ActualHeight,
                    radius, _gradient);

                return;
            }

            if (radius.HasRadius)
            {
                context.FillRoundRectangle(element.X, element.Y, element.ActualWidth, element.ActualHeight, radius, brush);
                return;
            }

            context.FillRectangle(element.X, element.Y, element.ActualWidth, element.ActualHeight, brush);
        }
    }
}
