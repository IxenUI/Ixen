using Ixen.Core.Visual.Classes;
using Ixen.Platform.Mac;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Native
{
    [TestClass]
    public class MacPreferencesTests
    {
        private static int[] Colors()
            => new[] { 0xECECEC, 0x262626, 0xFFFFFF, 0x000000, 0x0064E1, 0xFFFFFF, 0xB0B0B0 };

        [TestMethod]
        public void TheSevenColoursLandInTheirSlots()
        {
            SystemPalette palette = MacPreferences.From(Colors());

            Assert.AreEqual("#ECECEC", palette.Surface);
            Assert.AreEqual("#262626", palette.Text);
            Assert.AreEqual("#FFFFFF", palette.Control);
            Assert.AreEqual("#000000", palette.ControlText);
            Assert.AreEqual("#0064E1", palette.Accent);
            Assert.AreEqual("#FFFFFF", palette.AccentText);
            Assert.AreEqual("#B0B0B0", palette.Disabled);
        }

        [TestMethod]
        public void AColourThatCouldNotBeReadGivesNoPaletteAtAll()
        {
            int[] colors = Colors();

            colors[4] = -1;

            Assert.IsNull(MacPreferences.From(colors), "half a palette must not be published");
        }

        [TestMethod]
        public void TooFewColoursGiveNoPalette()
        {
            Assert.IsNull(MacPreferences.From(new[] { 0, 0, 0 }));
            Assert.IsNull(MacPreferences.From(null));
        }

        [TestMethod]
        public void AByteAboveTheColourIsIgnored()
        {
            int[] colors = Colors();

            colors[0] = 0x7F123456;

            Assert.AreEqual("#123456", MacPreferences.From(colors).Surface);
        }
    }
}
