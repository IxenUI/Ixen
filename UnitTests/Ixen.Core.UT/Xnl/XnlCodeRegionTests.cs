using Ixen.Core.Language.Base;
using Ixen.Core.Language.Xnl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Ixen.Core.UT.Xnl
{
    [TestClass]
    public class XnlCodeRegionTests
    {
        private static List<XnlToken> Tokens(string source)
            => new XnlTokenizer(source).Tokenize();

        private static XnlNode Nodify(string source, out XnlSource xnlSource)
        {
            xnlSource = new XnlSource(source);
            return xnlSource.Nodify();
        }

        [TestMethod]
        public void AHeaderIsReadUpToItsOpeningBrace()
        {
            List<XnlToken> tokens = Tokens("root {} [\r\n\t@if (Visible) {\r\n\t\tel {}\r\n\t@}\r\n]");

            XnlToken begin = tokens.First(t => t.Type == XnlTokenType.CodeRegionBegin);

            Assert.AreEqual("if (Visible)", begin.Content, "the brace is not part of the header");
            Assert.IsTrue(tokens.Any(t => t.Type == XnlTokenType.CodeRegionEnd));
        }

        [TestMethod]
        public void ABraceInsideAStringDoesNotEndTheHeader()
        {
            List<XnlToken> tokens = Tokens("root {} [\r\n\t@if (Name == \"{\") {\r\n\t\tel {}\r\n\t@}\r\n]");

            Assert.AreEqual("if (Name == \"{\")",
                tokens.First(t => t.Type == XnlTokenType.CodeRegionBegin).Content);
        }

        [TestMethod]
        public void ARegionBecomesANodeHoldingItsBody()
        {
            XnlNode root = Nodify("root {} [\r\n\t@if (Visible) {\r\n\t\tel {}\r\n\t@}\r\n]", out XnlSource source);

            Assert.IsFalse(source.HasErrors, string.Join(" | ", source.Diagnostics.Select(e => e.Message)));

            XnlNode container = root.Children[0];
            XnlNode region = container.Children[0];

            Assert.IsTrue(region.IsRegion);
            Assert.AreEqual("if (Visible)", region.Code);
            Assert.AreEqual(1, region.Children.Count);
            Assert.AreEqual("el", region.Children[0].Name);
        }

        [TestMethod]
        public void StaticSiblingsKeepTheirPlaceAroundARegion()
        {
            XnlNode root = Nodify(
                "root {} [\r\n\ta {}\r\n\t@if (V) {\r\n\t\tb {}\r\n\t@}\r\n\tc {}\r\n]", out XnlSource source);

            Assert.IsFalse(source.HasErrors);

            XnlNode container = root.Children[0];

            Assert.AreEqual(3, container.Children.Count, "the region is one child among the others");
            Assert.AreEqual("a", container.Children[0].Name);
            Assert.IsTrue(container.Children[1].IsRegion);
            Assert.AreEqual("c", container.Children[2].Name);
        }

        [TestMethod]
        public void ARegionWorksAtTopLevel()
        {
            XnlNode root = Nodify("@if (V) {\r\n\tel {}\r\n@}", out XnlSource source);

            Assert.IsFalse(source.HasErrors, string.Join(" | ", source.Diagnostics.Select(e => e.Message)));
            Assert.IsTrue(root.Children[0].IsRegion);
        }

        [TestMethod]
        public void AnEmptyRegionIsLegal()
        {
            XnlNode root = Nodify("root {} [\r\n\t@if (V) {\r\n\t@}\r\n]", out XnlSource source);

            Assert.IsFalse(source.HasErrors);
            Assert.AreEqual(0, root.Children[0].Children[0].Children.Count);
        }

        [TestMethod]
        public void AnUnclosedRegionIsReportedAtEndOfFile()
        {
            Nodify("root {} [\r\n\t@if (V) {\r\n\t\tel {}\r\n]", out XnlSource source);

            Assert.IsTrue(source.HasErrors);
            Assert.AreEqual(LanguageErrorCode.SYNTAX, source.Diagnostics[0].Code);
        }

        [TestMethod]
        public void AMarkerWithNoBraceIsASyntaxError()
        {
            Nodify("root {} [\r\n\t@if (V)\r\n]", out XnlSource source);

            Assert.IsTrue(source.HasErrors);
            Assert.AreEqual(LanguageErrorCode.SYNTAX, source.Diagnostics[0].Code);
        }

        [TestMethod]
        public void AMarkerIsNotAnElementName()
        {
            Nodify("root {} [\r\n\t@\r\n]", out XnlSource source);

            Assert.IsTrue(source.HasErrors, "'@' alone cannot start anything");
        }

        [TestMethod]
        public void AKeyClauseIsPartOfTheHeader()
        {
            List<XnlToken> tokens = Tokens(
                "root {} [\r\n\t@foreach (var row in Items) key (row.Id) {\r\n\t\tel {}\r\n\t@}\r\n]");

            Assert.AreEqual("foreach (var row in Items) key (row.Id)",
                tokens.First(t => t.Type == XnlTokenType.CodeRegionBegin).Content,
                "the header runs to the opening brace, clause included");
        }

        [TestMethod]
        public void ASemicolonEndsAStatementRatherThanABlock()
        {
            List<XnlToken> tokens = Tokens("root {} [\r\n\t@var max = 5;\r\n\tel {}\r\n]");

            Assert.AreEqual("var max = 5",
                tokens.First(t => t.Type == XnlTokenType.CodeStatement).Content);
            Assert.IsFalse(tokens.Any(t => t.Type == XnlTokenType.CodeRegionBegin),
                "a statement opens no block, so there is no @} to match");
        }

        [TestMethod]
        public void ASemicolonInsideParenthesesDoesNotEndTheHeader()
        {
            List<XnlToken> tokens = Tokens(
                "root {} [\r\n\t@for (int i = 0; i < 3; i++) {\r\n\t\tel {}\r\n\t@}\r\n]");

            Assert.AreEqual("for (int i = 0; i < 3; i++)",
                tokens.First(t => t.Type == XnlTokenType.CodeRegionBegin).Content,
                "the terminator only counts at paren depth 0");
            Assert.IsFalse(tokens.Any(t => t.Type == XnlTokenType.CodeStatement));
        }

        [TestMethod]
        public void AStatementIsANodeWithNoBody()
        {
            XnlNode root = Nodify("root {} [\r\n\t@var max = 5;\r\n\tel {}\r\n]", out XnlSource source);

            Assert.IsFalse(source.HasErrors, string.Join(" | ", source.Diagnostics.Select(e => e.Message)));

            XnlNode container = root.Children[0];

            Assert.AreEqual(2, container.Children.Count);
            Assert.IsTrue(container.Children[0].IsStatement);
            Assert.IsFalse(container.Children[0].IsRegion, "a statement is code, but not a region");
            Assert.AreEqual(0, container.Children[0].Children.Count);
            Assert.AreEqual("el", container.Children[1].Name);
        }

        [TestMethod]
        public void AnAtSignInsideAValueIsStillContent()
        {
            XnlNode root = Nodify("el { text: \"a@b\" }", out XnlSource source);

            Assert.IsFalse(source.HasErrors);
            Assert.AreEqual("a@b", root.Children[0].Properties[0].Value);
        }
    }
}
