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

            Assert.AreEqual(tokens[0].Type, XnlTokenType.PropertiesBegin);
            Assert.AreEqual(tokens[1].Type, XnlTokenType.PropertiesEnd);
            Assert.AreEqual(tokens[2].Type, XnlTokenType.ChildrenBegin);
            Assert.AreEqual(tokens[3].Type, XnlTokenType.PropertiesBegin);
            Assert.AreEqual(tokens[4].Type, XnlTokenType.PropertiesEnd);
            Assert.AreEqual(tokens[5].Type, XnlTokenType.PropertiesBegin);
            Assert.AreEqual(tokens[6].Type, XnlTokenType.PropertiesEnd);
            Assert.AreEqual(tokens[7].Type, XnlTokenType.ChildrenEnd);
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

            Assert.AreEqual(tokens[0].Type, XnlTokenType.ElementName);
            Assert.AreEqual(tokens[0].Content, "layout");
            Assert.AreEqual(tokens[1].Type, XnlTokenType.ElementTypeBegin);
            Assert.AreEqual(tokens[2].Type, XnlTokenType.ElementTypeName);
            Assert.AreEqual(tokens[2].Content, "VisualElement");
            Assert.AreEqual(tokens[3].Type, XnlTokenType.ElementTypeEnd);
            Assert.AreEqual(tokens[4].Type, XnlTokenType.PropertiesBegin);
            Assert.AreEqual(tokens[5].Type, XnlTokenType.PropertiesEnd);
            Assert.AreEqual(tokens[6].Type, XnlTokenType.ChildrenBegin);
            Assert.AreEqual(tokens[7].Type, XnlTokenType.ElementName);
            Assert.AreEqual(tokens[7].Content, "test");
            Assert.AreEqual(tokens[8].Type, XnlTokenType.PropertiesBegin);
            Assert.AreEqual(tokens[9].Type, XnlTokenType.PropertiesEnd);
            Assert.AreEqual(tokens[10].Type, XnlTokenType.ElementTypeBegin);
            Assert.AreEqual(tokens[11].Type, XnlTokenType.ElementTypeName);
            Assert.AreEqual(tokens[11].Content, "VisualElement");
            Assert.AreEqual(tokens[12].Type, XnlTokenType.ElementTypeEnd);
            Assert.AreEqual(tokens[13].Type, XnlTokenType.PropertiesBegin);
            Assert.AreEqual(tokens[14].Type, XnlTokenType.PropertiesEnd);
            Assert.AreEqual(tokens[15].Type, XnlTokenType.ChildrenEnd);
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

            Assert.AreEqual(tokens[0].Type, XnlTokenType.ElementName);
            Assert.AreEqual(tokens[0].Content, "layout");
            Assert.AreEqual(tokens[1].Type, XnlTokenType.ElementTypeBegin);
            Assert.AreEqual(tokens[2].Type, XnlTokenType.ElementTypeName);
            Assert.AreEqual(tokens[2].Content, "VisualElement");
            Assert.AreEqual(tokens[3].Type, XnlTokenType.ElementTypeEnd);
            Assert.AreEqual(tokens[4].Type, XnlTokenType.PropertiesBegin);
            Assert.AreEqual(tokens[5].Type, XnlTokenType.PropertyName);
            Assert.AreEqual(tokens[5].Content, "class");
            Assert.AreEqual(tokens[6].Type, XnlTokenType.PropertyEqual);
            Assert.AreEqual(tokens[7].Type, XnlTokenType.PropertyValueBegin);
            Assert.AreEqual(tokens[8].Type, XnlTokenType.PropertyValue);
            Assert.AreEqual(tokens[8].Content, "layout");
            Assert.AreEqual(tokens[9].Type, XnlTokenType.PropertyValueEnd);
            Assert.AreEqual(tokens[10].Type, XnlTokenType.PropertyName);
            Assert.AreEqual(tokens[10].Content, "truc");
            Assert.AreEqual(tokens[11].Type, XnlTokenType.PropertyEqual);
            Assert.AreEqual(tokens[12].Type, XnlTokenType.PropertyValueBegin);
            Assert.AreEqual(tokens[13].Type, XnlTokenType.PropertyValue);
            Assert.AreEqual(tokens[13].Content, "chose");
            Assert.AreEqual(tokens[14].Type, XnlTokenType.PropertyValueEnd);
            Assert.AreEqual(tokens[15].Type, XnlTokenType.PropertiesEnd);
            Assert.AreEqual(tokens[16].Type, XnlTokenType.ChildrenBegin);
            Assert.AreEqual(tokens[17].Type, XnlTokenType.PropertiesBegin);
            Assert.AreEqual(tokens[18].Type, XnlTokenType.PropertyName);
            Assert.AreEqual(tokens[18].Content, "class");
            Assert.AreEqual(tokens[19].Type, XnlTokenType.PropertyEqual);
            Assert.AreEqual(tokens[20].Type, XnlTokenType.PropertyValueBegin);
            Assert.AreEqual(tokens[21].Type, XnlTokenType.PropertyValue);
            Assert.AreEqual(tokens[21].Content, "el1");
            Assert.AreEqual(tokens[22].Type, XnlTokenType.PropertyValueEnd);
            Assert.AreEqual(tokens[23].Type, XnlTokenType.PropertiesEnd);
            Assert.AreEqual(tokens[24].Type, XnlTokenType.ChildrenBegin);
            Assert.AreEqual(tokens[25].Type, XnlTokenType.ElementTypeBegin);
            Assert.AreEqual(tokens[26].Type, XnlTokenType.ElementTypeName);
            Assert.AreEqual(tokens[26].Content, "label");
            Assert.AreEqual(tokens[27].Type, XnlTokenType.ElementTypeEnd);
            Assert.AreEqual(tokens[28].Type, XnlTokenType.PropertiesBegin);
            Assert.AreEqual(tokens[29].Type, XnlTokenType.PropertyName);
            Assert.AreEqual(tokens[29].Content, "text");
            Assert.AreEqual(tokens[30].Type, XnlTokenType.PropertyEqual);
            Assert.AreEqual(tokens[31].Type, XnlTokenType.PropertyValueBegin);
            Assert.AreEqual(tokens[32].Type, XnlTokenType.PropertyValue);
            Assert.AreEqual(tokens[32].Content, "Coucou");
            Assert.AreEqual(tokens[33].Type, XnlTokenType.PropertyValueEnd);
            Assert.AreEqual(tokens[34].Type, XnlTokenType.PropertiesEnd);
            Assert.AreEqual(tokens[35].Type, XnlTokenType.ChildrenEnd);
            Assert.AreEqual(tokens[36].Type, XnlTokenType.Comment);
            Assert.AreEqual(tokens[37].Type, XnlTokenType.Comment);
            Assert.AreEqual(tokens[38].Type, XnlTokenType.ElementTypeBegin);
            Assert.AreEqual(tokens[39].Type, XnlTokenType.ElementTypeName);
            Assert.AreEqual(tokens[39].Content, "textinput");
            Assert.AreEqual(tokens[40].Type, XnlTokenType.ElementTypeEnd);
            Assert.AreEqual(tokens[41].Type, XnlTokenType.PropertiesBegin);
            Assert.AreEqual(tokens[42].Type, XnlTokenType.PropertyName);
            Assert.AreEqual(tokens[42].Content, "placeholder");
            Assert.AreEqual(tokens[43].Type, XnlTokenType.PropertyEqual);
            Assert.AreEqual(tokens[44].Type, XnlTokenType.PropertyValueBegin);
            Assert.AreEqual(tokens[45].Type, XnlTokenType.PropertyValue);
            Assert.AreEqual(tokens[45].Content, "salut");
            Assert.AreEqual(tokens[46].Type, XnlTokenType.PropertyValueEnd);
            Assert.AreEqual(tokens[47].Type, XnlTokenType.PropertiesEnd);
            Assert.AreEqual(tokens[48].Type, XnlTokenType.ChildrenEnd);
        }
    }
}
