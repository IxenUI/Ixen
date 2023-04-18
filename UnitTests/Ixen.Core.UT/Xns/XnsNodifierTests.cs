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
            Assert.AreEqual(node.Children.Count, 2);

            var containerNode = node.Children[0];
            Assert.AreEqual(containerNode.Name, "container");
            Assert.AreEqual(containerNode.Styles.Count, 2);
            Assert.AreEqual(containerNode.Children.Count, 3);

            Assert.AreEqual(containerNode.Styles[0].Name, "layout");
            Assert.AreEqual(containerNode.Styles[0].Value, "row");
            Assert.AreEqual(containerNode.Styles[1].Name, "width");
            Assert.AreEqual(containerNode.Styles[1].Value, "100%");

            var panelNode = containerNode.Children[0];
            Assert.AreEqual(panelNode.Name, "panel");
            Assert.AreEqual(panelNode.Styles.Count, 2);
            Assert.AreEqual(panelNode.Children.Count, 0);

            Assert.AreEqual(panelNode.Styles[0].Name, "width");
            Assert.AreEqual(panelNode.Styles[0].Value, "50px");
            Assert.AreEqual(panelNode.Styles[1].Name, "background");
            Assert.AreEqual(panelNode.Styles[1].Value, "#222222");

            var contentNode = containerNode.Children[1];
            Assert.AreEqual(contentNode.Name, "content");
            Assert.AreEqual(contentNode.Styles.Count, 3);
            Assert.AreEqual(contentNode.Children.Count, 0);

            Assert.AreEqual(contentNode.Styles[0].Name, "width");
            Assert.AreEqual(contentNode.Styles[0].Value, "1*");
            Assert.AreEqual(contentNode.Styles[1].Name, "background");
            Assert.AreEqual(contentNode.Styles[1].Value, "#EEEEEE");
            Assert.AreEqual(contentNode.Styles[2].Name, "padding");
            Assert.AreEqual(contentNode.Styles[2].Value, "5px");

            var entriesNode = containerNode.Children[2];
            Assert.AreEqual(entriesNode.Name, "entries");
            Assert.AreEqual(entriesNode.Styles.Count, 1);
            Assert.AreEqual(entriesNode.Children.Count, 1);

            Assert.AreEqual(entriesNode.Styles[0].Name, "layout");
            Assert.AreEqual(entriesNode.Styles[0].Value, "column");

            Assert.AreEqual(entriesNode.Children[0].Name, "entry");

            var activeNode = node.Children[1];
            Assert.AreEqual(activeNode.Name, ".active");
            Assert.AreEqual(activeNode.Styles.Count, 1);

            Assert.AreEqual(activeNode.Styles[0].Name, "background");
            Assert.AreEqual(activeNode.Styles[0].Value, "#FF2222");
        }
    }
}
