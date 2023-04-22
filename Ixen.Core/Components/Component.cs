using Ixen.Core.Visual;

namespace Ixen.Core.Components
{
    public abstract class Component
    {
        internal abstract VisualElement GetVisualElement();
    }

    public class Component<TView> : Component
        where TView : VisualElement, new()
    {
        internal TView View { get; private set; } = new() { TypeName = typeof(TView).Name };
        internal override VisualElement GetVisualElement() => View;
     }
}
