using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.StyleParser
{
    [TestClass]
    public class NamedColorTests
    {
        [TestMethod]
        public void TheCssSpellingsAreTheNames()
        {
            Assert.IsTrue(Color.TryParse("fuchsia", out Color fuchsia));
            Assert.AreEqual(Color.Fuchsia, fuchsia);

            Assert.IsTrue(Color.TryParse("darkmagenta", out Color darkMagenta));
            Assert.AreEqual(Color.DarkMagenta, darkMagenta);
        }

        [TestMethod]
        public void TheOldMisspellingsAreGone()
        {
            Assert.IsFalse(Color.TryParse("fuschia", out _),
                "the table is built from the field names, so a misspelt field was the only "
                + "spelling a stylesheet could use");
            Assert.IsFalse(Color.TryParse("darkmagena", out _));
        }

        [TestMethod]
        public void NamesIgnoreCase()
        {
            Assert.IsTrue(Color.TryParse("DarkMagenta", out Color mixed));
            Assert.AreEqual(Color.DarkMagenta, mixed);
        }
    }
}
