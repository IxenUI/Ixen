using Ixen.Core.Visual.Shapes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text;

namespace Ixen.Core.UT.Shapes
{
    [TestClass]
    public class SvgDocumentTests
    {
        private const float TOLERANCE = 0.01f;

        private static SvgDocument Parse(string source)
        {
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(source)))
            {
                return SvgDocument.Parse(stream);
            }
        }

        private static SvgDocument One(string body)
            => Parse("<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 24 24\">"
                + body + "</svg>");

        [TestMethod]
        public void TheViewBoxIsTheNaturalSize()
        {
            SvgDocument document = Parse(
                "<svg viewBox=\"2 3 48 24\"><path d=\"M 0 0 L 1 1\"/></svg>");

            Assert.AreEqual(2f, document.Left, TOLERANCE);
            Assert.AreEqual(3f, document.Top, TOLERANCE);
            Assert.AreEqual(48f, document.Width, TOLERANCE);
            Assert.AreEqual(24f, document.Height, TOLERANCE);
        }

        [TestMethod]
        public void WithNoViewBoxTheWidthAndHeightAreTakenInstead()
        {
            SvgDocument document = Parse(
                "<svg width=\"16px\" height=\"32\"><path d=\"M 0 0 L 1 1\"/></svg>");

            Assert.AreEqual(16f, document.Width, TOLERANCE, "a px suffix is a length, not a unit");
            Assert.AreEqual(32f, document.Height, TOLERANCE);
        }

        [TestMethod]
        public void ADocumentWithNoSizeAtAllIsRefused()
        {
            Assert.IsNull(Parse("<svg><path d=\"M 0 0 L 1 1\"/></svg>"));
        }

        [TestMethod]
        public void SomethingThatIsNotAnSvgIsRefused()
        {
            Assert.IsNull(Parse("<html><body/></html>"));
            Assert.IsNull(Parse("not xml at all"));
            Assert.IsNull(Parse(string.Empty));
            Assert.IsNull(SvgDocument.Parse(null));
        }

        [TestMethod]
        public void EveryShapeBecomesAPath()
        {
            SvgDocument document = One(
                "<rect x=\"1\" y=\"2\" width=\"10\" height=\"4\"/>"
                + "<circle cx=\"12\" cy=\"12\" r=\"5\"/>"
                + "<ellipse cx=\"6\" cy=\"6\" rx=\"4\" ry=\"2\"/>"
                + "<line x1=\"0\" y1=\"0\" x2=\"8\" y2=\"8\"/>"
                + "<polyline points=\"0,0 4,4 8,0\"/>"
                + "<polygon points=\"0,0 4,4 8,0\"/>"
                + "<path d=\"M 0 0 L 3 3\"/>");

            Assert.AreEqual(7, document.Shapes.Count,
                "a rect, a circle, an ellipse, a line, a polyline, a polygon and a path are all "
                + "one d string once they are read");
        }

        [TestMethod]
        public void ARectangleKeepsItsBoxAndARoundedOneItsCorners()
        {
            SvgDocument plain = One("<rect x=\"1\" y=\"2\" width=\"10\" height=\"4\"/>");

            Assert.AreEqual(1f, plain.Shapes[0].Geometry.Left, TOLERANCE);
            Assert.AreEqual(2f, plain.Shapes[0].Geometry.Top, TOLERANCE);
            Assert.AreEqual(10f, plain.Shapes[0].Geometry.Width, TOLERANCE);
            Assert.AreEqual(4f, plain.Shapes[0].Geometry.Height, TOLERANCE);

            SvgDocument round = One("<rect x=\"0\" y=\"0\" width=\"10\" height=\"10\" rx=\"3\"/>");

            Assert.AreEqual(10f, round.Shapes[0].Geometry.Width, TOLERANCE,
                "a radius cuts the corners and leaves the box where it was");
            Assert.AreEqual(10f, round.Shapes[0].Geometry.Height, TOLERANCE);
        }

        [TestMethod]
        public void OneRadiusOnARectangleAnswersForBoth()
        {
            SvgDocument document = One("<rect width=\"10\" height=\"10\" ry=\"2\"/>");

            Assert.AreEqual(1, document.Shapes.Count,
                "ry alone is rx too, which is what SVG says");
        }

        [TestMethod]
        public void ACircleSpansItsDiameter()
        {
            PathGeometry geometry = One("<circle cx=\"12\" cy=\"8\" r=\"5\"/>").Shapes[0].Geometry;

            Assert.AreEqual(7f, geometry.Left, TOLERANCE);
            Assert.AreEqual(3f, geometry.Top, TOLERANCE);
            Assert.AreEqual(10f, geometry.Width, TOLERANCE);
            Assert.AreEqual(10f, geometry.Height, TOLERANCE);
        }

        [TestMethod]
        public void AShapeWithNoSizeIsSkippedRatherThanDrawn()
        {
            Assert.AreEqual(0, One("<rect width=\"0\" height=\"10\"/>").Shapes.Count);
            Assert.AreEqual(0, One("<circle cx=\"5\" cy=\"5\" r=\"0\"/>").Shapes.Count);
        }

        [TestMethod]
        public void TheFillDefaultsToBlackAndTheStrokeToNothing()
        {
            SvgShape shape = One("<path d=\"M 0 0 L 1 1\"/>").Shapes[0];

            Assert.AreEqual("#000000", shape.Fill, "which is what SVG says a shape is");
            Assert.IsNull(shape.Stroke);
        }

        [TestMethod]
        public void NoneIsNotAColour()
        {
            SvgShape shape = One("<path d=\"M 0 0 L 1 1\" fill=\"none\" stroke=\"red\"/>").Shapes[0];

            Assert.IsNull(shape.Fill);
            Assert.AreEqual("red", shape.Stroke);
        }

        [TestMethod]
        public void APaintTravelsAsItWasWritten()
        {
            Assert.AreEqual("white",
                One("<path d=\"M 0 0 L 1 1\" fill=\"white\"/>").Shapes[0].Fill,
                "a named colour is resolved where it is painted, not here");

            Assert.AreEqual("#fff",
                One("<path d=\"M 0 0 L 1 1\" fill=\"#fff\"/>").Shapes[0].Fill);
        }

        [TestMethod]
        public void AGradientReferenceIsNotAColourEither()
        {
            Assert.IsNull(One("<path d=\"M 0 0 L 1 1\" fill=\"url(#grad)\"/>").Shapes[0].Fill,
                "nothing here reads a gradient, so the honest answer is to paint nothing "
                + "rather than to paint black");
        }

        [TestMethod]
        public void CurrentColourIsBlackBecauseAnImageHasNoContext()
        {
            Assert.AreEqual("#000000",
                One("<path d=\"M 0 0 L 1 1\" fill=\"currentColor\"/>").Shapes[0].Fill);
        }

        [TestMethod]
        public void AGroupHandsItsPaintDown()
        {
            SvgDocument document = One(
                "<g fill=\"red\" stroke-width=\"3\"><path d=\"M 0 0 L 1 1\"/>"
                + "<path d=\"M 2 2 L 3 3\" fill=\"blue\"/></g>");

            Assert.AreEqual("red", document.Shapes[0].Fill);
            Assert.AreEqual(3f, document.Shapes[0].StrokeWidth, TOLERANCE);
            Assert.AreEqual("blue", document.Shapes[1].Fill, "and a child may take it back");
            Assert.AreEqual(3f, document.Shapes[1].StrokeWidth, TOLERANCE);
        }

        [TestMethod]
        public void AGroupsPaintStopsAtItsClosingTag()
        {
            SvgDocument document = One(
                "<g fill=\"red\"><path d=\"M 0 0 L 1 1\"/></g><path d=\"M 2 2 L 3 3\"/>");

            Assert.AreEqual("red", document.Shapes[0].Fill);
            Assert.AreEqual("#000000", document.Shapes[1].Fill,
                "the second path is outside the group and is black again");
        }

        [TestMethod]
        public void AStyleAttributeSaysTheSameThingsAndWins()
        {
            SvgShape shape = One(
                "<path d=\"M 0 0 L 1 1\" fill=\"red\" style=\"fill:#00FF00;stroke-width:4\"/>")
                .Shapes[0];

            Assert.AreEqual("#00FF00", shape.Fill,
                "a style declaration beats the presentation attribute beside it, which is what "
                + "CSS says and what every exported SVG relies on");
            Assert.AreEqual(4f, shape.StrokeWidth, TOLERANCE);
        }

        [TestMethod]
        public void TheFillRuleTravelsIntoTheGeometry()
        {
            Assert.IsTrue(One("<path d=\"M 0 0 L 1 1\" fill-rule=\"evenodd\"/>")
                .Shapes[0].Geometry.EvenOdd);

            Assert.IsFalse(One("<path d=\"M 0 0 L 1 1\"/>").Shapes[0].Geometry.EvenOdd);
        }

        [TestMethod]
        public void OpacityMultipliesDownTheTree()
        {
            SvgShape shape = One(
                "<g opacity=\"0.5\"><path d=\"M 0 0 L 1 1\" fill-opacity=\"0.5\"/></g>").Shapes[0];

            Assert.AreEqual(0.25f, shape.FillAlpha, TOLERANCE,
                "a group's opacity and a shape's own both apply");
            Assert.AreEqual(0.5f, shape.StrokeAlpha, TOLERANCE);
        }

        [TestMethod]
        public void APercentageIsAnOpacityToo()
        {
            Assert.AreEqual(0.4f,
                One("<path d=\"M 0 0 L 1 1\" fill-opacity=\"40%\"/>").Shapes[0].FillAlpha,
                TOLERANCE);
        }

        [TestMethod]
        public void ATranslateMovesAShapeWithoutMovingItsGeometry()
        {
            SvgShape shape = One(
                "<g transform=\"translate(5 7)\"><path d=\"M 0 0 L 1 1\"/></g>").Shapes[0];

            Assert.AreEqual(0f, shape.Geometry.Left, TOLERANCE,
                "the d is untouched; where it lands is the matrix's business");
            Assert.AreEqual(5f, shape.Transform.TransX, TOLERANCE);
            Assert.AreEqual(7f, shape.Transform.TransY, TOLERANCE);
        }

        [TestMethod]
        public void TransformsAccumulateFromTheOutsideIn()
        {
            SvgShape shape = One(
                "<g transform=\"translate(10 0)\">"
                + "<path d=\"M 0 0 L 1 1\" transform=\"scale(2)\"/></g>").Shapes[0];

            shape.Transform.Map(1, 1, out float x, out float y);

            Assert.AreEqual(12f, x, TOLERANCE, "scaled first, then translated");
            Assert.AreEqual(2f, y, TOLERANCE);
        }

        [TestMethod]
        public void SeveralOperationsInOneAttributeComposeLeftToRight()
        {
            SvgShape shape = One(
                "<path d=\"M 0 0 L 1 1\" transform=\"translate(4 0) scale(3)\"/>").Shapes[0];

            shape.Transform.Map(1, 0, out float x, out float y);

            Assert.AreEqual(7f, x, TOLERANCE);
            Assert.AreEqual(0f, y, TOLERANCE);
        }

        [TestMethod]
        public void AMatrixIsReadInSvgsOwnOrder()
        {
            SvgShape shape = One(
                "<path d=\"M 0 0 L 1 1\" transform=\"matrix(1 0 0 1 6 9)\"/>").Shapes[0];

            Assert.AreEqual(6f, shape.Transform.TransX, TOLERANCE);
            Assert.AreEqual(9f, shape.Transform.TransY, TOLERANCE);
        }

        [TestMethod]
        public void ARotationAboutAPointTurnsAroundThatPoint()
        {
            SvgShape shape = One(
                "<path d=\"M 0 0 L 1 1\" transform=\"rotate(90 5 5)\"/>").Shapes[0];

            shape.Transform.Map(5, 0, out float x, out float y);

            Assert.AreEqual(10f, x, TOLERANCE);
            Assert.AreEqual(5f, y, TOLERANCE);
        }

        [TestMethod]
        public void AnUnsupportedElementIsSkippedRatherThanRefused()
        {
            SvgDocument document = One(
                "<text x=\"0\" y=\"0\">hello</text>"
                + "<path d=\"M 0 0 L 1 1\"/>");

            Assert.AreEqual(1, document.Shapes.Count,
                "an element with nothing to become is left out and the rest of the file is "
                + "still read");
        }

        [TestMethod]
        public void AShapeInsideDefsIsATemplateAndIsNotDrawn()
        {
            SvgDocument document = One(
                "<defs><path d=\"M 9 9 L 9 9\"/></defs>"
                + "<path d=\"M 0 0 L 1 1\"/>");

            Assert.AreEqual(1, document.Shapes.Count,
                "defs, symbol, clipPath and mask hold things to be referenced rather than things "
                + "to paint, so a sprite sheet does not come out as every icon on top of each "
                + "other");
        }

        [TestMethod]
        public void WhatFollowsATemplateIsDrawnAgain()
        {
            SvgDocument document = One(
                "<defs><g><path d=\"M 9 9 L 9 9\"/></g></defs>"
                + "<g><path d=\"M 0 0 L 1 1\"/></g>");

            Assert.AreEqual(1, document.Shapes.Count,
                "the skip is bounded by the depth it started at rather than latched, so the "
                + "sibling after it is read normally");
        }

        [TestMethod]
        public void ADocumentTypeIsRefusedRatherThanExpanded()
        {
            Assert.IsNull(Parse(
                "<!DOCTYPE svg [<!ENTITY x \"boom\">]>"
                + "<svg viewBox=\"0 0 1 1\"><path d=\"M 0 0 L 1 1\"/></svg>"),
                "entity expansion is the oldest way to turn a picture into an attack, so the "
                + "reader refuses a doctype outright");
        }
    }
}
