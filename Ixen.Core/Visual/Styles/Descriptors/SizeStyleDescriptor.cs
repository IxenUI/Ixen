using System.Collections.Generic;
using System.Linq;

namespace Ixen.Core.Visual.Styles.Descriptors
{
    internal enum SizeStyleDescriptorType
    {
        Width,
        Height
    }

    public enum SizeUnit
    {
        Unset, 
        Pixels,
        Percents,
        Weight,
        Content
    }

    public class SizeStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.SIZE;

        public SizeUnit Unit { get; set; } = SizeUnit.Unset; // by default, is equivalent to Weight, but does not override inherited value
        public float Value { get; set; } = 1;
        public float Offset { get; set; } = 0;

        public SizeFunction Function { get; set; } = SizeFunction.None;
        public List<SizePart> Parts { get; set; } = new List<SizePart>();

        public void Set(SizeStyleDescriptor other)
        {
            Unit = other.Unit;
            Value = other.Value;
            Offset = other.Offset;
            Function = other.Function;
            Parts = other.Parts;
        }

        internal float Of(float available)
        {
            switch (Function)
            {
                case SizeFunction.Min:
                    return Parts.Min(p => p.Of(available));

                case SizeFunction.Max:
                    return Parts.Max(p => p.Of(available));

                case SizeFunction.Clamp:
                    float low = Parts[0].Of(available);
                    float high = Parts[2].Of(available);

                    return System.Math.Max(low,
                        System.Math.Min(Parts[1].Of(available), high));

                default:
                    return (available / 100f) * Value + Offset;
            }
        }

        internal string Fields()
        {
            string parts = Parts.Count == 0
                ? ""
                : $", {nameof(Parts)} = new() {{ "
                    + string.Join(", ", Parts.Select(p => p.ToSource()))
                    + "}";

            return $"{nameof(Unit)} = {nameof(SizeUnit)}.{Unit}, "
                + $"{nameof(Value)} = {SourceOf(Value)}, "
                + $"{nameof(Offset)} = {SourceOf(Offset)}, "
                + $"{nameof(Function)} = {nameof(SizeFunction)}.{Function}"
                + parts + " ";
        }
    }
}
