namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class MarginStyleDescriptor : SpaceStyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.MARGIN;

        public void Set(SpaceStyleDescriptor spaceDescriptor)
        {
            Top = spaceDescriptor.Top;
            Right = spaceDescriptor.Right;
            Bottom = spaceDescriptor.Bottom;
            Left = spaceDescriptor.Left;
        }


        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(MarginStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Top)} = new {nameof(SizeStyleDescriptor)} " +
                    "{ " +
                        $"{nameof(SizeStyleDescriptor.Unit)} = {nameof(SizeUnit)}.{Top.Unit}, " +
                        $"{nameof(SizeStyleDescriptor.Value)} = {SourceOf(Top.Value)} " +
                    "}, " +

                    $"{nameof(Right)} = new {nameof(SizeStyleDescriptor)} " +
                    "{ " +
                        $"{nameof(SizeStyleDescriptor.Unit)} = {nameof(SizeUnit)}.{Right.Unit}, " +
                        $"{nameof(SizeStyleDescriptor.Value)} = {SourceOf(Right.Value)} " +
                    "}, " +

                    $"{nameof(Bottom)} = new {nameof(SizeStyleDescriptor)} " +
                    "{ " +
                        $"{nameof(SizeStyleDescriptor.Unit)} = {nameof(SizeUnit)}.{Bottom.Unit}, " +
                        $"{nameof(SizeStyleDescriptor.Value)} = {SourceOf(Bottom.Value)} " +
                    "}, " +

                    $"{nameof(Left)} = new {nameof(SizeStyleDescriptor)} " +
                    "{ " +
                        $"{nameof(SizeStyleDescriptor.Unit)} = {nameof(SizeUnit)}.{Left.Unit}, " +
                        $"{nameof(SizeStyleDescriptor.Value)} = {SourceOf(Left.Value)} " +
                    "}, " +
                "}";
    }
}
