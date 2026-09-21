using System;

namespace Ixen.Core.Visual
{
    public interface IRegionHost
    {
        void SetRegion(Func<int> count, Func<IRegionRow> create, Action<IRegionRow, int> bind);
    }
}
