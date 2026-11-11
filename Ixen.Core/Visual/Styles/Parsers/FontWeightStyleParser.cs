using Ixen.Core.Visual.Styles.Descriptors;
using System.Globalization;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class FontWeightStyleParser : StyleParser
    {
        public FontWeightStyleDescriptor Descriptor { get; } = new();

        public FontWeightStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            string value = _content?.Trim().ToLowerInvariant().Replace("-", string.Empty);

            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int number))
            {
                if (number < 100 || number > 900 || number % 100 != 0)
                {
                    return false;
                }

                Descriptor.Value = (FontWeight)number;
                return true;
            }

            switch (value)
            {
                case "thin":
                    Descriptor.Value = FontWeight.Thin;
                    return true;

                case "extralight":
                    Descriptor.Value = FontWeight.ExtraLight;
                    return true;

                case "light":
                    Descriptor.Value = FontWeight.Light;
                    return true;

                case "normal":
                    Descriptor.Value = FontWeight.Normal;
                    return true;

                case "medium":
                    Descriptor.Value = FontWeight.Medium;
                    return true;

                case "semibold":
                    Descriptor.Value = FontWeight.SemiBold;
                    return true;

                case "bold":
                    Descriptor.Value = FontWeight.Bold;
                    return true;

                case "extrabold":
                    Descriptor.Value = FontWeight.ExtraBold;
                    return true;

                case "black":
                    Descriptor.Value = FontWeight.Black;
                    return true;

                default:
                    return false;
            }
        }
    }
}
