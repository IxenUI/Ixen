using Ixen.Core.Components;
using Ixen.Views;
using System.Collections.Generic;

namespace Ixen.Core.UT.Components.Fixtures
{
    public class CommandMenuComponent : Component<CommandMenuView>
    {
        public List<Command> Commands { get; set; } = new List<Command>();

        internal void Update(System.Action change) => SetState(change);
    }
}
