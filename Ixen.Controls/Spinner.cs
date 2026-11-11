using Ixen.Core.Accessibility;
using Ixen.Core.Rendering;
using Ixen.Core.Visual;
using System;

namespace Ixen.Controls
{
    public class Spinner : DrawingElement
    {
        public const string TRACK_TOKEN = "$control_line_off";

        private const float SWEEP = 90;

        public Spinner()
        {
            TypeName = nameof(Spinner);
            Role = AccessibleRole.ProgressBar;
            Label = "Busy";
        }

        protected override void OnDraw(DrawingContext context)
        {
            float side = Math.Min(context.Width, context.Height);

            if (side <= 0)
            {
                return;
            }

            float thickness = Math.Max(1.5f, side / 8);
            float radius = side / 2 - thickness / 2;
            float x = context.Width / 2;
            float y = context.Height / 2;

            context.DrawCircle(x, y, radius, context.ColorOf(TRACK_TOKEN), thickness);
            context.DrawArc(x, y, radius, 0, SWEEP, context.Foreground, thickness);
        }
    }
}
