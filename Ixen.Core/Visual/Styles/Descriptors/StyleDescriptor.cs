using System.Globalization;
using System.Text;

namespace Ixen.Core.Visual.Styles.Descriptors
{
    public abstract class StyleDescriptor
    {
        internal abstract string Identifier { get; }

        internal object Handler;

        public string Text { get; set; }

        public static T WithText<T>(T descriptor, string text)
            where T : StyleDescriptor
        {
            descriptor.Text = text;

            return descriptor;
        }

        internal virtual bool CanGenerateSource { get; } = false;
        internal virtual string ToSource() => string.Empty;

        internal string ToSourceWithText()
        {
            string expression = ToSource();

            return string.IsNullOrEmpty(Text)
                ? expression
                : "global::Ixen.Core.Visual.Styles.Descriptors.StyleDescriptor.WithText("
                    + expression + ", " + SourceOf(Text) + ")";
        }

        protected internal static string SourceOf(float value)
            => value.ToString("R", CultureInfo.InvariantCulture) + "f";

        protected internal static string SourceOf(bool value)
            => value ? "true" : "false";

        protected internal static string SourceOf(string value)
        {
            if (value == null)
            {
                return "null";
            }

            var sb = new StringBuilder(value.Length + 2);
            sb.Append('"');

            foreach (char c in value)
            {
                switch (c)
                {
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '"':
                        sb.Append("\\\"");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }

            sb.Append('"');

            return sb.ToString();
        }
    }
}
