using Ixen.Core.Components;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.Views;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Xnl
{
    [TestClass]
    public class XnlTypedValuesTests
    {
        private static XnlTestWidget Widget()
            => (XnlTestWidget)new Component<TypedValuesView>().View.Children[0].Children[0];

        [TestMethod]
        public void TheDeclaredTypeIsInstantiated()
        {
            Assert.IsInstanceOfType<XnlTestWidget>(Widget());
        }

        [TestMethod]
        public void StringValuesStillWork()
        {
            Assert.AreEqual("Coucou", Widget().Label);
        }

        [TestMethod]
        public void BackslashesAreEscapedRatherThanInterpreted()
        {
            Assert.AreEqual(@"C:\temp\new", Widget().Path,
                "a raw interpolation would have turned \\t and \\n into a tab and a newline");
        }

        [TestMethod]
        public void IntegerValuesAreConverted()
        {
            Assert.AreEqual(42, Widget().Count);
        }

        [TestMethod]
        public void BooleanValuesAreConverted()
        {
            Assert.IsTrue(Widget().Enabled);
        }

        [TestMethod]
        public void FloatValuesAreConverted()
        {
            Assert.AreEqual(1.5f, Widget().Ratio);
        }

        [TestMethod]
        public void DoubleValuesAreConverted()
        {
            Assert.AreEqual(2.25d, Widget().Precision);
        }

        [TestMethod]
        public void DecimalValuesAreConverted()
        {
            Assert.AreEqual(9.99m, Widget().Amount);
        }

        [TestMethod]
        public void CharValuesAreConverted()
        {
            Assert.AreEqual('K', Widget().Initial);
        }

        [TestMethod]
        public void EnumValuesAreConvertedByMemberName()
        {
            Assert.AreEqual(LayoutType.Row, Widget().Direction);
        }

        [TestMethod]
        public void NullableValuesAreConverted()
        {
            Assert.AreEqual(7, Widget().Optional);
        }
    }
}
