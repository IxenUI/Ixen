namespace Ixen.Core.Visual.Styles.Parsers
{
    internal abstract class StyleParser
    {
        protected string _content;

        public bool IsValid { get; protected set; } = true;

        public StyleParser(string content)
        {
            _content = content;
            IsValid = Parse();
        }

        protected abstract bool Parse();
    }
}
