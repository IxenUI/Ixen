using Ixen.Core.Visual.Styles.Descriptors;
using SkiaSharp;

namespace Ixen.Core.Rendering
{
    internal static class Dashes
    {
        private const float DASH_ON = 3f;
        private const float DASH_OFF = 2f;
        private const float DOT_OFF = 2f;
        private const float DOT_ON = 0.01f;

        internal static SKPathEffect Effect(BorderStyle style, float width)
        {
            if (style == BorderStyle.Solid || width <= 0)
            {
                return null;
            }

            return style == BorderStyle.Dashed
                ? SKPathEffect.CreateDash(new[] { DASH_ON * width, DASH_OFF * width }, 0)
                : SKPathEffect.CreateDash(new[] { DOT_ON, DOT_OFF * width }, 0);
        }

        internal static SKStrokeCap Cap(BorderStyle style)
            => style == BorderStyle.Dotted ? SKStrokeCap.Round : SKStrokeCap.Butt;
    }
}
