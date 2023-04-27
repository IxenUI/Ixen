namespace Ixen.Core.Language.Xnl
{
    internal enum XnlTokenType
    {
        None,
        Error,

        ElementName,
        ElementTypeBegin,
        ElementTypeName,
        ElementTypeEnd,

        PropertiesBegin,
        PropertiesEnd,

        PropertyName,
        PropertyEqual,
        PropertyValueBegin,
        PropertyValue,
        PropertyValueEnd,

        ChildrenBegin,
        ChildrenEnd,

        Comment
    }
}
