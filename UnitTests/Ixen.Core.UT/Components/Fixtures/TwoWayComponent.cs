using Ixen.Core.Components;
using Ixen.Views;

namespace Ixen.Core.UT.Components.Fixtures
{
    public class TwoWayInner
    {
        public string Label { get; set; } = "inner";
    }

    public class TwoWayComponent : Component<TwoWayView>
    {
        public string Name { get; set; } = "start";
        public TwoWayInner Inner { get; set; } = new TwoWayInner();

        public string Echo => $"[{Name}]";
    }
}
