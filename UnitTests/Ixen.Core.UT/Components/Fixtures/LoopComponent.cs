using Ixen.Core.Components;
using Ixen.Views;
using System.Collections.Generic;

namespace Ixen.Core.UT.Components.Fixtures
{
    public class LoopComponent : Component<LoopView>
    {
        public List<ListItem> Items { get; set; } = new List<ListItem>();
        public List<string> Words { get; set; } = new List<string>();
        public List<ListItem> Keyed { get; set; } = new List<ListItem>();
        public int Max { get; set; }

        internal void Refresh() => SetState();
    }
}
