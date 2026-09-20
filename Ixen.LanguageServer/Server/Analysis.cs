using System.Collections.Generic;
using Ixen.Core.Language.Base;
using Ixen.Core.Language.Xnl;
using Ixen.Core.Language.Xns;

namespace Ixen.LanguageServer.Server
{
    internal static class Analysis
    {
        private static readonly LanguageError[] _none = new LanguageError[0];

        public static IReadOnlyList<LanguageError> Diagnose(string path, string text)
        {
            if (Uris.IsXns(path))
            {
                XnsSource source = new XnsSource(text ?? string.Empty);
                source.Compile();

                return source.Diagnostics;
            }

            if (Uris.IsXnl(path))
            {
                XnlSource source = new XnlSource(text ?? string.Empty);
                source.Nodify();

                return source.Diagnostics;
            }

            return _none;
        }
    }
}
