using Ixen.Core.Visual;
using System;

namespace Ixen.Core.UT.Components.Fixtures
{
    public class RecordingHost : VisualElement, IRegionHost
    {
        internal Func<int> Counter;
        internal Func<IRegionRow> Factory;
        internal Action<IRegionRow, int> Binder;
        internal int Handovers;

        public void SetRegion(Func<int> count, Func<IRegionRow> create, Action<IRegionRow, int> bind)
        {
            Counter = count;
            Factory = create;
            Binder = bind;
            Handovers++;
        }

        internal IRegionRow Realise(int index)
        {
            IRegionRow row = Factory();

            Binder(row, index);

            return row;
        }
    }
}
