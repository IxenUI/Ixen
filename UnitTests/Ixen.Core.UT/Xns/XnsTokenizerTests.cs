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
    }
}
