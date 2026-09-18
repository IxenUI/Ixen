using Ixen.Core.Visual.Classes;
using System.Text.RegularExpressions;

namespace Ixen.Core.Visual.Styles
{
    internal static class StyleColors
    {
        private static readonly Regex _literal =
            new Regex(@"^#(?:[0-9A-Fa-f]{8}|[0-9A-Fa-f]{6})$");

        private static readonly Regex _token =
            new Regex(@"^\$[A-Za-z0-9_-]+$");

        internal static bool IsLiteral(string value)
            => value != null && _literal.IsMatch(value.Trim());

        internal static bool IsToken(string value)
            => value != null && _token.IsMatch(value.Trim());

        internal static bool IsValue(string value)
            => IsLiteral(value) || IsToken(value);

        internal static string Resolve(StyleTokens tokens, string value)
            => tokens == null ? value : tokens.Resolve(value);

        internal static int VersionOf(StyleTokens tokens)
            => tokens == null ? 0 : tokens.Version;
    }
}
