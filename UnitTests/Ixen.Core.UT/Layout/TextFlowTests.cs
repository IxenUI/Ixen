using Ixen.Core.Visual;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Layout
{
    [TestClass]
    public class TextFlowTests
    {
        private const int VIEWPORT = 400;
        private const float PER_CHAR = 10f;
        private const float LINE = 20f;

        private const string LABEL = "abcd";
        private const float LABEL_WIDTH = 40f;

        private sealed class FixedMeasurer : ITextMeasurer
        {
            public void MeasureText(string text, FontSpec font, out float width, out float height)
            {
                width = (text == null ? 0 : text.Length) * PER_CHAR;
                height = LINE;
            }

            public void MeasureCharacters(string text, FontSpec font, float[] advances)
            {
                for (int index = 0; index < (text == null ? 0 : text.Length); index++)
                {
                    advances[index] = PER_CHAR;
                }
            }

            public float GetLineHeight(FontSpec font) => LINE;
        }

        private VisualElement _root;
        private VisualElement _box;
        private IxenSurface _surface;

        [TestInitialize]
        public void Setup()
        {
            _root = new VisualElement { Name = "root" };
            _root.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };

            _box = new VisualElement { Name = "box" };

            _root.AddChild(_box);

            _surface = new IxenSurface(_root)
            {
                Styles = new StyleRegistry(),
                TextMeasurer = new FixedMeasurer()
            };
        }

        private void Layout() => _surface.ComputeLayout(VIEWPORT, VIEWPORT);

        private void Row(string text, float width = 0, float height = 60)
        {
            _box.Text = text;
            _box.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Row };
            Size(_box, width, height);
        }

        private void Column(string text, float width = 300, float height = 0)
        {
            _box.Text = text;
            _box.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Column };
            Size(_box, width, height);
        }

        private static void Size(VisualElement element, float width, float height)
        {
            element.Styles.Width = width > 0
                ? new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = width }
                : new WidthStyleDescriptor { Unit = SizeUnit.Content };

            element.Styles.Height = height > 0
                ? new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = height }
                : new HeightStyleDescriptor { Unit = SizeUnit.Content };
        }

        private VisualElement Item(float width = 50, float height = 20)
        {
            var item = new VisualElement { Name = "item" };
            Size(item, width, height);
            _box.AddChild(item);
            return item;
        }

        private VisualElement Filling()
        {
            var item = new VisualElement { Name = "item" };
            item.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Weight, Value = 1 };
            item.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 20 };
            _box.AddChild(item);
            return item;
        }

        private void WithGap(float value)
            => _box.Styles.Gap = new GapStyleDescriptor { Row = value, Column = value };

        private void WithPadding(float value)
        {
            var space = new SpaceStyleDescriptor
            {
                Top = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = value },
                Right = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = value },
                Bottom = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = value },
                Left = new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = value }
            };

            var padding = new PaddingStyleDescriptor();
            padding.Set(space);
            _box.Styles.Padding = padding;
        }

        [TestMethod]
        public void TheTextTakesItsOwnSlotAndTheChildStartsAfterIt()
        {
            Row(LABEL);
            VisualElement item = Item();

            Layout();

            Assert.IsTrue(_box.TextInFlow);
            Assert.AreEqual(LABEL_WIDTH, _box.TextExtent);
            Assert.AreEqual(0f, _box.TextSlotX);
            Assert.AreEqual(LABEL_WIDTH, _box.TextSlotWidth);
            Assert.AreEqual(LABEL_WIDTH, item.X);
        }

        [TestMethod]
        public void AColumnPutsItsTextAboveItsChildren()
        {
            Column(LABEL);
            VisualElement item = Item(height: 30);

            Layout();

            Assert.AreEqual(LINE, _box.TextExtent);
            Assert.AreEqual(0f, _box.TextSlotY);
            Assert.AreEqual(LINE, _box.TextSlotHeight);
            Assert.AreEqual(LINE, item.Y);
        }

        [TestMethod]
        public void TheTextSlotIsSeparatedFromTheFirstChildByTheGap()
        {
            Row(LABEL);
            WithGap(8);
            VisualElement item = Item();

            Layout();

            Assert.AreEqual(LABEL_WIDTH + 8, item.X);
            Assert.AreEqual(LABEL_WIDTH + 8 + 50, _box.Width);
        }

        [TestMethod]
        public void AContentSizedRowHugsItsTextAndItsChildrenTogether()
        {
            Row(LABEL);
            Item();

            Layout();

            Assert.AreEqual(LABEL_WIDTH + 50, _box.Width);
        }

        [TestMethod]
        public void AContentSizedColumnStacksItsTextAboveItsChildren()
        {
            Column(LABEL);
            Item(height: 30);

            Layout();

            Assert.AreEqual(LINE + 30, _box.Height);
        }

        [TestMethod]
        public void TheSlotAbsorbsTheSlackSoTheChildLandsAtTheFarEnd()
        {
            Row(LABEL, width: 300);
            VisualElement item = Item();

            Layout();

            Assert.AreEqual(250f, _box.TextSlotWidth);
            Assert.AreEqual(250f, item.X);
        }

        [TestMethod]
        public void AColumnSlotAbsorbsTheSlackTheSameWay()
        {
            Column(LABEL, height: 200);
            VisualElement item = Item(height: 30);

            Layout();

            Assert.AreEqual(170f, _box.TextSlotHeight);
            Assert.AreEqual(170f, item.Y);
        }

        [TestMethod]
        public void AFillingChildAbsorbsTheSlackInsteadOfTheText()
        {
            Row(LABEL, width: 300);
            VisualElement item = Filling();

            Layout();

            Assert.AreEqual(LABEL_WIDTH, _box.TextSlotWidth);
            Assert.AreEqual(300 - LABEL_WIDTH, item.Width);
            Assert.AreEqual(LABEL_WIDTH, item.X);
        }

        [TestMethod]
        public void TheSlotFillsTheCrossAxisOfARow()
        {
            Row(LABEL);
            Item();

            Layout();

            Assert.AreEqual(60f, _box.TextSlotHeight);
        }

        [TestMethod]
        public void TheSlotFillsTheCrossAxisOfAColumn()
        {
            Column(LABEL);
            Item();

            Layout();

            Assert.AreEqual(300f, _box.TextSlotWidth);
        }

        [TestMethod]
        public void PaddingMovesTheSlotWithTheChildren()
        {
            Row(LABEL);
            WithPadding(10);
            VisualElement item = Item();

            Layout();

            Assert.AreEqual(10f, _box.TextSlotX);
            Assert.AreEqual(10f, _box.TextSlotY);
            Assert.AreEqual(10 + LABEL_WIDTH, item.X);
        }

        [TestMethod]
        public void ContentAlignHasNothingToSlideWhenTheTextAbsorbsTheSlack()
        {
            Row(LABEL, width: 300);
            _box.Styles.ContentAlign = new ContentAlignStyleDescriptor { Horizontal = ContentAlign.Right };
            VisualElement item = Item();

            Layout();

            Assert.AreEqual(0f, _box.TextSlotX);
            Assert.AreEqual(250f, item.X);
        }

        [TestMethod]
        public void AnElementWithNoChildrenKeepsTheWholeContentBox()
        {
            Row(LABEL, width: 300);

            Layout();

            Assert.IsFalse(_box.TextInFlow);
            Assert.AreEqual(0f, _box.TextExtent);
            Assert.AreEqual(0f, _box.TextSlotX);
            Assert.AreEqual(300f, _box.TextSlotWidth);
            Assert.AreEqual(60f, _box.TextSlotHeight);
        }

        [TestMethod]
        public void AnElementWithNoTextLeavesItsChildrenAtTheContentOrigin()
        {
            Row(null, width: 300);
            VisualElement item = Item();

            Layout();

            Assert.IsFalse(_box.TextInFlow);
            Assert.AreEqual(0f, item.X);
        }

        [TestMethod]
        public void AGridLeavesItsTextOnTheWholeContentBox()
        {
            _box.Text = LABEL;
            _box.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Grid };
            Size(_box, 300, 60);
            Item();

            Layout();

            Assert.IsFalse(_box.TextInFlow);
            Assert.AreEqual(300f, _box.TextSlotWidth);
        }

        [TestMethod]
        public void AnAbsoluteContainerLeavesItsTextOnTheWholeContentBox()
        {
            _box.Text = LABEL;
            _box.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Absolute };
            Size(_box, 300, 60);
            Item();

            Layout();

            Assert.IsFalse(_box.TextInFlow);
            Assert.AreEqual(300f, _box.TextSlotWidth);
        }

        [TestMethod]
        public void AWeightlessLayerChildLeavesTheTextWhereItWas()
        {
            Row(LABEL);

            var layer = new VisualElement { Name = "layer" };
            layer.Styles.Layout = new LayoutStyleDescriptor { Type = LayoutType.Fixed };
            layer.Styles.Width = new WidthStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            layer.Styles.Height = new HeightStyleDescriptor { Unit = SizeUnit.Pixels, Value = 0 };
            _box.AddChild(layer);

            Layout();

            Assert.IsTrue(_box.TextInFlow);
            Assert.AreEqual(0f, _box.TextSlotX);
            Assert.AreEqual(LABEL_WIDTH, _box.Width);
        }

        [TestMethod]
        public void TheTextIsMeasuredAgainstTheWholeContentWidthRatherThanWhatIsLeft()
        {
            Row("abcdefghij", width: 200);
            Item(width: 150);

            Layout();

            Assert.AreEqual(100f, _box.TextExtent);
            Assert.AreEqual(100f, _box.TextSlotWidth);
        }
    }
}
