namespace Ixen.Core.Language.Base
{
    internal abstract class BaseToken
    {
        public int Index { get; set; }
        public string Content { get; set; }
        public string Message { get; set; }
        public abstract bool IsError { get; }
    }

    internal abstract class BaseToken<TTokenType, TTokenErrorType> : BaseToken
        where TTokenType : struct, System.Enum
        where TTokenErrorType : struct, System.Enum
    {
        public abstract TTokenType Type { get; set; }
        public abstract TTokenErrorType ErrorType { get; set; }
    }
}
