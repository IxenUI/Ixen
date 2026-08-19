namespace Ixen.Core.Language.Xns
{
    internal enum XnsTokenType
    {
        None,
        Error,

        ClassName,
        MediaQuery,
        VariableName,
        VariableValue,
        BeginClassContent,
        EndClassContent,
        StyleName,
        StyleEquals,
        StyleValue,

        StyleSizeValue,
        StyleColorValue,

        Comment
    }
}
