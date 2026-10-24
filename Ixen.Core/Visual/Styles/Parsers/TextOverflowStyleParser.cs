using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class TextOverflowStyleParser : StyleParser
    {
        public TextOverflowStyleDescriptor Descriptor { get; } = new();

        public TextOverflowStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            string[] parts = (_content ?? string.Empty).Trim().ToLowerInvariant()
                .Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length < 1 || parts.Length > 2)
            {
                return false;
            }

            switch (parts[0])
            {
                case "clip":
                    Descriptor.Value = TextOverflow.Clip;
                    break;

                case "ellipsis":
                    Descriptor.Value = TextOverflow.Ellipsis;
                    break;

                default:
                    return false;
            }

            if (parts.Length == 1)
            {
                return true;
            }

            if (!int.TryParse(parts[1], System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out int lines) || lines < 1)
            {
                return false;
            }

            Descriptor.MaxLines = lines;

            return true;
        }
    }
}
