using Ixen.Core.Language.Xnl;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Xnl
{
    [TestClass]
    public class XnlStyleHintTests
    {
        [TestMethod]
        public void AStyleWrittenInXnlIsPointedToTheStylesheet()
        {
            string hint = XnlStyleHint.For("anchor");

            Assert.IsNotNull(hint);
            StringAssert.Contains(hint, "is a style");
            StringAssert.Contains(hint, "{ anchor: ... }");
        }

        [TestMethod]
        public void EveryStyleGetsTheHint()
        {
            Assert.IsNotNull(XnlStyleHint.For("width"));
            Assert.IsNotNull(XnlStyleHint.For("background"));
            Assert.IsNotNull(XnlStyleHint.For("anchor-placement"));
        }

        [TestMethod]
        public void AMisspelledPropertyGetsNone()
        {
            Assert.IsNull(XnlStyleHint.For("lable"));
            Assert.IsNull(XnlStyleHint.For(""));
            Assert.IsNull(XnlStyleHint.For(null));
        }
    }
}
