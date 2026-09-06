using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Ixen.Core.UT.StyleScoping
{
    [TestClass]
    public class NthFormulaTests
    {
        private const int VIEWPORT = 200;
        private const string PICKED = "#4C6EF5";
        private const string OTHER = "#E8590C";

        private StyleRegistry _registry;
        private IxenSurface _surface;
        private VisualElement _list;

        private static ClassesSet Compile(string xns)
        {
            var source = new XnsSource(xns);
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            return set;
        }

        private VisualElement Build(string xns, int rows)
        {
            _registry = new StyleRegistry();
            _registry.Add(Compile(xns));

            _list = new VisualElement { Name = "list" };
            _list.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            for (int index = 0; index < rows; index++)
            {
                _list.AddChild(new VisualElement { Name = "row" });
            }

            _surface = new IxenSurface(_list) { Styles = _registry };

            _list.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            return _list;
        }

        private static string BackgroundOf(VisualElement element)
            => element.StylesHandlers.Background.Descriptor?.Color;

        private string Picked(int rows)
        {
            var picked = new List<string>();

            for (int index = 0; index < rows; index++)
            {
                if (BackgroundOf(_list.ChildElements[index]) == PICKED)
                {
                    picked.Add((index + 1).ToString());
                }
            }

            return string.Join(",", picked);
        }

        private void AssertPicks(string argument, int rows, string positions)
        {
            Build("row:nth-child(" + argument + ") { background: " + PICKED + " }", rows);

            Assert.AreEqual(positions, Picked(rows),
                "nth-child(" + argument + ") over " + rows + " rows");
        }

        [TestMethod]
        public void AStepAloneStripesEveryOtherRow()
            => AssertPicks("2n", 6, "2,4,6");

        [TestMethod]
        public void AStepAndAnOffsetTakeTheOtherHalf()
            => AssertPicks("2n+1", 6, "1,3,5");

        [TestMethod]
        public void ANegativeOffsetIsReadAsAnOffsetAndNotAsRubbish()
            => AssertPicks("2n-1", 6, "1,3,5");

        [TestMethod]
        public void AStepOfOneFromAnOffsetIsEverythingAfterIt()
            => AssertPicks("n+3", 6, "3,4,5,6");

        [TestMethod]
        public void AStepOfOneAloneIsEveryRow()
            => AssertPicks("n", 4, "1,2,3,4");

        [TestMethod]
        public void ANegativeStepCountsBackTowardsTheOffset()
            => AssertPicks("-n+3", 6, "1,2,3");

        [TestMethod]
        public void AStepOfThreeSkipsTwoRowsAtATime()
            => AssertPicks("3n+1", 7, "1,4,7");

        [TestMethod]
        public void AStepOfZeroIsAnExactPosition()
            => AssertPicks("0n+3", 5, "3");

        [TestMethod]
        public void AFormulaThatReachesNothingPicksNothing()
            => AssertPicks("-2n-3", 6, "");

        [TestMethod]
        public void SpacesAroundTheOperatorsDoNotMatter()
        {
            Build("row:nth-child(2n + 1) { background: " + PICKED + " }", 5);

            Assert.AreEqual("1,3,5", Picked(5),
                "a selector is compacted before it becomes a key, so 2n + 1 and 2n+1 are one "
                + "rule rather than two that each match nothing");
        }

        [TestMethod]
        public void AnExactPositionBeatsAFormulaThatAlsoMatches()
        {
            Build("row:nth-child(2n+1) { background: " + OTHER + " }"
                + " row:nth-child(3) { background: " + PICKED + " }", 5);

            Assert.AreEqual(PICKED, BackgroundOf(_list.ChildElements[2]),
                "position 3 satisfies both, and the formulas are applied before the exact index "
                + "so that naming a position outright is the more specific thing to say");
            Assert.AreEqual(OTHER, BackgroundOf(_list.ChildElements[0]),
                "and row 1 still takes the formula");
        }

        [TestMethod]
        public void TheFormulaDeclaredLastWinsWhereTwoOverlap()
        {
            Build("row:nth-child(2n) { background: " + OTHER + " }"
                + " row:nth-child(4n) { background: " + PICKED + " }", 8);

            Assert.AreEqual(PICKED, BackgroundOf(_list.ChildElements[3]),
                "position 4 satisfies both formulas, and the list keeps declaration order, so "
                + "the one written later wins the property");
            Assert.AreEqual(OTHER, BackgroundOf(_list.ChildElements[1]),
                "position 2 satisfies only the first");
        }

        [TestMethod]
        public void AFormulaTurnsTheStructuralPathOn()
        {
            var registry = new StyleRegistry();

            registry.Add(Compile("row:nth-child(2n+1) { background: " + PICKED + " }"));

            Assert.IsTrue(registry.HasStructuralClasses,
                "a sheet whose only structural mention is a formula still has to be walked, or "
                + "nothing ever builds the candidate");
            Assert.AreEqual(1, registry.NthFormulas.Count);
        }

        [TestMethod]
        public void TheSameFormulaTwiceIsRememberedOnce()
        {
            var registry = new StyleRegistry();

            registry.Add(Compile("row:nth-child(2n+1) { background: " + PICKED + " }"
                + " chip:nth-child(2n+1) { background: " + OTHER + " }"));

            Assert.AreEqual(1, registry.NthFormulas.Count,
                "the list is walked per element per selector, so it holds the distinct arguments "
                + "rather than one entry per rule that mentions them");
        }

        [TestMethod]
        public void AnExactPositionIsNotRememberedAsAFormula()
        {
            var registry = new StyleRegistry();

            registry.Add(Compile("row:nth-child(3) { background: " + PICKED + " }"));

            Assert.AreEqual(0, registry.NthFormulas.Count,
                "an exact index needs no list: the candidate is built from the element's own "
                + "position, which is the dictionary-key trick the rest of the structural "
                + "pseudo-classes rest on");
        }

        [TestMethod]
        public void AFormulaWorksInAScopeSegment()
        {
            _registry = new StyleRegistry();
            _registry.Add(Compile("group:nth-child(2n) { label { background: " + PICKED + " } }"));

            var root = new VisualElement { Name = "root" };
            root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            for (int index = 0; index < 4; index++)
            {
                var group = new VisualElement { Name = "group" };
                group.AddChild(new VisualElement { Name = "label" });
                root.AddChild(group);
            }

            _surface = new IxenSurface(root) { Styles = _registry };

            root.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);

            Assert.IsNull(BackgroundOf(root.ChildElements[0].ChildElements[0]));
            Assert.AreEqual(PICKED, BackgroundOf(root.ChildElements[1].ChildElements[0]),
                "a scope segment tests the formula through the same Holds a bare selector does");
            Assert.IsNull(BackgroundOf(root.ChildElements[2].ChildElements[0]));
            Assert.AreEqual(PICKED, BackgroundOf(root.ChildElements[3].ChildElements[0]));
        }

        [TestMethod]
        public void AFormulaWorksInsideANegation()
        {
            Build("row:not(:nth-child(2n)) { background: " + PICKED + " }", 6);

            Assert.AreEqual("1,3,5", Picked(6),
                "a negation carries segments, and a segment tests a formula the same way, so the "
                + "two features compose with nothing wired between them");
        }

        [TestMethod]
        public void NonsenseIsReportedRatherThanMatchingNothing()
        {
            var source = new XnsSource("row:nth-child(wobble) { background: " + PICKED + " }");

            source.Compile();

            Assert.IsTrue(source.HasErrors,
                "an argument the grammar cannot read used to compile, enter the registry and "
                + "match nothing for ever - which is exactly the trap this round was picked for");
            Assert.IsTrue(source.Diagnostics.Any(d => d.Message.Contains("2n+1")),
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));
        }

        [TestMethod]
        public void AHalfWrittenFormulaIsReportedToo()
        {
            var source = new XnsSource("row:nth-child(2n+) { background: " + PICKED + " }");

            source.Compile();

            Assert.IsTrue(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));
        }

        [TestMethod]
        public void ABareNumberAfterTheStepIsRefused()
        {
            var source = new XnsSource("row:nth-child(2n3) { background: " + PICKED + " }");

            source.Compile();

            Assert.IsTrue(source.HasErrors,
                "an offset needs its own sign, or 2n3 would silently read as an offset of three");
        }

        [TestMethod]
        public void APlusOutsideAParenthesisIsStillASyntaxError()
        {
            var source = new XnsSource("row+label { background: " + PICKED + " }");

            source.Compile();

            Assert.IsTrue(source.HasErrors,
                "the plus is accepted only inside the parentheses, so a sibling combinator stays "
                + "a clean diagnostic instead of becoming a selector that matches nothing - and "
                + "that is what keeps the notation free for a real one later");
        }

        [TestMethod]
        public void AFormulaSurvivesAGeneratedStylesheet()
        {
            var registry = new StyleRegistry();

            registry.Add(new Ixen.StyleSheets.AllGeneratedStyles_StyleSheet());

            Assert.IsTrue(registry.NthFormulas.Any(f => f.Argument == "2n+1"),
                "the fixture carries one, so a formula has to round-trip through the generated "
                + "source as well as through a fresh compile");
        }
    }
}
