using Ixen.Core.Components;
using Ixen.Views;
using System.Collections.Generic;

namespace Ixen.Core.UT.Components.Fixtures
{
    public class ListItem
    {
        public string Name { get; set; }
        public int Count { get; set; }
    }

    public class ListComponent : Component<ListView>
    {
        public string Title { get; set; } = "items";

        public List<ListItem> Items { get; set; } = new List<ListItem>();

        internal void Refresh() => SetState();
    }
}
