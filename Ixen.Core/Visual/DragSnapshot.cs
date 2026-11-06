using Ixen.Core.Accessibility;
using Ixen.Core.Rendering;
using Ixen.Core.Visual.Styles.Descriptors;
using SkiaSharp;

namespace Ixen.Core.Visual
{
    internal sealed class DragSnapshot : DrawingElement
    {
        internal const string TYPE = "DragSnapshot";

        private readonly SKPicture _picture;
        private readonly float _scale;

        internal DragSnapshot(SKPicture picture, float scale, float width, float height)
        {
            _picture = picture;
            _scale = scale <= 0 ? 1 : scale;

            TypeName = TYPE;
            Role = AccessibleRole.Presentation;

            Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = width };
            Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = height };
        }

        internal SKPicture Picture => _picture;

        protected override void OnDraw(DrawingContext context)
        {
            SKCanvas canvas = context.Canvas;

            if (canvas == null || _picture == null)
            {
                return;
            }

            int saved = canvas.Save();

            canvas.Scale(1 / _scale);
            canvas.DrawPicture(_picture);
            canvas.RestoreToCount(saved);
        }
    }
}
