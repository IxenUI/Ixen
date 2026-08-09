namespace Ixen.Core.Language.Base
{
    internal static class LanguageErrorCode
    {
        public const string SYNTAX = "XN001";
        public const string UNKNOWN_STYLE = "XN002";
        public const string INVALID_STYLE_VALUE = "XN003";
        public const string STRUCTURE = "XN004";
    }

    internal class LanguageError
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public int Index { get; set; }
        public int Length { get; set; }

        public LanguageError(string code, string message, int index, int length)
        {
            Code = code;
            Message = message;
            Index = index < 0 ? 0 : index;
            Length = length < 0 ? 0 : length;
        }
    }
}
