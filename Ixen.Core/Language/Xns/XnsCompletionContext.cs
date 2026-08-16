using System.Collections.Generic;

namespace Ixen.Core.Language.Xns
{
    internal class XnsCompletionContext
    {
        internal static readonly XnsCompletionContext None
            = new XnsCompletionContext(XnsCompletionKind.None, null, 0, 0, new string[0]);

        internal XnsCompletionKind Kind { get; }
        internal string StyleName { get; }
        internal int SpanStart { get; }
        internal int SpanLength { get; }
        internal IReadOnlyList<string> Items { get; }

        internal XnsCompletionContext(XnsCompletionKind kind, string styleName, int spanStart, int spanLength, IReadOnlyList<string> items)
        {
            Kind = kind;
            StyleName = styleName;
            SpanStart = spanStart;
            SpanLength = spanLength;
            Items = items;
        }
    }
}
