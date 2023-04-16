using Ixen.Core.Language.Xnl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ixen.Core.UT.Xln
{
    [TestClass]
    public class XnlParserTest
    {
        [TestMethod]
        public void TestParse()
        {
            string source = @"
 container:MonSuperType( start=""true"" machin=""bazar"" ) {

    panel:{
    
        entries:{
        
            entry:Nav( link=""/downloads"" class=""active"" )
            entry:Nav( link=""/config"" )
            entry:Nav( link=""/about"" )
        
        }
    }
    
    content:{
        _:Label( content=""Coucou"" )
    }
}";

            var xnlSource = new XnlSource(source);
            xnlSource.Parse();
            var content = xnlSource.GetContent();
        }
    }
}
