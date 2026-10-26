using Ixen.Core.Visual;

namespace Ixen.Core.UT.Components.Fixtures
{
    public class RoutedPanel : VisualElement
    {
        private readonly VisualElement _inner = new VisualElement { Name = "inner" };

        public RoutedPanel()
        {
            AddChild(_inner);
        }

        public VisualElement Inner => _inner;

        protected override VisualElement ContentHost => _inner;
    }
}
