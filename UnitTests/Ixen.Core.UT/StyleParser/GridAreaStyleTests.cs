using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.Core.Visual.Styles.Parsers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.StyleParser
{
    [TestClass]
    public class GridAreaStyleTests
    {
        private static AreaTemplateStyleDescriptor Template(string value)
        {
            var parser = new AreaTemplateStyleParser(value);

            Assert.IsTrue(parser.IsValid, $"'{value}' should have parsed");

            return parser.Descriptor;
        }

        private static void AssertRefused(string value)
        {
            Assert.IsFalse(new AreaTemplateStyleParser(value).IsValid,
                $"'{value}' should have been refused");
        }

        [TestMethod]
        public void ARowPerSeparatorAndACellPerWord()
        {
            AreaTemplateStyleDescriptor descriptor = Template("head head / nav main");

            Assert.AreEqual(2, descriptor.RowCount);
            Assert.AreEqual(2, descriptor.ColumnCount);
            Assert.AreEqual("head", descriptor.Rows[0][0]);
            Assert.AreEqual("main", descriptor.Rows[1][1]);
        }

        [TestMethod]
        public void OneRowIsAGridToo()
        {
            AreaTemplateStyleDescriptor descriptor = Template("nav main aside");

            Assert.AreEqual(1, descriptor.RowCount);
            Assert.AreEqual(3, descriptor.ColumnCount);
        }

        [TestMethod]
        public void AnEmptyCellIsADot()
        {
            AreaTemplateStyleDescriptor descriptor = Template("head head / . main");

            Assert.AreEqual(AreaTemplateStyleDescriptor.EMPTY, descriptor.Rows[1][0]);
            CollectionAssert.AreEquivalent(new[] { "head", "main" }, descriptor.Names().ToList());
        }

        [TestMethod]
        public void EveryRowMustHaveTheSameNumberOfCells()
        {
            AssertRefused("head head / nav main aside");
        }

        [TestMethod]
        public void AnEmptyRowIsRefused()
        {
            AssertRefused("head head / / nav main");
        }

        [TestMethod]
        public void AnEmptyValueIsRefused()
        {
            AssertRefused("   ");
        }

        [TestMethod]
        public void ACellMustStartWithALetterOrAnUnderscore()
        {
            AssertRefused("1head 1head");
        }

        [TestMethod]
        public void ACellMayHoldDigitsHyphensAndUnderscores()
        {
            AreaTemplateStyleDescriptor descriptor = Template("side_bar-2 side_bar-2");

            Assert.AreEqual(1, descriptor.RowCount);
            Assert.AreEqual(2, descriptor.ColumnCount);
        }

        [TestMethod]
        public void ACellHoldingAnythingElseIsRefused()
        {
            AssertRefused("head! head!");
        }

        [TestMethod]
        public void AnLShapedAreaIsRefused()
        {
            AssertRefused("head head / head .");
        }

        [TestMethod]
        public void ASplitAreaIsRefusedToo()
        {
            AssertRefused("head . head / . . .");
        }

        [TestMethod]
        public void ARectangleSpanningBothAxesIsAccepted()
        {
            AreaTemplateStyleDescriptor descriptor = Template("main main / main main");

            Assert.IsTrue(descriptor.TryFind("main", out int row, out int column,
                out int rowSpan, out int columnSpan));

            Assert.AreEqual(0, row);
            Assert.AreEqual(0, column);
            Assert.AreEqual(2, rowSpan);
            Assert.AreEqual(2, columnSpan);
        }

        [TestMethod]
        public void ASingleCellSpansOneOfEach()
        {
            AreaTemplateStyleDescriptor descriptor = Template("head nav");

            Assert.IsTrue(descriptor.TryFind("nav", out int row, out int column,
                out int rowSpan, out int columnSpan));

            Assert.AreEqual(0, row);
            Assert.AreEqual(1, column);
            Assert.AreEqual(1, rowSpan);
            Assert.AreEqual(1, columnSpan);
        }

        [TestMethod]
        public void ANameThatIsNotThereIsNotFound()
        {
            Assert.IsFalse(Template("head nav").TryFind("foot",
                out _, out _, out _, out _));
        }

        [TestMethod]
        public void AnUndeclaredTemplateSaysSo()
        {
            Assert.IsFalse(new AreaTemplateStyleDescriptor().IsDeclared);
            Assert.AreEqual(0, new AreaTemplateStyleDescriptor().ColumnCount);
        }

        [TestMethod]
        public void AGridAreaIsOneName()
        {
            var parser = new GridAreaStyleParser("  nav  ");

            Assert.IsTrue(parser.IsValid);
            Assert.AreEqual("nav", parser.Descriptor.Value);
            Assert.IsTrue(parser.Descriptor.IsDeclared);
        }

        [TestMethod]
        public void AGridAreaRefusesTwoNames()
        {
            Assert.IsFalse(new GridAreaStyleParser("nav main").IsValid);
        }

        [TestMethod]
        public void AGridAreaRefusesADot()
        {
            Assert.IsFalse(new GridAreaStyleParser(".").IsValid);
        }

        [TestMethod]
        public void AGridAreaRefusesNothing()
        {
            Assert.IsFalse(new GridAreaStyleParser("").IsValid);
        }
    }
}
