using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Ixen.Core.UT
{
    internal static class Allocations
    {
        internal const int WARMUPS = 4;

        internal static long PerPass(int passes, Action pass)
        {
            for (int index = 0; index < WARMUPS; index++)
            {
                pass();
            }

            long before = GC.GetAllocatedBytesForCurrentThread();

            for (int index = 0; index < passes; index++)
            {
                pass();
            }

            return (GC.GetAllocatedBytesForCurrentThread() - before) / passes;
        }

        internal static long Under(long budget, int passes, Action pass, string because)
        {
            long each = PerPass(passes, pass);

            Assert.IsTrue(each <= budget,
                $"{each} bytes a pass, against a budget of {budget}. {because}");

            return each;
        }
    }
}
