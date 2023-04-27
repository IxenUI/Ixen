using Ixen.Core.Language.Base;

namespace Ixen.Core.Language.Xnl
{
    internal class XnlToken : BaseToken<XnlTokenType, XnlTokenErrorType>
    {
        public override bool IsError => Type == XnlTokenType.Error;
        public override XnlTokenType Type { get; set; } = XnlTokenType.None;
        public override XnlTokenErrorType ErrorType { get; set; } = XnlTokenErrorType.None;
    }
}
