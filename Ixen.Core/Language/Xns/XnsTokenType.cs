namespace Ixen.Core.Language.Xns
{
    internal enum XnsTokenType
    {
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
