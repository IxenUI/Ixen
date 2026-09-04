using Ixen.Core.Components;
using Ixen.Core.Visual;
using Ixen.Views;

namespace Ixen.Core.UT.Components.Fixtures
{
    public class GranularityComponent : Component<GranularityView>
    {
        internal int CaptionReads;

        public string Caption
        {
            get
            {
                CaptionReads++;
                return "parent";
            }
        }

        internal CounterComponent Child(string name)
            => View.FindByName(name)?.Owner as CounterComponent;
    }
}
