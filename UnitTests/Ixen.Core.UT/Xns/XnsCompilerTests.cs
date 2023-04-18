using Ixen.Core.Language.Xns;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Xml.Linq;

namespace Ixen.Core.UT.Xns
{
    [TestClass]
    public class XnsCompilerTests
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

            var xnsSource = new XnsSource(source);
            var classes = xnsSource.Compile();

            Assert.IsNotNull(classes);
            Assert.AreEqual(classes.Classes.Count, 5);

            var containerClass = classes.Classes[0];
            Assert.AreEqual(containerClass.Name, "container");
            Assert.AreEqual(containerClass.Target, StyleClassTarget.ElementName);
            Assert.AreEqual(containerClass.Styles.Count, 2);

            Assert.AreEqual(containerClass.Styles[0].Identifier, StyleIdentifier.Layout);
            Assert.AreEqual(containerClass.Styles[1].Identifier, StyleIdentifier.Width);

            var layoutStyle = (LayoutStyleDescriptor)containerClass.Styles[0];
            Assert.AreEqual(layoutStyle.Type, LayoutType.Row);

            var widthStyle = (WidthStyleDescriptor)containerClass.Styles[1];
            Assert.AreEqual(widthStyle.Unit, SizeUnit.Percents);
            Assert.AreEqual(widthStyle.Value, 100);

            var panelClass = classes.Classes[1];
            Assert.AreEqual(panelClass.Name, "panel");
            Assert.AreEqual(panelClass.Scope, "container");
            Assert.AreEqual(panelClass.Target, StyleClassTarget.ElementName);
            Assert.AreEqual(panelClass.Styles.Count, 2);

            Assert.AreEqual(panelClass.Styles[0].Identifier, StyleIdentifier.Width);
            Assert.AreEqual(panelClass.Styles[1].Identifier, StyleIdentifier.Background);

            widthStyle = (WidthStyleDescriptor)panelClass.Styles[0];
            Assert.AreEqual(widthStyle.Unit, SizeUnit.Pixels);
            Assert.AreEqual(widthStyle.Value, 50);

            var backroundStyle = (BackgroundStyleDescriptor)panelClass.Styles[1];
            Assert.AreEqual(backroundStyle.Color, "#222222");
            Assert.IsFalse(backroundStyle.RepeatX);
            Assert.IsFalse(backroundStyle.RepeatY);
            Assert.IsNull(backroundStyle.ImageUrl);

            var contentClass = classes.Classes[2];
            Assert.AreEqual(contentClass.Name, "content");
            Assert.AreEqual(contentClass.Scope, "container");
            Assert.AreEqual(contentClass.Target, StyleClassTarget.ElementName);
            Assert.AreEqual(contentClass.Styles.Count, 3);

            Assert.AreEqual(contentClass.Styles[0].Identifier, StyleIdentifier.Width);
            Assert.AreEqual(contentClass.Styles[1].Identifier, StyleIdentifier.Background);
            Assert.AreEqual(contentClass.Styles[2].Identifier, StyleIdentifier.Padding);

            widthStyle = (WidthStyleDescriptor)contentClass.Styles[0];
            Assert.AreEqual(widthStyle.Unit, SizeUnit.Weight);
            Assert.AreEqual(widthStyle.Value, 1);

            var paddingStyle = (PaddingStyleDescriptor)contentClass.Styles[2];
            Assert.AreEqual(paddingStyle.Top.Unit, SizeUnit.Pixels);
            Assert.AreEqual(paddingStyle.Top.Value, 5);
            Assert.AreEqual(paddingStyle.Right.Unit, SizeUnit.Pixels);
            Assert.AreEqual(paddingStyle.Right.Value, 5);
            Assert.AreEqual(paddingStyle.Bottom.Unit, SizeUnit.Pixels);
            Assert.AreEqual(paddingStyle.Bottom.Value, 5);
            Assert.AreEqual(paddingStyle.Left.Unit, SizeUnit.Pixels);
            Assert.AreEqual(paddingStyle.Left.Value, 5);

            var entriesClass = classes.Classes[3];
            Assert.AreEqual(entriesClass.Name, "entries");
            Assert.AreEqual(entriesClass.Scope, "container");
            Assert.AreEqual(entriesClass.Target, StyleClassTarget.ElementName);
            Assert.AreEqual(entriesClass.Styles.Count, 1);

            var activeClass = classes.Classes[4];
            Assert.AreEqual(activeClass.Name, "active");
            Assert.AreEqual(activeClass.Target, StyleClassTarget.ClassName);
            Assert.AreEqual(activeClass.Styles.Count, 1);
        }
    }
}
