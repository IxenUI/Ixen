namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class GridIndexStyleDescriptor : StyleDescriptor
    {
        public const int AUTO = -1;

        internal override string Identifier => StyleIdentifier.GRID_INDEX;

        public int Value { get; set; } = AUTO;

        public bool IsAuto => Value < 0;

        public void Set(GridIndexStyleDescriptor descriptor) => Value = descriptor.Value;
    }

    public class ColumnIndexStyleDescriptor : GridIndexStyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.COLUMN_INDEX;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(ColumnIndexStyleDescriptor)} {{ {nameof(Value)} = {Value} }}";
    }

    public class RowIndexStyleDescriptor : GridIndexStyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.ROW_INDEX;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(RowIndexStyleDescriptor)} {{ {nameof(Value)} = {Value} }}";
    }

    public class GridSpanStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.GRID_SPAN;

        public int Value { get; set; } = 1;

        public void Set(GridSpanStyleDescriptor descriptor) => Value = descriptor.Value;
    }

    public class ColumnSpanStyleDescriptor : GridSpanStyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.COLUMN_SPAN;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(ColumnSpanStyleDescriptor)} {{ {nameof(Value)} = {Value} }}";
    }

    public class RowSpanStyleDescriptor : GridSpanStyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.ROW_SPAN;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(RowSpanStyleDescriptor)} {{ {nameof(Value)} = {Value} }}";
    }
}
