using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class Shadow
    {
        public float OffsetX { get; set; }
        public float OffsetY { get; set; }
        public float Blur { get; set; }
        public float Spread { get; set; }
        public string Color { get; set; }
        public bool Inset { get; set; }

        private static string Of(float value)
            => value.ToString("R", CultureInfo.InvariantCulture) + "f";

        internal Shadow Copy()
            => new Shadow
            {
                OffsetX = OffsetX,
                OffsetY = OffsetY,
                Blur = Blur,
                Spread = Spread,
                Color = Color,
                Inset = Inset
            };

        internal bool SameAs(Shadow other)
            => other != null
                && OffsetX == other.OffsetX
                && OffsetY == other.OffsetY
                && Blur == other.Blur
                && Spread == other.Spread
                && Color == other.Color
                && Inset == other.Inset;

        internal string ToSource()
            => $"new {nameof(Shadow)} {{ "
                + $"{nameof(OffsetX)} = {Of(OffsetX)}, "
                + $"{nameof(OffsetY)} = {Of(OffsetY)}, "
                + $"{nameof(Blur)} = {Of(Blur)}, "
                + $"{nameof(Spread)} = {Of(Spread)}, "
                + $"{nameof(Inset)} = {(Inset ? "true" : "false")}, "
                + $"{nameof(Color)} = {(Color == null ? "null" : "\"" + Color + "\"")} }}";
    }

    public class ShadowStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.BOX_SHADOW;

        public List<Shadow> Shadows { get; set; } = new List<Shadow>();

        internal bool IsDeclared => Shadows != null && Shadows.Count > 0;

        internal int Count => Shadows == null ? 0 : Shadows.Count;

        internal Shadow First => Count > 0 ? Shadows[0] : null;

        internal void Set(ShadowStyleDescriptor other)
        {
            Shadows.Clear();
            Shadows.AddRange(other.Shadows);
        }

        internal string Fields()
        {
            var sb = new StringBuilder();

            sb.Append($"{nameof(Shadows)} = new global::System.Collections.Generic.List<{nameof(Shadow)}> {{ ");

            for (int index = 0; index < Count; index++)
            {
                if (index > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(Shadows[index].ToSource());
            }

            sb.Append(" }");

            return sb.ToString();
        }
    }
}
