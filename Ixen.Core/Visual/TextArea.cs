namespace Ixen.Core.Visual
{
    public class TextArea : TextField
    {
        public TextArea()
        {
            TypeName = nameof(TextArea);
            Multiline = true;
            Scrollable = true;
        }
    }
}
