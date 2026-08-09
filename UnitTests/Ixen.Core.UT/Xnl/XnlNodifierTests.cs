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
            Assert.AreEqual(1, node.Children.Count);

            var firstNode = node.Children[0];
            Assert.AreEqual(2, firstNode.Children.Count);
            Assert.AreEqual(0, firstNode.Children[0].Children.Count);
            Assert.AreEqual(0, firstNode.Children[1].Children.Count);
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
            Assert.AreEqual(1, node.Children.Count);

            var firstNode = node.Children[0];
            Assert.AreEqual("layout", firstNode.Name);
            Assert.AreEqual("VisualElement", firstNode.Type);
            Assert.AreEqual(2, firstNode.Children.Count);

            var child1 = firstNode.Children[0];
            Assert.AreEqual("test", child1.Name);
            Assert.IsNull(child1.Type);
            Assert.AreEqual(0, child1.Children.Count);

            var child2 = firstNode.Children[1];
            Assert.IsNull(child2.Name);
            Assert.AreEqual("VisualElement", child2.Type);
            Assert.AreEqual(0, child2.Children.Count);
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
            Assert.AreEqual(1, node.Children.Count);

            var layoutNode = node.Children[0];
            Assert.AreEqual("layout", layoutNode.Name);
            Assert.AreEqual("VisualElement", layoutNode.Type);
            Assert.AreEqual(2, layoutNode.Children.Count);
            Assert.AreEqual(2, layoutNode.Properties.Count);
            Assert.AreEqual("class", layoutNode.Properties[0].Name);
            Assert.AreEqual("layout", layoutNode.Properties[0].Value);
            Assert.AreEqual("truc", layoutNode.Properties[1].Name);
            Assert.AreEqual("chose", layoutNode.Properties[1].Value);

            var childNode1 = layoutNode.Children[0];
            Assert.IsNull(childNode1.Name);
            Assert.IsNull(childNode1.Type);
            Assert.AreEqual(1, childNode1.Children.Count);

            var labelNode = childNode1.Children[0];
            Assert.IsNull(labelNode.Name);
            Assert.AreEqual("label", labelNode.Type);
            Assert.AreEqual(0, labelNode.Children.Count);
            Assert.AreEqual(1, labelNode.Properties.Count);
            Assert.AreEqual("text", labelNode.Properties[0].Name);
            Assert.AreEqual("Coucou", labelNode.Properties[0].Value);

            var childNode2 = layoutNode.Children[1];
            Assert.IsNull(childNode2.Name);
            Assert.AreEqual("textinput", childNode2.Type);
            Assert.AreEqual(0, childNode2.Children.Count);
            Assert.AreEqual(1, childNode2.Properties.Count);
            Assert.AreEqual("placeholder", childNode2.Properties[0].Name);
            Assert.AreEqual("salut", childNode2.Properties[0].Value);
        }
    }
}
