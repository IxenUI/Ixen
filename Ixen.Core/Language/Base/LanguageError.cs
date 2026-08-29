namespace Ixen.Core.Language.Base
{
    internal static class LanguageErrorCode
    {
        public const string SYNTAX = "XN001";
        public const string UNKNOWN_STYLE = "XN002";
        public const string INVALID_STYLE_VALUE = "XN003";
        public const string STRUCTURE = "XN004";
        public const string INVALID_ELEMENT_TYPE = "XN006";
        public const string INVALID_PROPERTY = "XN007";
        public const string INVALID_PROPERTY_VALUE = "XN008";
        public const string DROPPED_DEFAULT = "XN009";
    }

    internal enum LanguageErrorSeverity
    {
        Error,
        Warning
    }

    internal class LanguageError
    {
        public string Code { get; set; }
        public string Message { get; set; }
        public int Index { get; set; }
        public int Length { get; set; }
        public LanguageErrorSeverity Severity { get; set; }

        public LanguageError(string code, string message, int index, int length,
            LanguageErrorSeverity severity = LanguageErrorSeverity.Error)
        {
            Code = code;
            Message = message;
            Index = index < 0 ? 0 : index;
            Length = length < 0 ? 0 : length;
            Severity = severity;
        }
    }
}
