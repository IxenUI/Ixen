using Ixen.Core.Components;
using Ixen.Views;
using System.Collections.Generic;

namespace Ixen.Core.UT.Components.Fixtures
{
    public class ArrayLoopComponent : Component<ArrayLoopView>
    {
        public string[] Names { get; set; } = { "swans", "lake" };

        public IReadOnlyList<ListItem> Items { get; set; } = new[] { new ListItem { Id = 1, Name = "one" } };

        internal void Update(System.Action change) => SetState(change);
    }
}
