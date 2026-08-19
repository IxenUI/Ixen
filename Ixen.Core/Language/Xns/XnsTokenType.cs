namespace Ixen.Core.Language.Xns
{
    internal enum XnsTokenType
    {
        None,
        Error,

        ClassName,
        MediaQuery,
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
