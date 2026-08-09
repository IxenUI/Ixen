using Ixen.Core.Language.Base;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;

namespace Ixen.Generators
{
    internal static class Diagnostics
    {
        private const string CATEGORY = "Ixen";
        private const string FALLBACK_TITLE = "Ixen problem";

        private static readonly Dictionary<string, string> _titles = new()
        {
            [LanguageErrorCode.SYNTAX] = "Ixen syntax problem",
            [LanguageErrorCode.UNKNOWN_STYLE] = "Unknown Ixen style property",
            [LanguageErrorCode.INVALID_STYLE_VALUE] = "Invalid Ixen style value",
            [LanguageErrorCode.STRUCTURE] = "Ixen structure problem",
            [LanguageErrorCode.INVALID_ELEMENT_TYPE] = "Invalid XNL element type",
            [LanguageErrorCode.INVALID_PROPERTY] = "Invalid XNL property",
            [LanguageErrorCode.INVALID_PROPERTY_VALUE] = "Invalid XNL property value"
        };

        private static readonly Dictionary<(string code, DiagnosticSeverity severity), DiagnosticDescriptor> _descriptors
            = BuildDescriptors();

        private static Dictionary<(string, DiagnosticSeverity), DiagnosticDescriptor> BuildDescriptors()
        {
            var result = new Dictionary<(string, DiagnosticSeverity), DiagnosticDescriptor>();

            foreach (KeyValuePair<string, string> entry in _titles)
            {
                result[(entry.Key, DiagnosticSeverity.Error)] = Descriptor(entry.Key, entry.Value, DiagnosticSeverity.Error);
                result[(entry.Key, DiagnosticSeverity.Warning)] = Descriptor(entry.Key, entry.Value, DiagnosticSeverity.Warning);
            }

            return result;
        }

        private static DiagnosticDescriptor Descriptor(string id, string title, DiagnosticSeverity severity)
            => new DiagnosticDescriptor(id, title, "{0}", CATEGORY, severity, true);

        internal static void Report(SourceProductionContext context, string path, string content,
            IReadOnlyList<LanguageError> diagnostics)
        {
            foreach (LanguageError diagnostic in diagnostics)
            {
                DiagnosticSeverity severity = diagnostic.Severity == LanguageErrorSeverity.Warning
                    ? DiagnosticSeverity.Warning
                    : DiagnosticSeverity.Error;

                if (!_descriptors.TryGetValue((diagnostic.Code, severity), out DiagnosticDescriptor descriptor))
                {
                    descriptor = Descriptor(diagnostic.Code, FALLBACK_TITLE, severity);
                }

                context.ReportDiagnostic(Diagnostic.Create(descriptor, ToLocation(path, content, diagnostic), diagnostic.Message));
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
