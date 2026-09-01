using System.Globalization;
using System.Text;

namespace Ixen.Core.Visual.Styles.Descriptors
{
    public abstract class StyleDescriptor
    {
        internal abstract string Identifier { get; }

        internal object Handler;

        internal virtual bool CanGenerateSource { get; } = false;
        internal virtual string ToSource() => string.Empty;

        protected static string SourceOf(float value)
            => value.ToString("R", CultureInfo.InvariantCulture) + "f";

        protected static string SourceOf(bool value)
            => value ? "true" : "false";

        protected static string SourceOf(string value)
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
