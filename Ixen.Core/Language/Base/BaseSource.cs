using System.Collections.Generic;
using System.Linq;

namespace Ixen.Core.Language.Base
{
    internal abstract class BaseSource
    {
        protected SourceContent _source;
        protected List<LanguageError> _diagnostics = new();

        public IReadOnlyList<LanguageError> Diagnostics => _diagnostics;
        public bool HasErrors => _diagnostics.Any(d => d.Severity == LanguageErrorSeverity.Error);

        public BaseSource(string source)
        {
            _source = new SourceContent(source);
        }

        public virtual void UpdateSource(string source)
        {
            _source.Content = source;
        }

        protected void AddError(string code, string message, int index, int length)
            => _diagnostics.Add(new LanguageError(code, message, index, length));
    }
}
