using Ixen.Core.Language.Base;
using Ixen.Core.Language.Xns;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.StyleParser
{
    [TestClass]
    public class ContentAlignStyleTests
    {
        private static ContentAlignStyleDescriptor Parse(string value)
        {
            var xnsSource = new XnsSource($"box {{ content-align: {value} }}");
            ClassesSet set = xnsSource.Compile();

            Assert.IsFalse(xnsSource.HasErrors, string.Join(" | ", xnsSource.Diagnostics.Select(d => d.Message)));

            return (ContentAlignStyleDescriptor)set.Classes.Single().Styles.Single();
        }

        private static void AssertRejected(string value)
        {
            var xnsSource = new XnsSource($"box {{ content-align: {value} }}");
            xnsSource.Compile();

            Assert.IsTrue(xnsSource.HasErrors, $"'content-align: {value}' should have been rejected");
            Assert.AreEqual(LanguageErrorCode.INVALID_STYLE_VALUE, xnsSource.Diagnostics[0].Code);
        }

        [TestMethod]
        public void TheTwoAxesAreRecognisedByShapeInEitherOrder()
        {
            ContentAlignStyleDescriptor first = Parse("center middle");

            Assert.AreEqual(ContentAlign.Center, first.Horizontal);
            Assert.AreEqual(ContentVAlign.Middle, first.Vertical);

            ContentAlignStyleDescriptor swapped = Parse("bottom right");

            Assert.AreEqual(ContentAlign.Right, swapped.Horizontal,
                "each token names its own axis, so the order carries no meaning - the same "
                + "classify-by-shape rule border and text-align already use");
            Assert.AreEqual(ContentVAlign.Bottom, swapped.Vertical);
        }

        [TestMethod]
        public void OneValueLeavesTheOtherAxisUnset()
        {
            ContentAlignStyleDescriptor horizontal = Parse("right");

            Assert.AreEqual(ContentAlign.Right, horizontal.Horizontal);
            Assert.AreEqual(ContentVAlign.Unset, horizontal.Vertical);

            ContentAlignStyleDescriptor vertical = Parse("middle");

            Assert.AreEqual(ContentAlign.Unset, vertical.Horizontal);
            Assert.AreEqual(ContentVAlign.Middle, vertical.Vertical);
        }

        [TestMethod]
        public void UnsetIsWhatGatesTheWholeArrangePath()
        {
            Assert.IsFalse(new ContentAlignStyleDescriptor().IsDeclared,
                "an element with no rule must keep the shared default handler and pay nothing");

            Assert.IsTrue(Parse("left").IsDeclared,
                "an explicit start is still a declaration - it just happens to look the same");
        }

        [TestMethod]
        public void RepeatingAnAxisIsRejected()
        {
            AssertRejected("left right");
            AssertRejected("top bottom");
        }

        [TestMethod]
        public void ThreeValuesAreRejected()
        {
            AssertRejected("left top center");
        }

        [TestMethod]
        public void TheTextAlignVocabularyIsReusedAndNothingElseIsAccepted()
        {
            AssertRejected("start");
            AssertRejected("end");
            AssertRejected("space-between");
            AssertRejected("stretch");
            AssertRejected("centre");
        }

        [TestMethod]
        public void ItIsAcceptedOnItsOwnLineAndBesideOtherStyles()
        {
            var xnsSource = new XnsSource("box { layout: row  content-align: center middle  gap: 4px }");
            ClassesSet set = xnsSource.Compile();

            Assert.IsFalse(xnsSource.HasErrors, string.Join(" | ", xnsSource.Diagnostics.Select(d => d.Message)));

            Assert.AreEqual(1, set.Classes.Single().Styles
                .Count(s => s is ContentAlignStyleDescriptor),
                "the hyphen in the name is read by the tokenizer's style-name reader, and the "
                + "two-word value stops at the next name because no value can contain a colon");
        }
    }
}
