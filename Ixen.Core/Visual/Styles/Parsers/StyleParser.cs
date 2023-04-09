namespace Ixen.Core.Visual.Styles.Parsers
{
    public abstract class StyleParser
    {
        internal string Content { get; set; }

        public bool IsValid { get; protected set; } = true;

        public StyleParser(string content)
        {
            Content = content;
            IsValid = Parse();
        }

        protected abstract bool Parse();
    }
}
