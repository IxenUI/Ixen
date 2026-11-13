using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Layout.Geometry
{
    [TestClass]
    public class ContentSizedFillGeometryTests : BaseGeometryTests
    {
        [TestMethod]
        public void AContentSizedColumnIsAsTallAsItsContentEvenWithAFillChild()
        {
            VisualElement column = Element("column", LayoutType.Column, SizeUnit.Pixels, 200, SizeUnit.Content);
            VisualElement fill = Element("fill");
            VisualElement box = Element("box", widthUnit: SizeUnit.Pixels, widthValue: 100,
                heightUnit: SizeUnit.Pixels, heightValue: 40);

            column.AddChild(fill);
            column.AddChild(box);
            Layout(column);

            Assert.AreEqual(40, column.ActualHeight,
                "a weight shares what the container has left over; a container sized to its content "
                + "has nothing left over, so the weight child is its content size, as a grow in CSS");
        }
    }
}
