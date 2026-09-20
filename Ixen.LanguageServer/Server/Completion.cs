using System.Collections.Generic;
using Ixen.Core.Language.Xnl;
using Ixen.Core.Language.Xns;

namespace Ixen.LanguageServer.Server
{
    internal sealed class CompletionItem
    {
        public string Label { get; set; }
        public int Kind { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
    }

    internal static class Completion
    {
        public const int KIND_CLASS = 7;
        public const int KIND_PROPERTY = 10;
        public const int KIND_VALUE = 12;
        public const int KIND_KEYWORD = 14;

        private static readonly CompletionItem[] _none = new CompletionItem[0];

        public static IReadOnlyList<CompletionItem> At(Document document, int offset)
        {
            if (document == null)
            {
                return _none;
            }

            if (Uris.IsXns(document.Path))
            {
                XnsCompletionContext context = XnsCompletions.At(document.Text, offset);

                return Build(context.Items, KindOf(context.Kind), context.SpanStart, context.SpanLength);
            }

            if (Uris.IsXnl(document.Path))
            {
                XnlCompletionContext context = XnlCompletions.At(document.Text, offset);

                return Build(context.Items, KindOf(context.Kind), context.SpanStart, context.SpanLength);
            }

            return _none;
        }

        private static IReadOnlyList<CompletionItem> Build(IReadOnlyList<string> items, int kind, int start, int length)
        {
            if (kind < 0 || items == null || items.Count == 0)
            {
                return _none;
            }

            List<CompletionItem> built = new List<CompletionItem>(items.Count);

            foreach (string item in items)
            {
                if (!string.IsNullOrEmpty(item))
                {
                    built.Add(new CompletionItem { Label = item, Kind = kind, Start = start, Length = length });
                }
            }

            return built;
        }

        private static int KindOf(XnsCompletionKind kind)
        {
            switch (kind)
            {
                case XnsCompletionKind.StyleName: return KIND_PROPERTY;
                case XnsCompletionKind.StyleValue: return KIND_VALUE;
                case XnsCompletionKind.State: return KIND_KEYWORD;
                default: return -1;
            }
        }

        private static int KindOf(XnlCompletionKind kind)
        {
            switch (kind)
            {
                case XnlCompletionKind.ElementType: return KIND_CLASS;
                case XnlCompletionKind.PropertyName: return KIND_PROPERTY;
                case XnlCompletionKind.PropertyValue: return KIND_VALUE;
                default: return -1;
            }
        }
    }
}
