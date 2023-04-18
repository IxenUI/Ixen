using Ixen.Core.Language.Xnl;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Ixen.Core.UT.Xnl
{
    [TestClass]
    public class XnlNodifierTests
    {
        [TestMethod]
        public void TestNodify()
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
            var node = xnlSource.Nodify();

            Assert.IsNotNull(node);
            Assert.AreEqual(node.Parameters.Count, 0);
            Assert.AreEqual(node.Children.Count, 1);

            var containerNode = node.Children[0];
            Assert.AreEqual(containerNode.Name, "container");
            Assert.AreEqual(containerNode.Type, "MonSuperType");
            Assert.AreEqual(containerNode.Parameters.Count, 2);
            Assert.AreEqual(containerNode.Children.Count, 2);

            Assert.AreEqual(containerNode.Parameters[0].Name, "start");
            Assert.AreEqual(containerNode.Parameters[0].Value, "true");
            Assert.AreEqual(containerNode.Parameters[1].Name, "machin");
            Assert.AreEqual(containerNode.Parameters[1].Value, "bazar");

            var panelNode = containerNode.Children[0];
            Assert.AreEqual(panelNode.Name, "panel");
            Assert.AreEqual(panelNode.Parameters.Count, 0);
            Assert.AreEqual(panelNode.Children.Count, 1);

            var entriesNode = panelNode.Children[0];
            Assert.AreEqual(entriesNode.Name, "entries");
            Assert.AreEqual(entriesNode.Parameters.Count, 0);
            Assert.AreEqual(entriesNode.Children.Count, 3);

            Assert.AreEqual(entriesNode.Children[0].Name, "entry");
            Assert.AreEqual(entriesNode.Children[0].Type, "Nav");
            Assert.AreEqual(entriesNode.Children[1].Name, "entry");
            Assert.AreEqual(entriesNode.Children[1].Type, "Nav");
            Assert.AreEqual(entriesNode.Children[2].Name, "entry");
            Assert.AreEqual(entriesNode.Children[2].Type, "Nav");

            var navNode = entriesNode.Children[0];
            Assert.AreEqual(navNode.Parameters.Count, 2);
            Assert.AreEqual(navNode.Children.Count, 0);

            Assert.AreEqual(navNode.Parameters[0].Name, "link");
            Assert.AreEqual(navNode.Parameters[0].Value, "/downloads");
            Assert.AreEqual(navNode.Parameters[1].Name, "class");
            Assert.AreEqual(navNode.Parameters[1].Value, "active");

            var contentNode = containerNode.Children[1];
            Assert.AreEqual(contentNode.Parameters.Count, 0);
            Assert.AreEqual(contentNode.Children.Count, 1);

            var anonNode = contentNode.Children[0];
            Assert.AreEqual(anonNode.Name, "_");
            Assert.AreEqual(anonNode.Type, "Label");
            Assert.AreEqual(anonNode.Parameters.Count, 1);
            Assert.AreEqual(anonNode.Children.Count, 0);

            Assert.AreEqual(anonNode.Parameters[0].Name, "content");
            Assert.AreEqual(anonNode.Parameters[0].Value, "Coucou");
        }
    }
}
