using Ixen.Core.Components;
using Ixen.Views;

namespace Ixen.Core.UT.Components.Fixtures
{
    public class LifecycleInnerComponent : Component<LifecycleInnerView>
    {
        public int Attachments { get; private set; }
        public int Detachments { get; private set; }

        protected override void OnAttached() => Attachments++;
        protected override void OnDetached() => Detachments++;
    }
}
