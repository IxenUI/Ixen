namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class PaddingStyleDescriptor : MarginStyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.Padding;


        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(PaddingStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Top)} = new {nameof(SizeStyleDescriptor)} " +
                    "{ " +
                        $"{nameof(SizeStyleDescriptor.Unit)} = {nameof(SizeUnit)}.{Top.Unit}, " +
                        $"{nameof(SizeStyleDescriptor.Value)} = {Top.Value} " +
                    "}, " +

                    $"{nameof(Right)} = new {nameof(SizeStyleDescriptor)} " +
                    "{ " +
                        $"{nameof(SizeStyleDescriptor.Unit)} = {nameof(SizeUnit)}.{Right.Unit}, " +
                        $"{nameof(SizeStyleDescriptor.Value)} = {Right.Value} " +
                    "}, " +

                    $"{nameof(Bottom)} = new {nameof(SizeStyleDescriptor)} " +
                    "{ " +
                        $"{nameof(SizeStyleDescriptor.Unit)} = {nameof(SizeUnit)}.{Bottom.Unit}, " +
                        $"{nameof(SizeStyleDescriptor.Value)} = {Bottom.Value} " +
                    "}, " +

                    $"{nameof(Left)} = new {nameof(SizeStyleDescriptor)} " +
                    "{ " +
                        $"{nameof(SizeStyleDescriptor.Unit)} = {nameof(SizeUnit)}.{Left.Unit}, " +
                        $"{nameof(SizeStyleDescriptor.Value)} = {Left.Value} " +
                    "}, " +
                "}";
    }
}
