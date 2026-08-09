using Ixen.Core.Language.Base;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;

namespace Ixen.Generators
{
    internal static class Diagnostics
    {
        private const string CATEGORY = "Ixen";

        private static readonly Dictionary<string, DiagnosticDescriptor> _descriptors = new()
        {
            [LanguageErrorCode.SYNTAX] = Descriptor(LanguageErrorCode.SYNTAX, "Ixen syntax error"),
            [LanguageErrorCode.UNKNOWN_STYLE] = Descriptor(LanguageErrorCode.UNKNOWN_STYLE, "Unknown Ixen style property"),
            [LanguageErrorCode.INVALID_STYLE_VALUE] = Descriptor(LanguageErrorCode.INVALID_STYLE_VALUE, "Invalid Ixen style value"),
            [LanguageErrorCode.STRUCTURE] = Descriptor(LanguageErrorCode.STRUCTURE, "Ixen structure error")
        };

        private static DiagnosticDescriptor Descriptor(string id, string title)
            => new DiagnosticDescriptor(id, title, "{0}", CATEGORY, DiagnosticSeverity.Error, true);

        internal static void Report(SourceProductionContext context, string path, string content,
            IReadOnlyList<LanguageError> errors)
        {
            foreach (LanguageError error in errors)
            {
                if (!_descriptors.TryGetValue(error.Code, out DiagnosticDescriptor descriptor))
                {
                    descriptor = _descriptors[LanguageErrorCode.SYNTAX];
                }

                context.ReportDiagnostic(Diagnostic.Create(descriptor, ToLocation(path, content, error), error.Message));
            }
        }

        private static Location ToLocation(string path, string content, LanguageError error)
        {
            int index = Clamp(error.Index, 0, content.Length);
            int length = Clamp(error.Length, 0, content.Length - index);

            LinePosition start = ToLinePosition(content, index, 0, 0, out int line, out int lineStart);
            LinePosition end = ToLinePosition(content, index + length, line, lineStart, out _, out _);

            return Location.Create(path, new TextSpan(index, length), new LinePositionSpan(start, end));
        }

        private static LinePosition ToLinePosition(string content, int index, int fromLine, int fromLineStart,
            out int line, out int lineStart)
        {
            line = fromLine;
            lineStart = fromLineStart;

            for (int i = fromLineStart; i < index; i++)
            {
                if (content[i] == '\n')
                {
                    line++;
                    lineStart = i + 1;
                }
            }

            return new LinePosition(line, index - lineStart);
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
    }
}
