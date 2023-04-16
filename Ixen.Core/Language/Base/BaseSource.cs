namespace Ixen.Core.Language.Base
{
    internal abstract class BaseSource
    {
        protected string _source;

        public bool HasErrors { get; protected set; }

        public BaseSource(string source)
        {
            _source = source;
        }

        public BaseSource(ref string source)
        {
            _source = source;
        }
    }
}
