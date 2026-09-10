namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class GridAreaStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.GRID_AREA;

        public string Value { get; set; }

        public bool IsDeclared => !string.IsNullOrEmpty(Value);

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(GridAreaStyleDescriptor)} {{ {nameof(Value)} = \"{Value}\" }}";
    }
}
