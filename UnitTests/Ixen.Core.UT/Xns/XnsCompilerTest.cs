using Ixen.Core.Language.Xns;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Xns
{
    [TestClass]
    public class XnsCompilerTest
    {
        [TestMethod]
        public void TestParse()
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
            var classes = xnsSource.Compile();
        }
    }
}
