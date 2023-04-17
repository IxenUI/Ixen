using Ixen.Core.Language.Xnl;
using Ixen.Core.Language.Xns;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ixen.Core.UT.Xnl
{
    [TestClass]
    public class XnlTokenizerTest
    {
        [TestMethod]
        public void TestTokenize()
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
            var tokens = xnlSource.Tokenize();

            Assert.AreEqual(tokens.Count, 60);

            Assert.AreEqual(tokens[0].Content, "container");
            Assert.AreEqual(tokens[0].Type, XnlTokenType.ElementName);
            Assert.AreEqual(tokens[1].Type, XnlTokenType.ElementTypeEquals);
            Assert.AreEqual(tokens[2].Type, XnlTokenType.ElementTypeName);
            Assert.AreEqual(tokens[2].Content, "MonSuperType");

            Assert.AreEqual(tokens[3].Type, XnlTokenType.BeginParams);

            Assert.AreEqual(tokens[4].Type, XnlTokenType.ParamName);
            Assert.AreEqual(tokens[4].Content, "start");
            Assert.AreEqual(tokens[5].Type, XnlTokenType.ParamEquals);
            Assert.AreEqual(tokens[6].Type, XnlTokenType.ParamValue);
            Assert.AreEqual(tokens[6].Content, "true");

            Assert.AreEqual(tokens[7].Type, XnlTokenType.ParamName);
            Assert.AreEqual(tokens[7].Content, "machin");
            Assert.AreEqual(tokens[8].Type, XnlTokenType.ParamEquals);
            Assert.AreEqual(tokens[9].Type, XnlTokenType.ParamValue);
            Assert.AreEqual(tokens[9].Content, "bazar");

            Assert.AreEqual(tokens[10].Type, XnlTokenType.EndParams);

            Assert.AreEqual(tokens[11].Type, XnlTokenType.BeginContent);

            Assert.AreEqual(tokens[12].Type, XnlTokenType.ElementName);
            Assert.AreEqual(tokens[12].Content, "panel");

            Assert.AreEqual(tokens[15].Type, XnlTokenType.ElementName);
            Assert.AreEqual(tokens[15].Content, "entries");

            Assert.AreEqual(tokens[18].Type, XnlTokenType.ElementName);
            Assert.AreEqual(tokens[18].Content, "entry");
            Assert.AreEqual(tokens[19].Type, XnlTokenType.ElementTypeEquals);
            Assert.AreEqual(tokens[20].Type, XnlTokenType.ElementTypeName);
            Assert.AreEqual(tokens[20].Content, "Nav");

            Assert.AreEqual(tokens[45].Type, XnlTokenType.EndContent);
            Assert.AreEqual(tokens[46].Type, XnlTokenType.EndContent);

            Assert.AreEqual(tokens[47].Type, XnlTokenType.ElementName);
            Assert.AreEqual(tokens[47].Content, "content");

            Assert.AreEqual(tokens[50].Type, XnlTokenType.ElementName);
            Assert.AreEqual(tokens[50].Content, "_");

            Assert.AreEqual(tokens[54].Type, XnlTokenType.ParamName);
            Assert.AreEqual(tokens[54].Content, "content");
            Assert.AreEqual(tokens[55].Type, XnlTokenType.ParamEquals);
            Assert.AreEqual(tokens[56].Type, XnlTokenType.ParamValue);
            Assert.AreEqual(tokens[56].Content, "Coucou");

            Assert.AreEqual(tokens[58].Type, XnlTokenType.EndContent);
            Assert.AreEqual(tokens[59].Type, XnlTokenType.EndContent);
        }
    }
}
