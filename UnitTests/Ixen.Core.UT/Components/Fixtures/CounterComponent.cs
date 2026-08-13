using Ixen.Core.Components;
using Ixen.Core.Visual;
using Ixen.Views;

namespace Ixen.Core.UT.Components.Fixtures
{
    public class CounterComponent : Component<CounterView>
    {
        public string Caption { get; set; }
        public int Step { get; set; } = 1;

        internal int Count;
        internal int Renders;
        internal string CaptionAtInit;
        internal int ChildrenAtInit;

        private VisualElement _label;

        internal VisualElement Button { get; private set; }

        protected override void OnInitialized()
        {
            CaptionAtInit = Caption;
            ChildrenAtInit = View.Children.Count;

            _label = View.FindByName("counter_label");
            Button = View.FindByName("counter_button");

            if (Button != null)
            {
                Button.PointerClick += (sender, args) => SetState(() => Count += Step);
            }
        }

        protected override void Render()
        {
            Renders++;

            if (_label != null)
            {
                _label.Text = $"{Caption} {Count}";
            }
        }
    }
}
