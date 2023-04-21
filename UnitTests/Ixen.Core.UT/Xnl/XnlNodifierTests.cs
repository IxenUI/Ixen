using Ixen.Core.Language.Xnl;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Xnl
{
    [TestClass]
    public class XnlNodifierTests
    {
        [TestMethod]
        public void TestNodify1()
        {
            string source = @"
{}
[
	{}
    {}
]
";

            var xnlSource = new XnlSource(source);
            var node = xnlSource.Nodify();

            Assert.IsNotNull(node);
            Assert.AreEqual(node.Children.Count, 1);

            var firstNode = node.Children[0];
            Assert.AreEqual(firstNode.Children.Count, 2);
            Assert.AreEqual(firstNode.Children[0].Children.Count, 0);
            Assert.AreEqual(firstNode.Children[1].Children.Count, 0);
        }

        [TestMethod]
        public void TestNodify2()
        {
            string source = @"
layout<VisualElement>{}
[
	test {}
    <VisualElement> {}
]
";

            var xnlSource = new XnlSource(source);
            var node = xnlSource.Nodify();

            Assert.IsNotNull(node);
            Assert.AreEqual(node.Children.Count, 1);

            var firstNode = node.Children[0];
            Assert.AreEqual(firstNode.Name, "layout");
            Assert.AreEqual(firstNode.Type, "VisualElement");
            Assert.AreEqual(firstNode.Children.Count, 2);

            var child1 = firstNode.Children[0];
            Assert.AreEqual(child1.Name, "test");
            Assert.IsNull(child1.Type);
            Assert.AreEqual(child1.Children.Count, 0);

            var child2 = firstNode.Children[1];
            Assert.IsNull(child2.Name);
            Assert.AreEqual(child2.Type, "VisualElement");
            Assert.AreEqual(child2.Children.Count, 0);
        }

        [TestMethod]
        public void TestNodify3()
        {
            string source = @"
layout<VisualElement>{class: ""layout"" truc: ""chose""}
[
	{
        class:""el1""
    }
    [
        <label>{text: ""Coucou""}
    ]
    <textinput>{placeholder: ""salut""}
]
";

            var xnlSource = new XnlSource(source);
            var node = xnlSource.Nodify();

            Assert.IsNotNull(node);
            Assert.AreEqual(node.Children.Count, 1);

            var layoutNode = node.Children[0];
            Assert.AreEqual(layoutNode.Name, "layout");
            Assert.AreEqual(layoutNode.Type, "VisualElement");
            Assert.AreEqual(layoutNode.Children.Count, 2);
            Assert.AreEqual(layoutNode.Properties.Count, 2);
            Assert.AreEqual(layoutNode.Properties[0].Name, "class");
            Assert.AreEqual(layoutNode.Properties[0].Value, "layout");
            Assert.AreEqual(layoutNode.Properties[1].Name, "truc");
            Assert.AreEqual(layoutNode.Properties[1].Value, "chose");

            var childNode1 = layoutNode.Children[0];
            Assert.IsNull(childNode1.Name);
            Assert.IsNull(childNode1.Type);
            Assert.AreEqual(childNode1.Children.Count, 1);

            var labelNode = childNode1.Children[0];
            Assert.IsNull(labelNode.Name);
            Assert.AreEqual(labelNode.Type, "label");
            Assert.AreEqual(labelNode.Children.Count, 0);
            Assert.AreEqual(labelNode.Properties.Count, 1);
            Assert.AreEqual(labelNode.Properties[0].Name, "text");
            Assert.AreEqual(labelNode.Properties[0].Value, "Coucou");

            var childNode2 = layoutNode.Children[1];
            Assert.IsNull(childNode2.Name);
            Assert.AreEqual(childNode2.Type, "textinput");
            Assert.AreEqual(childNode2.Children.Count, 0);
            Assert.AreEqual(childNode2.Properties.Count, 1);
            Assert.AreEqual(childNode2.Properties[0].Name, "placeholder");
            Assert.AreEqual(childNode2.Properties[0].Value, "salut");
        }
    }
}
