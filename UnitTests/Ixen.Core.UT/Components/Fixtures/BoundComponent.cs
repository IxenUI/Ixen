using Ixen.Core.Components;
using Ixen.Views;

namespace Ixen.Core.UT.Components.Fixtures
{
    public class BoundInner
    {
        public string Name { get; set; }
    }

    public class BoundComponent : Component<BoundView>
    {
        public string Caption { get; set; } = "hello";
        public int Count { get; set; }
        public int Total { get; set; } = 3;
        public bool IsEditable { get; set; }
        public BoundInner Inner { get; set; } = new BoundInner { Name = "inner" };

        public string Describe(int value) => $"n={value}";

        internal void Advance() => SetState(() => Count++);
    }
}
