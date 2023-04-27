using Ixen.Core.Language.Xns;
using Microsoft.VisualStudio.TestTools.UnitTesting;

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

            Assert.AreEqual(tokens.Count, 46);

            Assert.AreEqual(tokens[0].Index, 0);
            Assert.AreEqual(tokens[0].Content, "container");
            Assert.AreEqual(tokens[0].Type, XnsTokenType.ClassName);

            Assert.AreEqual(tokens[1].Type, XnsTokenType.BeginClassContent);

            Assert.AreEqual(tokens[2].Type, XnsTokenType.StyleName);
            Assert.AreEqual(tokens[2].Content, "layout");
            Assert.AreEqual(tokens[3].Type, XnsTokenType.StyleEquals);
            Assert.AreEqual(tokens[4].Type, XnsTokenType.StyleValue);
            Assert.AreEqual(tokens[4].Content, "row");

            Assert.AreEqual(tokens[5].Type, XnsTokenType.StyleName);
            Assert.AreEqual(tokens[5].Content, "width");
            Assert.AreEqual(tokens[6].Type, XnsTokenType.StyleEquals);
            Assert.AreEqual(tokens[7].Type, XnsTokenType.StyleValue);
            Assert.AreEqual(tokens[7].Content, "100%");

            Assert.AreEqual(tokens[8].Type, XnsTokenType.Comment);

            Assert.AreEqual(tokens[9].Type, XnsTokenType.ClassName);
            Assert.AreEqual(tokens[9].Content, "panel");

            Assert.AreEqual(tokens[10].Type, XnsTokenType.BeginClassContent);

            Assert.AreEqual(tokens[11].Type, XnsTokenType.StyleName);
            Assert.AreEqual(tokens[11].Content, "width");
            Assert.AreEqual(tokens[12].Type, XnsTokenType.StyleEquals);
            Assert.AreEqual(tokens[13].Type, XnsTokenType.StyleValue);
            Assert.AreEqual(tokens[13].Content, "50px");

            Assert.AreEqual(tokens[14].Type, XnsTokenType.StyleName);
            Assert.AreEqual(tokens[14].Content, "background");
            Assert.AreEqual(tokens[15].Type, XnsTokenType.StyleEquals);
            Assert.AreEqual(tokens[16].Type, XnsTokenType.StyleValue);
            Assert.AreEqual(tokens[16].Content, "#222222");

            Assert.AreEqual(tokens[17].Type, XnsTokenType.EndClassContent);

            Assert.AreEqual(tokens[30].Type, XnsTokenType.ClassName);
            Assert.AreEqual(tokens[30].Content, "entries");

            Assert.AreEqual(tokens[31].Type, XnsTokenType.BeginClassContent);

            Assert.AreEqual(tokens[32].Type, XnsTokenType.StyleName);
            Assert.AreEqual(tokens[32].Content, "layout");
            Assert.AreEqual(tokens[33].Type, XnsTokenType.StyleEquals);
            Assert.AreEqual(tokens[34].Type, XnsTokenType.StyleValue);
            Assert.AreEqual(tokens[34].Content, "column");

            Assert.AreEqual(tokens[35].Type, XnsTokenType.ClassName);
            Assert.AreEqual(tokens[35].Content, "entry");

            Assert.AreEqual(tokens[36].Type, XnsTokenType.BeginClassContent);
            Assert.AreEqual(tokens[37].Type, XnsTokenType.EndClassContent);

            Assert.AreEqual(tokens[38].Type, XnsTokenType.EndClassContent);
            Assert.AreEqual(tokens[39].Type, XnsTokenType.EndClassContent);
        }
    }
}
