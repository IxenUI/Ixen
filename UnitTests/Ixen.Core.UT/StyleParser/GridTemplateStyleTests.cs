using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.Core.Visual.Styles.Parsers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.StyleParser
{
    [TestClass]
    public class GridTemplateStyleTests
    {
        private static SizeTemplateStyleDescriptor Parse(string value)
        {
            var parser = new SizeTemplateStyleParser(value);

            Assert.IsTrue(parser.IsValid, $"'{value}' should have parsed");

            return parser.Descriptor;
        }

        private static void Refuse(string value)
            => Assert.IsFalse(new SizeTemplateStyleParser(value).IsValid,
                $"'{value}' should have been refused");

        [TestMethod]
        public void ARepeatExpandsIntoPlainTracks()
        {
            SizeTemplateStyleDescriptor descriptor = Parse("repeat(3, 1*)");

            Assert.AreEqual(3, descriptor.Value.Count);

            foreach (SizeStyleDescriptor track in descriptor.Value)
            {
                Assert.AreEqual(SizeUnit.Weight, track.Unit);
                Assert.AreEqual(1, track.Value);
            }
        }

        [TestMethod]
        public void ARepeatOfSeveralTracksCyclesThem()
        {
            SizeTemplateStyleDescriptor descriptor = Parse("repeat(2, 100px 1*)");

            Assert.AreEqual(4, descriptor.Value.Count);
            Assert.AreEqual(SizeUnit.Pixels, descriptor.Value[0].Unit);
            Assert.AreEqual(SizeUnit.Weight, descriptor.Value[1].Unit);
            Assert.AreEqual(SizeUnit.Pixels, descriptor.Value[2].Unit);
            Assert.AreEqual(SizeUnit.Weight, descriptor.Value[3].Unit);
        }

        [TestMethod]
        public void ARepeatSitsBesidePlainTracks()
        {
            SizeTemplateStyleDescriptor descriptor = Parse("200px repeat(2, 1*) 40px");

            Assert.AreEqual(4, descriptor.Value.Count);
            Assert.AreEqual(200, descriptor.Value[0].Value);
            Assert.AreEqual(SizeUnit.Weight, descriptor.Value[1].Unit);
            Assert.AreEqual(SizeUnit.Weight, descriptor.Value[2].Unit);
            Assert.AreEqual(40, descriptor.Value[3].Value);
        }

        [TestMethod]
        public void ARepeatNeedsAWholePositiveCount()
        {
            Refuse("repeat(0, 1*)");
            Refuse("repeat(0, 1*) 100px");
            Refuse("repeat(-2, 1*)");
            Refuse("repeat(1.5, 1*)");
            Refuse("repeat(lots, 1*)");
        }

        [TestMethod]
        [Timeout(10000)]
        public void ARepeatIsCappedRatherThanTrusted()
        {
            Refuse($"repeat({SizeTemplateStyleParser.MAX_TRACKS + 1}, 1px)");

            Assert.AreEqual(SizeTemplateStyleParser.MAX_TRACKS,
                Parse($"repeat({SizeTemplateStyleParser.MAX_TRACKS}, 1px)").Value.Count,
                "the cap itself is still legal");

            Refuse("repeat(2000000000, 1px)");

            Refuse($"repeat({SizeTemplateStyleParser.MAX_TRACKS}, 1px) "
                + $"repeat({SizeTemplateStyleParser.MAX_TRACKS}, 1px)");
        }

        [TestMethod]
        public void ARepeatCannotHoldARepeat()
        {
            Refuse("repeat(2, repeat(2, 1*))");
        }

        [TestMethod]
        public void ARepeatNeedsSomethingToRepeat()
        {
            Refuse("repeat(2)");
            Refuse("repeat()");
        }

        [TestMethod]
        public void AMinmaxIsOneTrackCarryingItsFloor()
        {
            SizeTemplateStyleDescriptor descriptor = Parse("minmax(160px, 1*)");

            Assert.AreEqual(1, descriptor.Value.Count);
            Assert.AreEqual(SizeUnit.Weight, descriptor.Value[0].Unit,
                "the track resolves by its max");
            Assert.IsNotNull(descriptor.Value[0].TrackMin);
            Assert.AreEqual(SizeUnit.Pixels, descriptor.Value[0].TrackMin.Unit);
            Assert.AreEqual(160, descriptor.Value[0].TrackMin.Value);
        }

        [TestMethod]
        public void AMinmaxTakesExactlyTwoBounds()
        {
            Refuse("minmax(160px)");
            Refuse("minmax(160px, 1*, 40px)");
            Refuse("minmax()");
        }

        [TestMethod]
        public void AWeightIsRefusedAsAFloor()
        {
            Refuse("minmax(1*, 300px)");
            Refuse("minmax(1*, 1*)");
        }

        [TestMethod]
        public void ContentIsAcceptedAsAFloor()
        {
            SizeTemplateStyleDescriptor descriptor = Parse("minmax(?, 300px)");

            Assert.AreEqual(SizeUnit.Content, descriptor.Value[0].TrackMin.Unit);
        }

        [TestMethod]
        public void AMinmaxNestsInsideARepeat()
        {
            SizeTemplateStyleDescriptor descriptor = Parse("repeat(2, minmax(160px, 1*))");

            Assert.AreEqual(2, descriptor.Value.Count);
            Assert.AreEqual(160, descriptor.Value[0].TrackMin.Value);
            Assert.AreEqual(160, descriptor.Value[1].TrackMin.Value);
        }

        [TestMethod]
        public void AutoFillKeepsTheGroupOnceAndSaysSo()
        {
            SizeTemplateStyleDescriptor descriptor = Parse("repeat(auto-fill, minmax(160px, 1*))");

            Assert.IsTrue(descriptor.AutoFill);
            Assert.AreEqual(1, descriptor.Value.Count,
                "the group is kept once and the track lookup repeats it");
        }

        [TestMethod]
        public void AutoFillTakesAGroupOfSeveralTracks()
        {
            SizeTemplateStyleDescriptor descriptor = Parse("repeat(auto-fill, 100px 60px)");

            Assert.IsTrue(descriptor.AutoFill);
            Assert.AreEqual(2, descriptor.Value.Count);
        }

        [TestMethod]
        public void AutoFillNeedsADefiniteFloorOnEveryTrack()
        {
            Refuse("repeat(auto-fill, 1*)");
            Refuse("repeat(auto-fill, ?)");
            Refuse("repeat(auto-fill, minmax(?, 1*))");
            Refuse("repeat(auto-fill, 100px 1*)");
        }

        [TestMethod]
        public void AutoFillRefusesAFloorThatWouldNeverFinish()
        {
            Refuse("repeat(auto-fill, 0px)");
            Refuse("repeat(auto-fill, minmax(0px, 1*))");
        }

        [TestMethod]
        public void AutoFillIsTheWholeTemplateOrNothing()
        {
            Refuse("100px repeat(auto-fill, 100px)");
            Refuse("repeat(auto-fill, 100px) 100px");
            Refuse("repeat(auto-fill, 100px) repeat(auto-fill, 100px)");
        }

        [TestMethod]
        public void AutoFitIsRefusedRatherThanTakenAsASynonym()
        {
            Refuse("repeat(auto-fit, minmax(160px, 1*))");
        }

        [TestMethod]
        public void AutoFillIsRefusedOnTheRowHeights()
        {
            Assert.IsTrue(new RowTemplateStyleParser("repeat(auto-fill, 100px)").IsValid,
                "row-template declares the column widths, which is the axis with a width to fill");

            Assert.IsFalse(new ColumnTemplateStyleParser("repeat(auto-fill, 100px)").IsValid,
                "column-template declares the row heights, and the row count comes from the children");
        }

        [TestMethod]
        public void AnUnbalancedParenthesisIsRefused()
        {
            Refuse("repeat(2, 1*");
            Refuse("minmax(160px, 1*");
            Refuse("1* )");
        }

        [TestMethod]
        public void APlainTemplateIsUntouched()
        {
            SizeTemplateStyleDescriptor descriptor = Parse("50px 1* 2% ?");

            Assert.AreEqual(4, descriptor.Value.Count);
            Assert.IsFalse(descriptor.AutoFill);

            foreach (SizeStyleDescriptor track in descriptor.Value)
            {
                Assert.IsNull(track.TrackMin);
            }
        }

        [TestMethod]
        public void AFunctionStillWorksAsATrack()
        {
            SizeTemplateStyleDescriptor descriptor = Parse("min(300px, 50%) 1*");

            Assert.AreEqual(SizeFunction.Min, descriptor.Value[0].Function);
            Assert.AreEqual(2, descriptor.Value[0].Parts.Count);
        }
    }
}
