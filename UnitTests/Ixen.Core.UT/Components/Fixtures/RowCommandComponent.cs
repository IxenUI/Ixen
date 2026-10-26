using Ixen.Core.Components;
using Ixen.Views;
using System.Collections.Generic;

namespace Ixen.Core.UT.Components.Fixtures
{
    public class RowCommandComponent : Component<RowCommandView>
    {
        public RowCommandComponent()
        {
            Remove = new Command<ListItem>(row => SetState(() => Rows.Remove(row)), row => row.Count > 0);
            Echo = new Command<string>(text => Echoed = text);
        }

        public List<ListItem> Rows { get; set; } = new List<ListItem>();

        public Command<ListItem> Remove { get; }

        public Command<string> Echo { get; }

        public string Echoed { get; private set; }

        internal void Update(System.Action change) => SetState(change);
    }
}
