using Ixen.Core.Language.Base;
using Ixen.Core.Language.Xns;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.Xns
{
    [TestClass]
    public class XnsVariablesTests
    {
        private static ClassesSet Compile(string xns)
        {
            var source = new XnsSource(xns);
            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            return set;
        }

        private static StyleDescriptor Single(string xns)
            => Compile(xns).Classes.Single().Styles.Single();

        private static void AssertRejected(string xns, string expected)
        {
            var source = new XnsSource(xns);
            source.Compile();

            Assert.IsTrue(source.HasErrors, $"'{xns}' should have been rejected");
            StringAssert.Contains(
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)), expected);
        }

        [TestMethod]
        public void AVariableStandsInForAWholeValue()
        {
            var background = (BackgroundStyleDescriptor)Single(
                "$accent: #4C6EF5\r\nbox {\r\n    background: $accent\r\n}");

            Assert.AreEqual("#4C6EF5", background.Color);
        }

        [TestMethod]
        public void AVariableWorksInsideACompoundValue()
        {
            var border = (BorderStyleDescriptor)Single(
                "$line: #3C424E\r\nbox {\r\n    border: $line 1px inner\r\n}");

            Assert.AreEqual("#3C424E", border.Color);
            Assert.AreEqual(1, border.Thickness);
            Assert.AreEqual(BorderType.Inner, border.Type);
        }

        [TestMethod]
        public void SeveralVariablesInOneValueAllExpand()
        {
            var margin = (MarginStyleDescriptor)Single(
                "$small: 4px\r\n$big: 20px\r\nbox {\r\n    margin: $small $big\r\n}");

            Assert.AreEqual(4, margin.Top.Value);
            Assert.AreEqual(20, margin.Right.Value);
        }

        [TestMethod]
        public void AVariableNamesAMediaCondition()
        {
            ClassesSet set = Compile(
                "$phone: (max-width: 600px)\r\n"
                + "@media $phone {\r\n    box {\r\n        width: 50px\r\n    }\r\n}");

            StyleClass box = set.Classes.Single();

            Assert.IsNotNull(box.Media);
            Assert.IsTrue(box.Media.Matches(500, 800));
            Assert.IsFalse(box.Media.Matches(700, 800));
        }

        [TestMethod]
        public void AVariableMayReferAnother()
        {
            var background = (BackgroundStyleDescriptor)Single(
                "$blue: #4C6EF5\r\n$accent: $blue\r\nbox {\r\n    background: $accent\r\n}");

            Assert.AreEqual("#4C6EF5", background.Color);
        }

        [TestMethod]
        public void DeclarationOrderDoesNotMatter()
        {
            var background = (BackgroundStyleDescriptor)Single(
                "box {\r\n    background: $accent\r\n}\r\n$accent: #4C6EF5");

            Assert.AreEqual("#4C6EF5", background.Color,
                "every declaration is collected before anything is compiled");
        }

        [TestMethod]
        public void ADashOrUnderscoreIsLegalInAName()
        {
            var background = (BackgroundStyleDescriptor)Single(
                "$surface-2: #2E3138\r\nbox {\r\n    background: $surface-2\r\n}");

            Assert.AreEqual("#2E3138", background.Color);
        }

        [TestMethod]
        public void ATrailingCommentIsNotPartOfTheValue()
        {
            var background = (BackgroundStyleDescriptor)Single(
                "$accent: #4C6EF5  // the brand blue\r\nbox {\r\n    background: $accent\r\n}");

            Assert.AreEqual("#4C6EF5", background.Color);
        }

        [TestMethod]
        public void AnUndeclaredNameIsReported()
        {
            AssertRejected("box {\r\n    background: $missing\r\n}", "$missing");
        }

        [TestMethod]
        public void AnUndeclaredNameInAConditionIsReported()
        {
            AssertRejected("@media $missing {\r\n    box {\r\n        width: 1px\r\n    }\r\n}", "$missing");
        }

        [TestMethod]
        public void ASelfReferenceIsReportedRatherThanLooping()
        {
            AssertRejected("$loop: $loop\r\nbox {\r\n    background: $loop\r\n}", "refers to itself");
        }

        [TestMethod]
        public void AMutualReferenceIsReportedRatherThanLooping()
        {
            AssertRejected("$a: $b\r\n$b: $a\r\nbox {\r\n    background: $a\r\n}", "refers to itself");
        }

        [TestMethod]
        public void ADeclarationInsideABlockIsReported()
        {
            AssertRejected("box {\r\n    $accent: #4C6EF5\r\n}", "top level");
        }

        [TestMethod]
        public void ASheetWithoutVariablesIsUnaffected()
        {
            var background = (BackgroundStyleDescriptor)Single("box {\r\n    background: #FF0000\r\n}");

            Assert.AreEqual("#FF0000", background.Color);
        }

        [TestMethod]
        public void ALoneDollarIsStillASyntaxError()
        {
            var source = new XnsSource("$ {\r\n    background: #FF0000\r\n}");
            source.Compile();

            Assert.IsTrue(source.HasErrors);
            Assert.AreEqual(LanguageErrorCode.SYNTAX, source.Diagnostics[0].Code);
        }

        [TestMethod]
        public void TheVariableReachesTheResolvedStyle()
        {
            var source = new XnsSource(
                "$accent: #4C6EF5\r\n$phone: (max-width: 600px)\r\n"
                + "box {\r\n    background: #111111\r\n}\r\n"
                + "@media $phone {\r\n    box {\r\n        background: $accent\r\n    }\r\n}");

            ClassesSet set = source.Compile();

            Assert.IsFalse(source.HasErrors,
                string.Join(" | ", source.Diagnostics.Select(d => d.Message)));

            var registry = new StyleRegistry();
            registry.Add(set);

            var box = new VisualElement { Name = "box" };
            box.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            var surface = new IxenSurface(box) { Styles = registry };
            box.Invalidate();
            surface.ComputeLayout(500, 800);

            Assert.AreEqual("#4C6EF5", box.StylesHandlers.Background.Descriptor?.Color,
                "a variable and a breakpoint together, end to end");

            surface.ComputeLayout(900, 800);

            Assert.AreEqual("#111111", box.StylesHandlers.Background.Descriptor?.Color);
        }
    }
}
