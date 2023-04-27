using Ixen.Core.Language.Base;

namespace Ixen.Core.Language.Xns
{
    internal class XnsToken : BaseToken<XnsTokenType, XnsTokenErrorType>
    {
        public override bool IsError => Type == XnsTokenType.Error;
        public override XnsTokenType Type { get; set; } = XnsTokenType.None;
        public override XnsTokenErrorType ErrorType { get; set; } = XnsTokenErrorType.None;
    }
}
