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

        protected static bool IsImageName(string value)
        {
            int dot = value.LastIndexOf('.');

            if (dot <= 0 || dot >= value.Length - 1)
            {
                return false;
            }

            for (int index = dot + 1; index < value.Length; index++)
            {
                if (!char.IsLetter(value[index]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
