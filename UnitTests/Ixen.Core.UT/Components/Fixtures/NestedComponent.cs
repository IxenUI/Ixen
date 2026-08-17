using Ixen.Core.Components;
using Ixen.Views;
using System.Collections.Generic;

namespace Ixen.Core.UT.Components.Fixtures
{
    public class NestedComponent : Component<NestedView>
    {
        public List<ListItem> Items { get; set; } = new List<ListItem>();
        public List<string> Words { get; set; } = new List<string>();
        public bool ShowDeep { get; set; }
        public ListItem Picked { get; set; }

        public void Pick(ListItem item) => Picked = item;

        internal void Refresh() => SetState();
    }
}
