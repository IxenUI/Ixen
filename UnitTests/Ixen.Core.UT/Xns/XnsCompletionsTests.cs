using Ixen.Core.Language.Xns;
using Ixen.Core.Visual.Styles;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.Xns
{
    [TestClass]
    public class XnsCompletionsTests
    {
        private static XnsCompletionContext At(string content)
        {
            int caret = content.IndexOf('|');
            Assert.IsTrue(caret >= 0, "the fixture must mark the caret with a pipe");

            return XnsCompletions.At(content.Remove(caret, 1), caret);
        }

        [TestMethod]
        public void InsideABlockAPartialWordAsksForAStyleName()
        {
            XnsCompletionContext context = At("el {\r\n    wid|\r\n}");

            Assert.AreEqual(XnsCompletionKind.StyleName, context.Kind);
            Assert.AreEqual(3, context.SpanLength);
            CollectionAssert.Contains(context.Items.ToArray(), StyleIdentifier.WIDTH);
        }

        [TestMethod]
        public void AtTheTopLevelThereIsNothingToPropose()
        {
            Assert.AreEqual(XnsCompletionKind.None, At("el|").Kind);
            Assert.AreEqual(XnsCompletionKind.None, At("|").Kind);
            Assert.AreEqual(XnsCompletionKind.None, At("el { width: 200px }\r\nother|").Kind);
        }

        [TestMethod]
        public void AfterAColonTheStyleDecidesTheValues()
        {
            XnsCompletionContext context = At("el {\r\n    layout: |\r\n}");

            Assert.AreEqual(XnsCompletionKind.StyleValue, context.Kind);
            Assert.AreEqual(StyleIdentifier.LAYOUT, context.StyleName);
            Assert.AreEqual(0, context.SpanLength);
            CollectionAssert.AreEquivalent(
                new[] { "row", "column", "grid", "absolute", "fixed", "dock" },
                context.Items.ToArray());
        }

        [TestMethod]
        public void APartialValueIsTheApplicableSpan()
        {
            XnsCompletionContext context = At("el {\r\n    text-overflow: ell|\r\n}");

            Assert.AreEqual(XnsCompletionKind.StyleValue, context.Kind);
            Assert.AreEqual(3, context.SpanLength);
            Assert.AreEqual(25, context.SpanStart, "the span starts at the partial value, not at the style");
        }

        [TestMethod]
        public void AKeywordIsProposedInsideACompoundValue()
        {
            XnsCompletionContext context = At("el {\r\n    border: #CCCCCC 1px in|\r\n}");

            Assert.AreEqual(XnsCompletionKind.StyleValue, context.Kind);
            Assert.AreEqual(StyleIdentifier.BORDER, context.StyleName);
            CollectionAssert.AreEquivalent(new[] { "inner", "center", "outer" }, context.Items.ToArray());
        }

        [TestMethod]
        public void TheSecondStyleOnALineIsWhatCounts()
        {
            XnsCompletionContext context = At("el { width: 200px  layout: ro| }");

            Assert.AreEqual(StyleIdentifier.LAYOUT, context.StyleName);
        }

        [TestMethod]
        public void AFreeFormStyleProposesNothing()
        {
            Assert.AreEqual(XnsCompletionKind.None, At("el {\r\n    width: |\r\n}").Kind);
            Assert.AreEqual(XnsCompletionKind.None, At("el {\r\n    background: #FF|\r\n}").Kind);
        }

        [TestMethod]
        public void AColonAfterSomethingThatIsNotAStyleIsAStateSelector()
        {
            XnsCompletionContext context = At("card {\r\n    action:|\r\n}");

            Assert.AreEqual(XnsCompletionKind.State, context.Kind);
            CollectionAssert.AreEquivalent(new[] { "hover", "pressed", "focus", "disabled" }, context.Items.ToArray());
        }

        [TestMethod]
        public void AClassSelectorTakesTheSameStates()
        {
            Assert.AreEqual(XnsCompletionKind.State, At("card {\r\n    .badge:ho|\r\n}").Kind);
            Assert.AreEqual(XnsCompletionKind.State, At("card {\r\n    #TextField:fo|\r\n}").Kind);
        }

        [TestMethod]
        public void ABraceEndsTheBackwardSearch()
        {
            XnsCompletionContext context = At("el { layout: row }\r\nother { wid| }");

            Assert.AreEqual(XnsCompletionKind.StyleName, context.Kind,
                "a colon in the previous block must not leak into the next one");
        }

        [TestMethod]
        public void NothingIsProposedInsideAComment()
        {
            Assert.AreEqual(XnsCompletionKind.None, At("el {\r\n    // wid|\r\n}").Kind);
            Assert.AreEqual(XnsCompletionKind.None, At("el {\r\n    /* layout: ro| */\r\n}").Kind);
        }

        [TestMethod]
        public void ACommentDoesNotDisturbTheDepth()
        {
            Assert.AreEqual(XnsCompletionKind.StyleName, At("// a } brace\r\nel {\r\n    wid|\r\n}").Kind);
            Assert.AreEqual(XnsCompletionKind.None, At("el { /* { */ }\r\nwid|").Kind);
        }

        [TestMethod]
        public void EveryProposedStyleNameIsAKnownStyle()
        {
            foreach (string name in XnsCompletions.StyleNames)
            {
                Assert.IsNotNull(StyleDefinitions.Find(name), name);
            }

            Assert.AreEqual(StyleDefinitions.All.Count, XnsCompletions.StyleNames.Count);
        }

        [TestMethod]
        public void AProposedValueCompilesOnceCommitted()
        {
            XnsCompletionContext context = At("el {\r\n    dock: |\r\n}");

            foreach (string item in context.Items)
            {
                var source = new XnsSource($"el {{ dock: {item} }}");
                source.Compile();

                Assert.IsFalse(source.HasErrors, item);
            }
        }

        [TestMethod]
        public void AnOutOfRangePositionIsHarmless()
        {
            Assert.AreEqual(XnsCompletionKind.None, XnsCompletions.At(null, 0).Kind);
            Assert.AreEqual(XnsCompletionKind.None, XnsCompletions.At("el {}", 99).Kind);
            Assert.AreEqual(XnsCompletionKind.None, XnsCompletions.At("el {}", -1).Kind);
        }
    }
}
