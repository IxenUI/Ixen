using Ixen.Core.UT.Layout.Geometry;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Text
{
    [TestClass]
    public class TextVerticalEllipsisTests : BaseGeometryTests
    {
        private const string LONG_TEXT =
            "the quick brown fox jumps over the lazy dog and then walks back again slowly";

        private const string ELLIPSIS = "…";

        private static VisualElement Sized(string name, SizeUnit widthUnit, float widthValue,
            SizeUnit heightUnit, float heightValue)
        {
            VisualElement element = Element(name, LayoutType.Column,
                widthUnit, widthValue, heightUnit, heightValue);

            element.Styles.FontSize = new FontSizeStyleDescriptor { Value = 16 };
            element.Styles.LineHeight = new LineHeightStyleDescriptor
            {
                Kind = LineHeightKind.Pixels,
                Value = 20
            };

            element.Styles.TextOverflow = new TextOverflowStyleDescriptor
            {
                Value = TextOverflow.Ellipsis
            };

            return element;
        }

        private static VisualElement Label(SizeUnit heightUnit, float heightValue, bool ellipsis)
        {
            VisualElement element = Element("label", LayoutType.Column,
                SizeUnit.Pixels, 120, heightUnit, heightValue);

            element.Styles.FontSize = new FontSizeStyleDescriptor { Value = 16 };
            element.Styles.LineHeight = new LineHeightStyleDescriptor
            {
                Kind = LineHeightKind.Pixels,
                Value = 20
            };

            if (ellipsis)
            {
                element.Styles.TextOverflow = new TextOverflowStyleDescriptor
                {
                    Value = TextOverflow.Ellipsis
                };
            }

            element.Text = LONG_TEXT;

            return element;
        }

        private static int LineCount(VisualElement element)
            => element.TextLines == null ? 0 : element.TextLines.Count;

        private static string LastLine(VisualElement element)
            => element.TextLines[element.TextLines.Count - 1];

        [TestMethod]
        public void ADefiniteHeightCutsTheLinesThatDoNotFit()
        {
            VisualElement label = Label(SizeUnit.Pixels, 60, true);

            Layout(label);

            Assert.AreEqual(3, LineCount(label),
                "60 units of content at a 20 unit line height is three lines, and the rest is "
                + "dropped rather than painted past the bottom edge - an element's own painting "
                + "is not clipped by itself, so without this it really does spill");
        }

        [TestMethod]
        public void AndTheLastVisibleLineSaysThereIsMore()
        {
            VisualElement label = Label(SizeUnit.Pixels, 60, true);

            Layout(label);

            Assert.IsTrue(LastLine(label).EndsWith(ELLIPSIS),
                "the marker goes on the last line that is shown, which is the vertical half of "
                + "what text-overflow already did horizontally");
        }

        [TestMethod]
        public void AContentHeightIsNeverTruncated()
        {
            VisualElement definite = Label(SizeUnit.Pixels, 60, true);
            VisualElement content = Label(SizeUnit.Content, 0, true);

            Layout(definite);
            Layout(content);

            Assert.IsTrue(LineCount(content) > LineCount(definite),
                "a ? height is derived from the lines, so truncating would feed back into the "
                + "measurement it came from - the box grows to fit instead, which is what ? means");
            Assert.IsFalse(LastLine(content).EndsWith(ELLIPSIS));
        }

        [TestMethod]
        public void WithoutTheEllipsisNothingIsDropped()
        {
            VisualElement clipped = Label(SizeUnit.Pixels, 60, false);
            VisualElement ellipsised = Label(SizeUnit.Pixels, 60, true);

            Layout(clipped);
            Layout(ellipsised);

            Assert.IsTrue(LineCount(clipped) > LineCount(ellipsised),
                "text-overflow is opt-in, and 'an element is not clipped by itself' is a "
                + "deliberate rule that makes outer borders work - so clip keeps painting, and "
                + "dropping lines is what asking for the marker buys");
        }

        [TestMethod]
        public void OneLineIsAlwaysKept()
        {
            VisualElement label = Label(SizeUnit.Pixels, 4, true);

            Layout(label);

            Assert.AreEqual(1, LineCount(label),
                "a box too short for even one line shows one, truncated - showing nothing at all "
                + "would be worse than showing something");
            Assert.IsTrue(LastLine(label).EndsWith(ELLIPSIS));
        }

        [TestMethod]
        public void ARoomyBoxIsLeftAlone()
        {
            VisualElement label = Label(SizeUnit.Pixels, 400, true);

            Layout(label);

            Assert.IsFalse(LastLine(label).EndsWith(ELLIPSIS),
                "nothing overflows, so nothing is marked");
        }

        [TestMethod]
        public void TheWidthFollowsTheLinesThatAreLeft()
        {
            VisualElement kept = Sized("short", SizeUnit.Content, 0, SizeUnit.Pixels, 20);
            VisualElement both = Sized("both", SizeUnit.Content, 0, SizeUnit.Pixels, 400);

            kept.Text = "short\nan altogether very much longer second line indeed";
            both.Text = kept.Text;

            Layout(kept);
            Layout(both);

            Assert.AreEqual(1, LineCount(kept));
            Assert.AreEqual(2, LineCount(both));

            Assert.IsTrue(kept.ActualWidth < both.ActualWidth / 2,
                "the widest line was dropped, so the box must shrink to what is left - keeping "
                + "the width from BuildLines would leave a box sized for a line nobody sees");
        }

        [TestMethod]
        public void AContentHeightInsideAShortBoxIsStillNotTruncated()
        {
            VisualElement parent = Element("parent", LayoutType.Column,
                SizeUnit.Pixels, 140, SizeUnit.Pixels, 30);

            VisualElement label = Sized("label", SizeUnit.Pixels, 120, SizeUnit.Content, 0);

            label.Text = LONG_TEXT;

            parent.AddChild(label);

            Layout(parent);

            Assert.IsTrue(LineCount(label) > 3,
                "the offered height is a loose bound here, not a decision - truncating against "
                + "it is the circularity this feature refuses, and it only shows up when the "
                + "container is smaller than the text");
            Assert.IsFalse(LastLine(label).EndsWith(ELLIPSIS));
        }

        [TestMethod]
        public void TheHeightIsPartOfTheCacheKey()
        {
            VisualElement label = Label(SizeUnit.Pixels, 60, true);

            Layout(label);

            Assert.AreEqual(3, LineCount(label));

            label.Styles.Height = new HeightStyleDescriptor
            {
                Unit = SizeUnit.Pixels,
                Value = 100
            };

            label.Invalidate();
            Layout(label);

            Assert.AreEqual(5, LineCount(label),
                "the layout cache has to know the height it truncated against, or resizing the "
                + "box hands back the line set from the old one");
        }
    }
}
