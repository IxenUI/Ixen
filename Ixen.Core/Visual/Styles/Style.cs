namespace Ixen.Core.Visual.Styles
{
    public abstract class Style
    {
        protected string _content;

        public Style()
        {}

        public Style(string content)
        {
            _content = content;
            Parse();
        }

        public abstract void Parse();
    }
}
