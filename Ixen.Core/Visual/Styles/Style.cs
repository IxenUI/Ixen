namespace Ixen.Core.Visual.Styles
{
    public abstract class Style
    {
        internal abstract string Identifier { get; }
        internal string Content { get; set; }

        public bool IsValid { get; protected set; } = true;

        public Style()
        {}

        public Style(string content)
        {
            Content = content;
            IsValid = Parse();
        }

        protected abstract bool Parse();
    }
}
