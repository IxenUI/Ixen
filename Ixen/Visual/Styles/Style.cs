namespace Ixen.Visual.Styles
{
    public abstract class Style
    {
        protected string _content;
        public abstract void Parse();

        public Style()
        {}

        public Style(string content)
        {
            _content = content;
            Parse();
        }
    }
}
