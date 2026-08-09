using Ixen.Core.Language.Xns;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Xns
{
    [TestClass]
    public class XnsCompilerTests
    {
        [TestMethod]
        public void TestCompile()
        {
            string source = @"container {
    layout: row
    width: 100%
    
    panel-test_Test2 {
        width: 50px
        background: #222222
    }
    
    .content {
        width: 1*
        row-template: 1px
        padding: 5px
    }
    
    #entries {
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
            Assert.AreEqual(5, classes.Classes.Count);

            var containerClass = classes.Classes[0];
            Assert.AreEqual("container", containerClass.Name);
            Assert.AreEqual(StyleClassTarget.ElementName, containerClass.Target);
            Assert.AreEqual(2, containerClass.Styles.Count);

            Assert.AreEqual(StyleIdentifier.LAYOUT, containerClass.Styles[0].Identifier);
            Assert.AreEqual(StyleIdentifier.WIDTH, containerClass.Styles[1].Identifier);

            var layoutStyle = (LayoutStyleDescriptor)containerClass.Styles[0];
            Assert.AreEqual(LayoutType.Row, layoutStyle.Type);

            var widthStyle = (WidthStyleDescriptor)containerClass.Styles[1];
            Assert.AreEqual(SizeUnit.Percents, widthStyle.Unit);
            Assert.AreEqual(100, widthStyle.Value);

            var panelClass = classes.Classes[1];
            Assert.AreEqual("panel-test_Test2", panelClass.Name);
            Assert.AreEqual("container", panelClass.Scope);
            Assert.AreEqual(StyleClassTarget.ElementName, panelClass.Target);
            Assert.AreEqual(2, panelClass.Styles.Count);

            Assert.AreEqual(StyleIdentifier.WIDTH, panelClass.Styles[0].Identifier);
            Assert.AreEqual(StyleIdentifier.BACKGROUND, panelClass.Styles[1].Identifier);

            widthStyle = (WidthStyleDescriptor)panelClass.Styles[0];
            Assert.AreEqual(SizeUnit.Pixels, widthStyle.Unit);
            Assert.AreEqual(50, widthStyle.Value);

            var backroundStyle = (BackgroundStyleDescriptor)panelClass.Styles[1];
            Assert.AreEqual("#222222", backroundStyle.Color);
            Assert.IsFalse(backroundStyle.RepeatX);
            Assert.IsFalse(backroundStyle.RepeatY);
            Assert.IsNull(backroundStyle.ImageUrl);

            var contentClass = classes.Classes[2];
            Assert.AreEqual("content", contentClass.Name);
            Assert.AreEqual("container", contentClass.Scope);
            Assert.AreEqual(StyleClassTarget.ClassName, contentClass.Target);
            Assert.AreEqual(3, contentClass.Styles.Count);

            Assert.AreEqual(StyleIdentifier.WIDTH, contentClass.Styles[0].Identifier);
            Assert.AreEqual(StyleIdentifier.ROW_TEMPLATE, contentClass.Styles[1].Identifier);
            Assert.AreEqual(StyleIdentifier.PADDING, contentClass.Styles[2].Identifier);

            widthStyle = (WidthStyleDescriptor)contentClass.Styles[0];
            Assert.AreEqual(SizeUnit.Weight, widthStyle.Unit);
            Assert.AreEqual(1, widthStyle.Value);

            var paddingStyle = (PaddingStyleDescriptor)contentClass.Styles[2];
            Assert.AreEqual(SizeUnit.Pixels, paddingStyle.Top.Unit);
            Assert.AreEqual(5, paddingStyle.Top.Value);
            Assert.AreEqual(SizeUnit.Pixels, paddingStyle.Right.Unit);
            Assert.AreEqual(5, paddingStyle.Right.Value);
            Assert.AreEqual(SizeUnit.Pixels, paddingStyle.Bottom.Unit);
            Assert.AreEqual(5, paddingStyle.Bottom.Value);
            Assert.AreEqual(SizeUnit.Pixels, paddingStyle.Left.Unit);
            Assert.AreEqual(5, paddingStyle.Left.Value);

            var entriesClass = classes.Classes[3];
            Assert.AreEqual("entries", entriesClass.Name);
            Assert.AreEqual("container", entriesClass.Scope);
            Assert.AreEqual(StyleClassTarget.ElementType, entriesClass.Target);
            Assert.AreEqual(1, entriesClass.Styles.Count);

            var activeClass = classes.Classes[4];
            Assert.AreEqual("active", activeClass.Name);
            Assert.AreEqual(StyleClassTarget.ClassName, activeClass.Target);
            Assert.AreEqual(1, activeClass.Styles.Count);
        }
    }
}
