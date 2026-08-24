using Ixen.Core.Components;
using Ixen.Views;
using System.Collections.Generic;

namespace Ixen.Core.UT.Components.Fixtures
{
    public class SlotHostComponent : Component<SlotHostView>
    {
        public string Caption { get; set; } = "bound";
        public bool Flag { get; set; }
        public List<string> Words { get; set; } = new List<string>();
        public int Bumps { get; private set; }

        public void Bump() => Bumps++;

        internal void Refresh() => SetState();
    }
}
