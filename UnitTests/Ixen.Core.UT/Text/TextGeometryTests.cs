using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.Core.UT.Layout.Geometry;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Text
{
    [TestClass]
    public class TextGeometryTests : BaseGeometryTests
    {
        private static VisualElement Label(string text, float fontSize = 16)
        {
            var element = Element("label", LayoutType.Column, SizeUnit.Content, 0, SizeUnit.Content, 0);
            element.Styles.FontSize = new FontSizeStyleDescriptor { Value = fontSize };
            element.Text = text;
            return element;
        }

        [TestMethod]
        public void AContentSizedLabel_TakesTheSizeOfItsText()
        {
            VisualElement label = Label("Coucou");

            Layout(label);

            Assert.IsTrue(label.Width > 0, $"width was {label.Width}");
            Assert.IsTrue(label.Height > 0, $"height was {label.Height}");
        }

        [TestMethod]
        public void ALongerText_MeasuresWider()
        {
            VisualElement shortLabel = Label("Hi");
            VisualElement longLabel = Label("Hi there, this is longer");

            Layout(shortLabel);
            Layout(longLabel);

            Assert.IsTrue(longLabel.Width > shortLabel.Width,
                $"short={shortLabel.Width} long={longLabel.Width}");
        }

        [TestMethod]
        public void ABiggerFontSize_MeasuresTallerAndWider()
        {
            VisualElement small = Label("Coucou", 10);
            VisualElement big = Label("Coucou", 30);

            Layout(small);
            Layout(big);

            Assert.IsTrue(big.Width > small.Width, $"small={small.Width} big={big.Width}");
            Assert.IsTrue(big.Height > small.Height, $"small={small.Height} big={big.Height}");
        }

        [TestMethod]
        public void AnExplicitSize_WinsOverTheTextSize()
        {
            var label = Element("label", LayoutType.Column, SizeUnit.Pixels, 300, SizeUnit.Pixels, 40);
            label.Styles.FontSize = new FontSizeStyleDescriptor { Value = 16 };
            label.Text = "Coucou";

            Layout(label);

            AssertBox(label, 0, 0, 300, 40);
        }

        [TestMethod]
        public void PaddingIsAddedAroundTheMeasuredText()
        {
            VisualElement bare = Label("Coucou");
            VisualElement padded = WithPadding(Label("Coucou"), 10);

            Layout(bare);
            Layout(padded);

            Assert.AreEqual(bare.Width + 20, padded.Width, "width");
            Assert.AreEqual(bare.Height + 20, padded.Height, "height");
        }

        [TestMethod]
        public void AnEmptyText_MeasuresToNothing()
        {
            VisualElement empty = Label(null);

            Layout(empty);

            AssertBox(empty, 0, 0, 0, 0);
        }

        [TestMethod]
        public void ALabelInARow_PushesItsSibling()
        {
            var row = Element("row", LayoutType.Row, SizeUnit.Pixels, 400, SizeUnit.Pixels, 50);
            VisualElement label = Label("Coucou");
            var next = Element("next", LayoutType.Column, SizeUnit.Pixels, 20, SizeUnit.Pixels, 20);
            row.AddChildren(label, next);

            Layout(row);

            Assert.IsTrue(label.Width > 0, "the label should be measured");
            Assert.AreEqual(label.Width, next.X, "the sibling starts after the measured text");
        }

        [TestMethod]
        public void SettingTheText_InvalidatesTheLayout()
        {
            VisualElement label = Label("Hi");
            var root = Element("root", LayoutType.Row);
            root.AddChild(label);

            var surface = new IxenSurface(root);
            surface.ComputeLayout(400, 400);

            float before = label.Width;
            Assert.IsTrue(before > 0, "the label should have been measured");

            label.Text = "Something much longer than before";
            surface.ComputeLayout(400, 400);

            Assert.IsTrue(label.Width > before, $"before={before} after={label.Width}");
        }
    }
}
