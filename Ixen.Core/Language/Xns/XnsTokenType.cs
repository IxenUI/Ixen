namespace Ixen.Core.Language.Xns
{
    internal enum XnsTokenType
    {
        None,
        Error,

        ClassIdentifier,
        BeginClassContent,
        EndClassContent,
        StyleName,
        StyleEquals,
        StyleValue,

        StyleSizeValue,
        StyleColorValue
    }
}
