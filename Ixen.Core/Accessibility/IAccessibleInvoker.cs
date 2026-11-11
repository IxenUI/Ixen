using Ixen.Core.Visual;

namespace Ixen.Core.Accessibility
{
    public interface IAccessibleInvoker
    {
        bool CanInvoke(VisualElement item);

        bool Invoke(VisualElement item);
    }
}
