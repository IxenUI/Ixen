namespace Ixen.Core.Visual.Styles
{
    public abstract class Style
    {
        internal abstract string Identifier { get; }

        protected string _content;

        public bool IsValid { get; protected set; } = true;

        public Style()
        {}

        public Style(string content)
        {
            _content = content;
            IsValid = Parse();
        }

        protected abstract bool Parse();
    }
}
