using System;
using System.Collections.Generic;
using Ixen.Core.Language.Xnl;
using Ixen.Core.Language.Xns;
using Ixen.Core.Visual.Classes;

namespace Ixen.LanguageServer.Server
{
    internal sealed class Target
    {
        public string Path { get; set; }
        public int Index { get; set; }
        public int Length { get; set; }
    }

    internal static class Definition
    {
        private static readonly Target[] _none = new Target[0];

        public static IReadOnlyList<Target> Find(Document document, int offset, Workspace workspace, Documents documents)
        {
            if (document == null)
            {
                return _none;
            }

            if (Uris.IsXns(document.Path))
            {
                return InStyles(document, offset);
            }

            if (Uris.IsXnl(document.Path))
            {
                return InView(document, offset, workspace, documents);
            }

            return _none;
        }

        private static IReadOnlyList<Target> InStyles(Document document, int offset)
        {
            List<XnsToken> tokens = new XnsSource(document.Text).Tokenize();
            XnsToken at = TokenAt(tokens, offset);

            if (at != null && at.Type == XnsTokenType.IncludeName)
            {
                return Matching(tokens, document.Path, XnsTokenType.MixinName, at.Content, at.Index);
            }

            string word = WordAt(document.Text, offset, out bool variable);

            if (word == null)
            {
                return _none;
            }

            int origin = at?.Index ?? -1;

            return variable
                ? Matching(tokens, document.Path, XnsTokenType.VariableName, word, origin)
                : Matching(tokens, document.Path, XnsTokenType.ClassName, "@" + word, origin);
        }

        private static IReadOnlyList<Target> Matching(List<XnsToken> tokens, string path, XnsTokenType type, string content, int origin)
        {
            List<Target> targets = new List<Target>();

            foreach (XnsToken token in tokens)
            {
                if (token.Type == type && token.Index != origin
                    && string.Equals(token.Content, content, StringComparison.Ordinal))
                {
                    targets.Add(new Target { Path = path, Index = token.Index, Length = token.Length });
                }
            }

            return targets;
        }

        private static IReadOnlyList<Target> InView(Document document, int offset, Workspace workspace, Documents documents)
        {
            List<XnlToken> tokens = new XnlSource(document.Text).Tokenize();
            int index = IndexAt(tokens, offset);

            if (index < 0)
            {
                return _none;
            }

            XnlToken at = tokens[index];
            StyleClassTarget target;
            string name;

            switch (at.Type)
            {
                case XnlTokenType.ElementName:
                    target = StyleClassTarget.ElementName;
                    name = at.Content;
                    break;

                case XnlTokenType.ElementTypeName:
                    target = StyleClassTarget.ElementType;
                    name = at.Content;
                    break;

                case XnlTokenType.PropertyValue:
                    if (!string.Equals(PropertyBefore(tokens, index), XnlTypes.CLASS_PROPERTY, StringComparison.Ordinal))
                    {
                        return _none;
                    }

                    target = StyleClassTarget.ClassName;
                    name = WordAt(document.Text, offset, out bool _);
                    break;

                default:
                    return _none;
            }

            if (string.IsNullOrEmpty(name))
            {
                return _none;
            }

            List<Target> targets = new List<Target>();

            foreach (Sheet sheet in workspace.Sheets(documents))
            {
                if (sheet.Classes?.Classes == null)
                {
                    continue;
                }

                foreach (StyleClass style in sheet.Classes.Classes)
                {
                    if (style.Target == target && string.Equals(Bare(style.Name), name, StringComparison.Ordinal))
                    {
                        targets.Add(new Target { Path = sheet.Path, Index = style.SourceIndex, Length = style.SourceLength });
                    }
                }
            }

            return targets;
        }

        private static string PropertyBefore(List<XnlToken> tokens, int index)
        {
            for (int step = index - 1; step >= 0; step--)
            {
                XnlToken token = tokens[step];

                if (token.Type == XnlTokenType.PropertyName)
                {
                    return token.Content;
                }

                if (token.Type == XnlTokenType.PropertiesBegin || token.Type == XnlTokenType.PropertiesEnd)
                {
                    return null;
                }
            }

            return null;
        }

        private static string Bare(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            int colon = name.IndexOf(':');

            return colon < 0 ? name : name.Substring(0, colon);
        }

        private static XnsToken TokenAt(List<XnsToken> tokens, int offset)
        {
            foreach (XnsToken token in tokens)
            {
                if (offset >= token.Index && offset < token.Index + token.Length)
                {
                    return token;
                }
            }

            return null;
        }

        private static int IndexAt(List<XnlToken> tokens, int offset)
        {
            for (int index = 0; index < tokens.Count; index++)
            {
                XnlToken token = tokens[index];

                if (offset >= token.Index && offset < token.Index + token.Length)
                {
                    return index;
                }
            }

            return -1;
        }

        private static string WordAt(string text, int offset, out bool variable)
        {
            variable = false;

            if (text == null || offset < 0 || offset > text.Length)
            {
                return null;
            }

            int start = offset;

            while (start > 0 && IsWord(text[start - 1]))
            {
                start--;
            }

            int end = offset;

            while (end < text.Length && IsWord(text[end]))
            {
                end++;
            }

            if (end <= start)
            {
                return null;
            }

            variable = start > 0 && text[start - 1] == '$';

            return text.Substring(start, end - start);
        }

        private static bool IsWord(char value)
        {
            return char.IsLetterOrDigit(value) || value == '_' || value == '-';
        }
    }
}
