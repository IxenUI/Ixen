using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class WindowPlacementTests
    {
        [TestMethod]
        public void APlacementRoundTripsThroughItsText()
        {
            var placement = new WindowPlacement(-1200, 80, 1100, 700, true);

            string text = placement.ToString();
            WindowPlacement back = WindowPlacement.Parse(text);

            Assert.AreEqual("-1200 80 1100 700 maximized", text,
                "invariant digits, so a file saved on a French machine reads back anywhere; a "
                + "negative x is a monitor left of the main one");
            Assert.AreEqual(-1200, back.X);
            Assert.AreEqual(80, back.Y);
            Assert.AreEqual(1100, back.Width);
            Assert.AreEqual(700, back.Height);
            Assert.IsTrue(back.Maximized);
        }

        [TestMethod]
        public void WhatIsNotAPlacementReadsAsNone()
        {
            foreach (string text in new[] { null, "", "  ", "1 2 3", "1 2 3 4 big", "1 2 0 4 normal", "a 2 3 4 normal" })
            {
                Assert.IsNull(WindowPlacement.Parse(text),
                    $"'{text}' - a damaged settings file opens the window where it would have opened "
                    + "anyway, rather than throwing at start-up");
            }
        }
    }
}
