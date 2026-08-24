using Ixen.Core.Components;
using Ixen.Views;

namespace Ixen.Core.UT.Components.Fixtures
{
    public class LifecycleHostComponent : Component<LifecycleHostView>
    {
        public bool Show { get; set; }

        internal void Refresh() => SetState();
    }
}
