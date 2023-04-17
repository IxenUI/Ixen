namespace Ixen.Core.Language.Xnl
{
    internal enum XnlTokenType
    {
        None,
        Error,

        ElementName,
        ElementTypeEquals,
        ElementTypeName,

        BeginParams,
        EndParams,

        ParamName,
        ParamEquals,
        ParamValueBegin,
        ParamValue,

        BeginContent,
        EndContent,
    }
}
