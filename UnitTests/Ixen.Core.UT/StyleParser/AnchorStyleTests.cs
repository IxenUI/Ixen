using Ixen.Core.Language.Base;
using Ixen.Core.Language.Xns;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.StyleParser
{
    [TestClass]
    public class AnchorStyleTests
    {
        private static T Parse<T>(string style, string value)
            where T : StyleDescriptor
        {
            var xnsSource = new XnsSource($"box {{ {style}: {value} }}");
            ClassesSet set = xnsSource.Compile();

            Assert.IsFalse(xnsSource.HasErrors, string.Join(" | ", xnsSource.Diagnostics.Select(d => d.Message)));

            return (T)set.Classes.Single().Styles.Single();
        }

        private static void AssertRejected(string style, string value)
        {
            var xnsSource = new XnsSource($"box {{ {style}: {value} }}");
            xnsSource.Compile();

            Assert.IsTrue(xnsSource.HasErrors, $"'{style}: {value}' should have been rejected");
            Assert.AreEqual(LanguageErrorCode.INVALID_STYLE_VALUE, xnsSource.Diagnostics[0].Code);
        }

        [TestMethod]
        public void AnAnchorIsAnElementName()
        {
            Assert.AreEqual("open_button", Parse<AnchorStyleDescriptor>("anchor", "open_button").Name);
            Assert.AreEqual("nav-item", Parse<AnchorStyleDescriptor>("anchor", "nav-item").Name);
        }

        [TestMethod]
        public void AnUnderscoreAndAHyphenSurviveTheTokenizer()
        {
            var xnsSource = new XnsSource("box { anchor: open_button  anchor-placement: below start }");
            ClassesSet set = xnsSource.Compile();

            Assert.IsFalse(xnsSource.HasErrors, string.Join(" | ", xnsSource.Diagnostics.Select(d => d.Message)));
            Assert.AreEqual(2, set.Classes.Single().Styles.Count);
        }

        [TestMethod]
        public void AnAnchorRefusesWhatCannotBeAnElementName()
        {
            AssertRejected("anchor", "12px");
            AssertRejected("anchor", "two names");
        }

        [TestMethod]
        public void ThePlacementDefaultsToBelowStart()
        {
            var descriptor = new AnchorPlacementStyleDescriptor();

            Assert.AreEqual(AnchorSide.Below, descriptor.Side);
            Assert.AreEqual(AnchorAlign.Start, descriptor.Align);
            Assert.IsFalse(descriptor.NoFlip);
        }

        [TestMethod]
        public void ASideAloneIsEnough()
        {
            AnchorPlacementStyleDescriptor placement =
                Parse<AnchorPlacementStyleDescriptor>("anchor-placement", "above");

            Assert.AreEqual(AnchorSide.Above, placement.Side);
            Assert.AreEqual(AnchorAlign.Start, placement.Align);
        }

        [TestMethod]
        public void AnAlignAloneIsEnoughToo()
        {
            AnchorPlacementStyleDescriptor placement =
                Parse<AnchorPlacementStyleDescriptor>("anchor-placement", "center");

            Assert.AreEqual(AnchorSide.Below, placement.Side);
            Assert.AreEqual(AnchorAlign.Center, placement.Align);
        }

        [TestMethod]
        public void TheOrderDoesNotMatter()
        {
            AnchorPlacementStyleDescriptor first =
                Parse<AnchorPlacementStyleDescriptor>("anchor-placement", "right end noflip");

            AnchorPlacementStyleDescriptor second =
                Parse<AnchorPlacementStyleDescriptor>("anchor-placement", "noflip end right");

            Assert.AreEqual(first.Side, second.Side);
            Assert.AreEqual(first.Align, second.Align);
            Assert.AreEqual(first.NoFlip, second.NoFlip);
            Assert.AreEqual(AnchorSide.Right, first.Side);
            Assert.AreEqual(AnchorAlign.End, first.Align);
            Assert.IsTrue(first.NoFlip);
        }

        [TestMethod]
        public void TwoValuesOnTheSameAxisAreRejected()
        {
            AssertRejected("anchor-placement", "below above");
            AssertRejected("anchor-placement", "start end");
            AssertRejected("anchor-placement", "noflip noflip");
        }

        [TestMethod]
        public void AFourthValueIsRejected()
        {
            AssertRejected("anchor-placement", "below start noflip extra");
        }

        [TestMethod]
        public void AnUnknownKeywordIsRejected()
        {
            AssertRejected("anchor-placement", "beneath");
        }

        [TestMethod]
        public void BothRoundTripThroughGeneratedSource()
        {
            var xnsSource = new XnsSource(
                "box { anchor: the_button  anchor-placement: left center noflip }");

            ClassesSet set = xnsSource.Compile();
            Assert.IsFalse(xnsSource.HasErrors);

            string anchor = set.Classes.Single().Styles
                .OfType<AnchorStyleDescriptor>().Single().ToSource();

            string placement = set.Classes.Single().Styles
                .OfType<AnchorPlacementStyleDescriptor>().Single().ToSource();

            StringAssert.Contains(anchor, "the_button");
            StringAssert.Contains(placement, "AnchorSide.Left");
            StringAssert.Contains(placement, "AnchorAlign.Center");
            StringAssert.Contains(placement, "NoFlip = true");
        }
    }
}
