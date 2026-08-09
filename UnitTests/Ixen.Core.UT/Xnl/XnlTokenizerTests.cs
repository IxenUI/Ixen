using Ixen.Core.Language.Xnl;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Xnl
{
    [TestClass]
    public class XnlTokenizerTests
    {
        [TestMethod]
        public void TestTokenize1()
        {
            string source = @"
{}
[
	{}
    {}
]
";
            var xnlSource = new XnlSource(source);
            var tokens = xnlSource.Tokenize();

            Assert.IsNotNull(tokens);
            Assert.AreEqual(8, tokens.Count);

            Assert.AreEqual(XnlTokenType.PropertiesBegin, tokens[0].Type);
            Assert.AreEqual(XnlTokenType.PropertiesEnd, tokens[1].Type);
            Assert.AreEqual(XnlTokenType.ChildrenBegin, tokens[2].Type);
            Assert.AreEqual(XnlTokenType.PropertiesBegin, tokens[3].Type);
            Assert.AreEqual(XnlTokenType.PropertiesEnd, tokens[4].Type);
            Assert.AreEqual(XnlTokenType.PropertiesBegin, tokens[5].Type);
            Assert.AreEqual(XnlTokenType.PropertiesEnd, tokens[6].Type);
            Assert.AreEqual(XnlTokenType.ChildrenEnd, tokens[7].Type);
        }

        [TestMethod]
        public void TestTokenize2()
        {
            string source = @"
layout<VisualElement>{}
[
	test{}
    <VisualElement>{}
]
";
            var xnlSource = new XnlSource(source);
            var tokens = xnlSource.Tokenize();

            Assert.IsNotNull(tokens);
            Assert.AreEqual(16, tokens.Count);

            Assert.AreEqual(XnlTokenType.ElementName, tokens[0].Type);
            Assert.AreEqual("layout", tokens[0].Content);
            Assert.AreEqual(XnlTokenType.ElementTypeBegin, tokens[1].Type);
            Assert.AreEqual(XnlTokenType.ElementTypeName, tokens[2].Type);
            Assert.AreEqual("VisualElement", tokens[2].Content);
            Assert.AreEqual(XnlTokenType.ElementTypeEnd, tokens[3].Type);
            Assert.AreEqual(XnlTokenType.PropertiesBegin, tokens[4].Type);
            Assert.AreEqual(XnlTokenType.PropertiesEnd, tokens[5].Type);
            Assert.AreEqual(XnlTokenType.ChildrenBegin, tokens[6].Type);
            Assert.AreEqual(XnlTokenType.ElementName, tokens[7].Type);
            Assert.AreEqual("test", tokens[7].Content);
            Assert.AreEqual(XnlTokenType.PropertiesBegin, tokens[8].Type);
            Assert.AreEqual(XnlTokenType.PropertiesEnd, tokens[9].Type);
            Assert.AreEqual(XnlTokenType.ElementTypeBegin, tokens[10].Type);
            Assert.AreEqual(XnlTokenType.ElementTypeName, tokens[11].Type);
            Assert.AreEqual("VisualElement", tokens[11].Content);
            Assert.AreEqual(XnlTokenType.ElementTypeEnd, tokens[12].Type);
            Assert.AreEqual(XnlTokenType.PropertiesBegin, tokens[13].Type);
            Assert.AreEqual(XnlTokenType.PropertiesEnd, tokens[14].Type);
            Assert.AreEqual(XnlTokenType.ChildrenEnd, tokens[15].Type);
        }

        [TestMethod]
        public void TestTokenize3()
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
    // comment
    /* multi
        comment */
    <textinput>{placeholder: ""salut""}
]
";
            var xnlSource = new XnlSource(source);
            var tokens = xnlSource.Tokenize();

            Assert.IsNotNull(tokens);
            Assert.AreEqual(49, tokens.Count);

            Assert.AreEqual(XnlTokenType.ElementName, tokens[0].Type);
            Assert.AreEqual("layout", tokens[0].Content);
            Assert.AreEqual(XnlTokenType.ElementTypeBegin, tokens[1].Type);
            Assert.AreEqual(XnlTokenType.ElementTypeName, tokens[2].Type);
            Assert.AreEqual("VisualElement", tokens[2].Content);
            Assert.AreEqual(XnlTokenType.ElementTypeEnd, tokens[3].Type);
            Assert.AreEqual(XnlTokenType.PropertiesBegin, tokens[4].Type);
            Assert.AreEqual(XnlTokenType.PropertyName, tokens[5].Type);
            Assert.AreEqual("class", tokens[5].Content);
            Assert.AreEqual(XnlTokenType.PropertyEqual, tokens[6].Type);
            Assert.AreEqual(XnlTokenType.PropertyValueBegin, tokens[7].Type);
            Assert.AreEqual(XnlTokenType.PropertyValue, tokens[8].Type);
            Assert.AreEqual("layout", tokens[8].Content);
            Assert.AreEqual(XnlTokenType.PropertyValueEnd, tokens[9].Type);
            Assert.AreEqual(XnlTokenType.PropertyName, tokens[10].Type);
            Assert.AreEqual("truc", tokens[10].Content);
            Assert.AreEqual(XnlTokenType.PropertyEqual, tokens[11].Type);
            Assert.AreEqual(XnlTokenType.PropertyValueBegin, tokens[12].Type);
            Assert.AreEqual(XnlTokenType.PropertyValue, tokens[13].Type);
            Assert.AreEqual("chose", tokens[13].Content);
            Assert.AreEqual(XnlTokenType.PropertyValueEnd, tokens[14].Type);
            Assert.AreEqual(XnlTokenType.PropertiesEnd, tokens[15].Type);
            Assert.AreEqual(XnlTokenType.ChildrenBegin, tokens[16].Type);
            Assert.AreEqual(XnlTokenType.PropertiesBegin, tokens[17].Type);
            Assert.AreEqual(XnlTokenType.PropertyName, tokens[18].Type);
            Assert.AreEqual("class", tokens[18].Content);
            Assert.AreEqual(XnlTokenType.PropertyEqual, tokens[19].Type);
            Assert.AreEqual(XnlTokenType.PropertyValueBegin, tokens[20].Type);
            Assert.AreEqual(XnlTokenType.PropertyValue, tokens[21].Type);
            Assert.AreEqual("el1", tokens[21].Content);
            Assert.AreEqual(XnlTokenType.PropertyValueEnd, tokens[22].Type);
            Assert.AreEqual(XnlTokenType.PropertiesEnd, tokens[23].Type);
            Assert.AreEqual(XnlTokenType.ChildrenBegin, tokens[24].Type);
            Assert.AreEqual(XnlTokenType.ElementTypeBegin, tokens[25].Type);
            Assert.AreEqual(XnlTokenType.ElementTypeName, tokens[26].Type);
            Assert.AreEqual("label", tokens[26].Content);
            Assert.AreEqual(XnlTokenType.ElementTypeEnd, tokens[27].Type);
            Assert.AreEqual(XnlTokenType.PropertiesBegin, tokens[28].Type);
            Assert.AreEqual(XnlTokenType.PropertyName, tokens[29].Type);
            Assert.AreEqual("text", tokens[29].Content);
            Assert.AreEqual(XnlTokenType.PropertyEqual, tokens[30].Type);
            Assert.AreEqual(XnlTokenType.PropertyValueBegin, tokens[31].Type);
            Assert.AreEqual(XnlTokenType.PropertyValue, tokens[32].Type);
            Assert.AreEqual("Coucou", tokens[32].Content);
            Assert.AreEqual(XnlTokenType.PropertyValueEnd, tokens[33].Type);
            Assert.AreEqual(XnlTokenType.PropertiesEnd, tokens[34].Type);
            Assert.AreEqual(XnlTokenType.ChildrenEnd, tokens[35].Type);
            Assert.AreEqual(XnlTokenType.Comment, tokens[36].Type);
            Assert.AreEqual(XnlTokenType.Comment, tokens[37].Type);
            Assert.AreEqual(XnlTokenType.ElementTypeBegin, tokens[38].Type);
            Assert.AreEqual(XnlTokenType.ElementTypeName, tokens[39].Type);
            Assert.AreEqual("textinput", tokens[39].Content);
            Assert.AreEqual(XnlTokenType.ElementTypeEnd, tokens[40].Type);
            Assert.AreEqual(XnlTokenType.PropertiesBegin, tokens[41].Type);
            Assert.AreEqual(XnlTokenType.PropertyName, tokens[42].Type);
            Assert.AreEqual("placeholder", tokens[42].Content);
            Assert.AreEqual(XnlTokenType.PropertyEqual, tokens[43].Type);
            Assert.AreEqual(XnlTokenType.PropertyValueBegin, tokens[44].Type);
            Assert.AreEqual(XnlTokenType.PropertyValue, tokens[45].Type);
            Assert.AreEqual("salut", tokens[45].Content);
            Assert.AreEqual(XnlTokenType.PropertyValueEnd, tokens[46].Type);
            Assert.AreEqual(XnlTokenType.PropertiesEnd, tokens[47].Type);
            Assert.AreEqual(XnlTokenType.ChildrenEnd, tokens[48].Type);
        }
    }
}
