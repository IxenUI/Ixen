using Ixen.Core.Visual;
using System;

namespace Ixen.Core.UT.Components.Fixtures
{
    public class RecordingHost : VisualElement, IRegionHost
    {
        internal Func<int> Counter;
        internal Func<IRegionRow> Factory;
        internal Action<IRegionRow, int> Binder;
        internal Func<int, object> Keys;
        internal int Handovers;

        public void SetRegion(Func<int> count, Func<IRegionRow> create, Action<IRegionRow, int> bind,
            Func<int, object> keyOf)
        {
            Counter = count;
            Factory = create;
            Binder = bind;
            Keys = keyOf;
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
