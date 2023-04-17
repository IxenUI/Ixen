namespace Ixen.Core.Language.Base
{
    internal abstract class BaseSource
    {
        protected SourceContent _source;

        public bool HasErrors { get; protected set; }

        public BaseSource(string source)
        {
            _source = new SourceContent(source);
        }

        public virtual void UpdateSource(string source)
        {
            _source.Content = source;
        }
    }
}
