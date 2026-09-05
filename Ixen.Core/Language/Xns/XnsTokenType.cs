namespace Ixen.Core.Language.Xns
{
    internal enum XnsTokenType
    {
        None,
        Error,

        ClassName,
        MediaQuery,
        ContainerQuery,
        VariableName,
        VariableValue,
        MixinName,
        IncludeName,
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
