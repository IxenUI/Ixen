using System.Collections.Generic;

namespace Ixen.Core.Language.Base
{
    internal abstract class BaseSource
    {
        protected SourceContent _source;
        protected List<LanguageError> _errors = new();

        public IReadOnlyList<LanguageError> Errors => _errors;
        public bool HasErrors => _errors.Count > 0;

        public BaseSource(string source)
        {
            _source = new SourceContent(source);
        }

        public virtual void UpdateSource(string source)
        {
            _source.Content = source;
        }

        protected void AddError(string code, string message, int index, int length)
            => _errors.Add(new LanguageError(code, message, index, length));
    }
}
