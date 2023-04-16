using Ixen.Core.Language.Xns;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Xns
{
    [TestClass]
    public class XnsTokenizerTest
    {
        [TestMethod]
        public void TestTokenize()
        {
            string source = @"container {
    layout: row
    width: 100%
    
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

            var xnsSource = XnsSource.FromSource(source);
            var tokens = xnsSource.Tokenize();

            Assert.AreEqual(tokens.Count, 45);

            Assert.AreEqual(tokens[0].LineNum, 0);
            Assert.AreEqual(tokens[0].LineIndex, 0);
            Assert.AreEqual(tokens[0].Content, "container");
            Assert.AreEqual(tokens[0].Type, XnsTokenType.ClassIdentifier);
        }
    }
}
