namespace Ixen
{
    internal abstract class IxenApplicationBase
    {
        protected IxenApplicationInitOptions _initOptions;

        public abstract string Title { get; set; }

        public IxenApplicationBase(IxenApplicationInitOptions initOptions = null)
        {
            _initOptions = initOptions ?? new IxenApplicationInitOptions();
        }
    }
}
