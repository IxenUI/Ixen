using Ixen.Core.Components;
using Ixen.Views;
using System.Collections.Generic;

namespace Ixen.Core.UT.Components.Fixtures
{
    public class LifecycleComponent : Component<LifecycleView>
    {
        public List<string> Trace { get; } = new List<string>();

        public int Attachments { get; private set; }
        public int Detachments { get; private set; }

        public bool HadHostWhenAttached { get; private set; }
        public bool HadHostWhenDetached { get; private set; }

        public string Stamp { get; private set; } = "before";

        public LifecycleInnerComponent Inner
            => View.FindByName("inner")?.Owner as LifecycleInnerComponent;

        protected override void OnInitialized() => Trace.Add("initialized");

        protected override void OnAttached()
        {
            Attachments++;
            Stamp = "after";
            HadHostWhenAttached = View.Host != null;
            Trace.Add("attached");
        }

        public void Touch() => SetState();

        protected override void OnDetached()
        {
            Detachments++;
            HadHostWhenDetached = View.Host != null;
            Trace.Add("detached");
        }
    }
}
