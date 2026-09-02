using Ixen.Core.Rendering;
using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class BackgroundStyleHandler : RenderedStyleHandler
    {
        public BackgroundStyleDescriptor Descriptor { get; private set; }

        private Brush _brush;
        private Color _color = Color.Transparent;
        private readonly GradientShader[] _gradients;

        private readonly string _colorSource;
        private readonly Gradient[] _gradientSnapshots;

        public BackgroundStyleHandler()
            : this(new())
        { }

        public BackgroundStyleHandler(BackgroundStyleDescriptor descriptor)
        {
            Descriptor = descriptor;
            _color = new Color(descriptor.Color);
            _brush = new Brush(_color);

            _colorSource = descriptor.Color;

            int count = descriptor.Layers.Count;

            _gradients = new GradientShader[count];
            _gradientSnapshots = new Gradient[count];

            for (int index = 0; index < count; index++)
            {
                Gradient gradient = descriptor.Layers[index].Gradient;

                if (gradient == null)
                {
                    continue;
                }

                _gradientSnapshots[index] = gradient.Snapshot();
                _gradients[index] = new GradientShader(gradient);
            }
        }

        internal GradientShader GradientFor(int index)
            => index >= 0 && index < _gradients.Length ? _gradients[index] : null;

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

                if (_gradientSnapshots.Length != Descriptor.Layers.Count)
                {
                    return false;
                }

                for (int index = 0; index < _gradientSnapshots.Length; index++)
                {
                    Gradient gradient = Descriptor.Layers[index].Gradient;
                    Gradient snapshot = _gradientSnapshots[index];

                    if (snapshot == null || gradient == null)
                    {
                        if (snapshot != null || gradient != null)
                        {
                            return false;
                        }

                        continue;
                    }

                    if (!snapshot.SameAs(gradient))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        internal Color Color => _color;

        internal override void Render(VisualElement element, RendererContext context)
        {
            CornerRadiusStyleDescriptor radius = element.StylesHandlers.CornerRadius.Descriptor;
            Brush brush = element.AnimatedBrush(StyleIdentifier.BACKGROUND) ?? _brush;

            if (radius.HasRadius)
            {
                context.FillRoundRectangle(element.X, element.Y, element.ActualWidth, element.ActualHeight, radius, brush);
                return;
            }

            context.FillRectangle(element.X, element.Y, element.ActualWidth, element.ActualHeight, brush);
        }
    }
}
