namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class GapStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.GAP;

        public float Row { get; set; }
        public float Column { get; set; }

        internal bool IsDeclared => Row > 0 || Column > 0;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(GapStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Row)} = {SourceOf(Row)}, " +
                    $"{nameof(Column)} = {SourceOf(Column)} " +
                "}";
    }
}
