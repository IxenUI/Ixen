using Ixen.Core.Language.Xns;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Ixen.Core.UT.Xns
{
    [TestClass]
    public class XnsTokenizerTests
    {
        [TestMethod]
        public void TestTokenize()
        {
            string source = @"container {
    layout: row
    width: 100%
    /* test */
    panel {
        width: 50px
        background: #222222
    }
    
    content {
        width: 1*
        background: #EEEEEE
        padding: 5px
    }
    
    entries {
        layout: column
        
        entry {
            
        }
    }
}

.active {
    background: #FF2222
}";

            var xnsSource = new XnsSource(source);
            var tokens = xnsSource.Tokenize();

            Assert.AreEqual(46, tokens.Count);

            Assert.AreEqual(0, tokens[0].Index);
            Assert.AreEqual("container", tokens[0].Content);
            Assert.AreEqual(XnsTokenType.ClassName, tokens[0].Type);

            Assert.AreEqual(XnsTokenType.BeginClassContent, tokens[1].Type);

            Assert.AreEqual(XnsTokenType.StyleName, tokens[2].Type);
            Assert.AreEqual("layout", tokens[2].Content);
            Assert.AreEqual(XnsTokenType.StyleEquals, tokens[3].Type);
            Assert.AreEqual(XnsTokenType.StyleValue, tokens[4].Type);
            Assert.AreEqual("row", tokens[4].Content);

            Assert.AreEqual(XnsTokenType.StyleName, tokens[5].Type);
            Assert.AreEqual("width", tokens[5].Content);
            Assert.AreEqual(XnsTokenType.StyleEquals, tokens[6].Type);
            Assert.AreEqual(XnsTokenType.StyleValue, tokens[7].Type);
            Assert.AreEqual("100%", tokens[7].Content);

            Assert.AreEqual(XnsTokenType.Comment, tokens[8].Type);

            Assert.AreEqual(XnsTokenType.ClassName, tokens[9].Type);
            Assert.AreEqual("panel", tokens[9].Content);

            Assert.AreEqual(XnsTokenType.BeginClassContent, tokens[10].Type);

            Assert.AreEqual(XnsTokenType.StyleName, tokens[11].Type);
            Assert.AreEqual("width", tokens[11].Content);
            Assert.AreEqual(XnsTokenType.StyleEquals, tokens[12].Type);
            Assert.AreEqual(XnsTokenType.StyleValue, tokens[13].Type);
            Assert.AreEqual("50px", tokens[13].Content);

            Assert.AreEqual(XnsTokenType.StyleName, tokens[14].Type);
            Assert.AreEqual("background", tokens[14].Content);
            Assert.AreEqual(XnsTokenType.StyleEquals, tokens[15].Type);
            Assert.AreEqual(XnsTokenType.StyleValue, tokens[16].Type);
            Assert.AreEqual("#222222", tokens[16].Content);

            Assert.AreEqual(XnsTokenType.EndClassContent, tokens[17].Type);

            Assert.AreEqual(XnsTokenType.ClassName, tokens[30].Type);
            Assert.AreEqual("entries", tokens[30].Content);

            Assert.AreEqual(XnsTokenType.BeginClassContent, tokens[31].Type);

            Assert.AreEqual(XnsTokenType.StyleName, tokens[32].Type);
            Assert.AreEqual("layout", tokens[32].Content);
            Assert.AreEqual(XnsTokenType.StyleEquals, tokens[33].Type);
            Assert.AreEqual(XnsTokenType.StyleValue, tokens[34].Type);
            Assert.AreEqual("column", tokens[34].Content);

            Assert.AreEqual(XnsTokenType.ClassName, tokens[35].Type);
            Assert.AreEqual("entry", tokens[35].Content);

            Assert.AreEqual(XnsTokenType.BeginClassContent, tokens[36].Type);
            Assert.AreEqual(XnsTokenType.EndClassContent, tokens[37].Type);

            Assert.AreEqual(XnsTokenType.EndClassContent, tokens[38].Type);
            Assert.AreEqual(XnsTokenType.EndClassContent, tokens[39].Type);
        }

        [TestMethod]
        public void ADecimalValueIsReadAsASingleToken()
        {
            var xnsSource = new XnsSource("box {\r\n    corner-radius: 2.5px\r\n}");
            var tokens = xnsSource.Tokenize();

            XnsToken value = tokens.Single(t => t.Type == XnsTokenType.StyleValue);

            Assert.AreEqual("2.5px", value.Content);
        }

        [TestMethod]
        public void ADecimalValueDoesNotSwallowTheNextClass()
        {
            var xnsSource = new XnsSource("box {\r\n    corner-radius: 2.5px\r\n}\r\n.active {\r\n    corner-radius: 1px\r\n}");
            var tokens = xnsSource.Tokenize();

            Assert.IsFalse(xnsSource.HasErrors, string.Join(" | ", xnsSource.Diagnostics.Select(d => d.Message)));
            Assert.AreEqual(".active", tokens.Where(t => t.Type == XnsTokenType.ClassName).Last().Content);
        }

        private static string TokensOf(string source, XnsTokenType type)
        {
            var xnsSource = new XnsSource(source);
            var tokens = xnsSource.Tokenize();

            Assert.IsFalse(xnsSource.HasErrors, string.Join(" | ", xnsSource.Diagnostics.Select(d => d.Message)));

            return string.Join("|", tokens.Where(t => t.Type == type).Select(t => t.Content));
        }

        private static string StyleValuesOf(string source) => TokensOf(source, XnsTokenType.StyleValue);
        private static string StyleNamesOf(string source) => TokensOf(source, XnsTokenType.StyleName);

        [TestMethod]
        public void TwoStylesCanShareALine()
        {
            string source = "el {\r\n    width: 200px  height: 100px\r\n}";

            Assert.AreEqual("width|height", StyleNamesOf(source));
            Assert.AreEqual("200px|100px", StyleValuesOf(source));
        }

        [TestMethod]
        public void AWholeBlockCanBeWrittenOnOneLine()
        {
            Assert.AreEqual("row|1*|50%", StyleValuesOf("el { layout: row width: 1* height: 50% }"));
        }

        [TestMethod]
        public void AHyphenatedStyleNameEndsThePreviousValue()
        {
            Assert.AreEqual("12px|Verdana", StyleValuesOf("el { font-size: 12px font-family: Verdana }"));
        }

        [TestMethod]
        public void AMultiPartValueIsNotCutByItsOwnWords()
        {
            Assert.AreEqual("#FF0000 2px inner|10px 20px",
                StyleValuesOf("el {\r\n    border: #FF0000 2px inner\r\n    margin: 10px 20px\r\n}"));
        }

        [TestMethod]
        public void AMultiPartValueSurvivesASiblingOnTheSameLine()
        {
            Assert.AreEqual("#FF0000 2px inner|200px",
                StyleValuesOf("el { border: #FF0000 2px inner  width: 200px }"));
        }

        [TestMethod]
        public void ANestedClassStillEndsTheValueBefore()
        {
            var xnsSource = new XnsSource("box {\r\n    width: 1* height: 1*\r\n    inner { width: 5px }\r\n}");
            var tokens = xnsSource.Tokenize();

            Assert.IsFalse(xnsSource.HasErrors, string.Join(" | ", xnsSource.Diagnostics.Select(d => d.Message)));
            CollectionAssert.AreEqual(new[] { "box", "inner" },
                tokens.Where(t => t.Type == XnsTokenType.ClassName).Select(t => t.Content).ToArray());
        }

        [TestMethod]
        public void AContentUnitIsReadOutsideTheFirstPosition()
        {
            var xnsSource = new XnsSource("box {\r\n    row-template: 100px ? 1*\r\n}");
            var tokens = xnsSource.Tokenize();

            Assert.IsFalse(xnsSource.HasErrors, string.Join(" | ", xnsSource.Diagnostics.Select(d => d.Message)));
            Assert.AreEqual("100px ? 1*", tokens.Single(t => t.Type == XnsTokenType.StyleValue).Content);
        }

        [TestMethod]
        public void AHyphenatedValueIsReadWhole()
        {
            var xnsSource = new XnsSource("box {\r\n    cursor: ew-resize\r\n}");
            var tokens = xnsSource.Tokenize();

            Assert.IsFalse(xnsSource.HasErrors, string.Join(" | ", xnsSource.Diagnostics.Select(d => d.Message)));
            Assert.AreEqual("ew-resize", tokens.Single(t => t.Type == XnsTokenType.StyleValue).Content);
        }

        [TestMethod]
        public void AHyphenatedValueDoesNotSwallowTheNextStyle()
        {
            Assert.AreEqual("ew-resize|200px",
                StyleValuesOf("el { cursor: ew-resize  width: 200px }"));
        }
    }
}
