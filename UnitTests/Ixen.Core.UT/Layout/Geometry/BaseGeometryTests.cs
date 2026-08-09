using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Layout.Geometry
{
    public abstract class BaseGeometryTests
    {
        protected const int ViewportWidth = 400;
        protected const int ViewportHeight = 400;

        protected static VisualElement Layout(VisualElement subject, int width = ViewportWidth, int height = ViewportHeight)
        {
            var viewport = new VisualElement { Name = "viewport" };
            viewport.AddChild(subject);

            var surface = new IxenSurface(viewport);
            surface.ComputeLayout(width, height);

            return subject;
        }

        protected static VisualElement Element(string name, LayoutType layout = LayoutType.Column,
            SizeUnit widthUnit = SizeUnit.Unset, float widthValue = 1,
            SizeUnit heightUnit = SizeUnit.Unset, float heightValue = 1)
        {
            var element = new VisualElement { Name = name };
            element.Styles.Layout = new LayoutStyleDescriptor { Type = layout };
            element.Styles.Width = new WidthStyleDescriptor { Unit = widthUnit, Value = widthValue };
            element.Styles.Height = new HeightStyleDescriptor { Unit = heightUnit, Value = heightValue };
            return element;
        }

        protected static VisualElement WithMargin(VisualElement element, float value)
        {
            var descriptor = new MarginStyleDescriptor();
            descriptor.Set(UniformSpace(value));
            element.Styles.Margin = descriptor;
            return element;
        }

        protected static VisualElement WithPadding(VisualElement element, float value)
        {
            var descriptor = new PaddingStyleDescriptor();
            descriptor.Set(UniformSpace(value));
            element.Styles.Padding = descriptor;
            return element;
        }

        private static SpaceStyleDescriptor UniformSpace(float value)
            => new SpaceStyleDescriptor
            {
                Top = Pixels(value),
                Right = Pixels(value),
                Bottom = Pixels(value),
                Left = Pixels(value)
            };

        private static SizeStyleDescriptor Pixels(float value)
            => new SizeStyleDescriptor { Unit = SizeUnit.Pixels, Value = value };

        private static string Label(VisualElement element)
            => string.IsNullOrEmpty(element.Name) ? "(unnamed)" : element.Name;

        private static void AssertFloat(float expected, float actual, string label)
            => Assert.AreEqual(expected: expected, actual: actual, message: label);

        protected static void AssertBox(VisualElement element, float x, float y, float width, float height)
        {
            string label = Label(element);

            AssertFloat(x, element.X, $"{label}.X");
            AssertFloat(y, element.Y, $"{label}.Y");
            AssertFloat(width, element.Width, $"{label}.Width");
            AssertFloat(height, element.Height, $"{label}.Height");
        }

        protected static void AssertActualSize(VisualElement element, float actualWidth, float actualHeight)
        {
            string label = Label(element);

            AssertFloat(actualWidth, element.ActualWidth, $"{label}.ActualWidth");
            AssertFloat(actualHeight, element.ActualHeight, $"{label}.ActualHeight");
        }

        protected static void AssertContentSize(VisualElement element, float contentWidth, float contentHeight)
        {
            string label = Label(element);

            AssertFloat(contentWidth, element.ContentWidth, $"{label}.ContentWidth");
            AssertFloat(contentHeight, element.ContentHeight, $"{label}.ContentHeight");
        }

        protected static void AssertBoxSize(VisualElement element, float boxWidth, float boxHeight)
        {
            string label = Label(element);

            AssertFloat(boxWidth, element.BoxWidth, $"{label}.BoxWidth");
            AssertFloat(boxHeight, element.BoxHeight, $"{label}.BoxHeight");
        }

        protected static void AssertPadding(VisualElement element, float left, float top, float right, float bottom)
        {
            string label = Label(element);

            AssertFloat(left, element.PaddingLeft, $"{label}.PaddingLeft");
            AssertFloat(top, element.PaddingTop, $"{label}.PaddingTop");
            AssertFloat(right, element.PaddingRight, $"{label}.PaddingRight");
            AssertFloat(bottom, element.PaddingBottom, $"{label}.PaddingBottom");
        }
    }
}
