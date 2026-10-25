using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Ixen.Core.UT.StyleScoping
{
    [TestClass]
    public class NthLastChildTests
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

        private void Build(string xns, int rows)
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

            Layout();
        }

        private void Layout()
        {
            _list.Invalidate();
            _surface.ComputeLayout(VIEWPORT, VIEWPORT);
        }

        private string Painted(string colour)
        {
            var picked = new List<string>();

            for (int index = 0; index < _list.ChildElements.Count; index++)
            {
                if (_list.ChildElements[index].StylesHandlers.Background.Descriptor?.Color == colour)
                {
                    picked.Add((index + 1).ToString());
                }
            }

            return string.Join(",", picked);
        }

        private void AssertPicks(string argument, int rows, string positions)
        {
            Build("row:nth-last-child(" + argument + ") { background: " + PICKED + " }", rows);

            Assert.AreEqual(positions, Painted(PICKED), "nth-last-child(" + argument + ") over " + rows + " rows");
        }

        [TestMethod]
        public void ANumberCountsFromTheEnd()
        {
            AssertPicks("1", 5, "5");
            AssertPicks("2", 5, "4");
        }

        [TestMethod]
        public void OddAndEvenCountFromTheEndToo()
        {
            AssertPicks("odd", 4, "2,4");
            AssertPicks("even", 4, "1,3");
        }

        [TestMethod]
        public void AFormulaPicksTheLastFew()
        {
            AssertPicks("-n+2", 6, "5,6");
            AssertPicks("2n+1", 4, "2,4");
        }

        [TestMethod]
        public void TheSameFormulaFromEitherEndIsTwoRules()
        {
            Build("row:nth-child(2n+1) { background: " + PICKED + " }\n"
                + "row:nth-last-child(2n+1) { background: " + OTHER + " }", 4);

            Assert.AreEqual("1,3", Painted(PICKED), "counted from the start");
            Assert.AreEqual("2,4", Painted(OTHER), "and from the end: the two formulas are remembered apart");
        }

        [TestMethod]
        public void AddingARowAtTheEndMovesTheSelection()
        {
            Build("row:nth-last-child(-n+2) { background: " + PICKED + " }", 3);

            Assert.AreEqual("2,3", Painted(PICKED));

            _list.AddChild(new VisualElement { Name = "row" });
            Layout();

            Assert.AreEqual("3,4", Painted(PICKED),
                "a row appended changes every earlier row's place from the end, not only its own");
        }

        [TestMethod]
        public void ItWorksInsideANegationAndACompound()
        {
            Build("row:not(:nth-last-child(1)) { background: " + PICKED + " }", 4);

            Assert.AreEqual("1,2,3", Painted(PICKED), "everything but the last");

            Build("row:nth-last-child(2):hover { background: " + PICKED + " }", 4);

            _list.ChildElements[2].ToggleState("hover", true);
            _list.ChildElements[1].ToggleState("hover", true);
            Layout();

            Assert.AreEqual("3", Painted(PICKED), "the compound needs both parts");
        }

        [TestMethod]
        public void NonsenseIsReportedUnderItsOwnName()
        {
            var source = new XnsSource("row:nth-last-child(wobble) { background: " + PICKED + " }");

            source.Compile();

            Assert.IsTrue(source.HasErrors);
            Assert.IsTrue(source.Diagnostics.Any(d => d.Message.Contains("nth-last-child(wobble)")),
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));
        }

        [TestMethod]
        public void AFormulaFromTheEndSurvivesAGeneratedStylesheet()
        {
            var registry = new StyleRegistry();

            registry.Add(new Ixen.StyleSheets.AllGeneratedStyles_StyleSheet());

            Assert.IsTrue(registry.NthLastFormulas.Any(f => f.Argument == "-n+2"));
            Assert.IsFalse(registry.NthFormulas.Any(f => f.Argument == "-n+2"),
                "and it is not mistaken for one counted from the start");
        }
    }
}
