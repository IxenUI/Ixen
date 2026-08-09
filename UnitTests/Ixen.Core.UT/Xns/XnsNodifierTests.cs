using Ixen.Core.Language.Xnl;
using Ixen.Core.Language.Xns;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Xns
{
    [TestClass]
    public class XnsNodifierTests
    {
        [TestMethod]
        public void TestNodify()
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

            var xnsSource = new XnsSource(source);
            var node = xnsSource.Nodify();

            Assert.IsNotNull(node);
            Assert.AreEqual(2, node.Children.Count);

            var containerNode = node.Children[0];
            Assert.AreEqual("container", containerNode.Name);
            Assert.AreEqual(2, containerNode.Styles.Count);
            Assert.AreEqual(3, containerNode.Children.Count);

            Assert.AreEqual("layout", containerNode.Styles[0].Name);
            Assert.AreEqual("row", containerNode.Styles[0].Value);
            Assert.AreEqual("width", containerNode.Styles[1].Name);
            Assert.AreEqual("100%", containerNode.Styles[1].Value);

            var panelNode = containerNode.Children[0];
            Assert.AreEqual("panel", panelNode.Name);
            Assert.AreEqual(2, panelNode.Styles.Count);
            Assert.AreEqual(0, panelNode.Children.Count);

            Assert.AreEqual("width", panelNode.Styles[0].Name);
            Assert.AreEqual("50px", panelNode.Styles[0].Value);
            Assert.AreEqual("background", panelNode.Styles[1].Name);
            Assert.AreEqual("#222222", panelNode.Styles[1].Value);

            var contentNode = containerNode.Children[1];
            Assert.AreEqual("content", contentNode.Name);
            Assert.AreEqual(3, contentNode.Styles.Count);
            Assert.AreEqual(0, contentNode.Children.Count);

            Assert.AreEqual("width", contentNode.Styles[0].Name);
            Assert.AreEqual("1*", contentNode.Styles[0].Value);
            Assert.AreEqual("background", contentNode.Styles[1].Name);
            Assert.AreEqual("#EEEEEE", contentNode.Styles[1].Value);
            Assert.AreEqual("padding", contentNode.Styles[2].Name);
            Assert.AreEqual("5px", contentNode.Styles[2].Value);

            var entriesNode = containerNode.Children[2];
            Assert.AreEqual("entries", entriesNode.Name);
            Assert.AreEqual(1, entriesNode.Styles.Count);
            Assert.AreEqual(1, entriesNode.Children.Count);

            Assert.AreEqual("layout", entriesNode.Styles[0].Name);
            Assert.AreEqual("column", entriesNode.Styles[0].Value);

            Assert.AreEqual("entry", entriesNode.Children[0].Name);

            var activeNode = node.Children[1];
            Assert.AreEqual(".active", activeNode.Name);
            Assert.AreEqual(1, activeNode.Styles.Count);

            Assert.AreEqual("background", activeNode.Styles[0].Name);
            Assert.AreEqual("#FF2222", activeNode.Styles[0].Value);
        }
    }
}
