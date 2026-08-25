using Ixen.Core.Components;
using Ixen.Views;
using System.Collections.Generic;

namespace Ixen.Core.UT.Components.Fixtures
{
    public class NamedSlotHostComponent : Component<NamedSlotHostView>
    {
        public string Caption { get; set; } = "titled";
        public bool Flag { get; set; }
        public int Bumps { get; private set; }

        public void Bump() => Bumps++;

        internal void Refresh() => SetState();
    }
}
