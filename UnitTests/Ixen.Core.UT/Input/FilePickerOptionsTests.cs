using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.Core.UT.Input
{
    [TestClass]
    public class FilePickerOptionsTests
    {
        [TestMethod]
        public void FiltersBecomeTheLinesTheNativeDialogReads()
        {
            var options = new FilePickerOptions();

            options.Filters.Add(new FileFilter("Music", "mp3", ".flac", "*.m4a", " "));
            options.Filters.Add(new FileFilter("Every file"));
            options.Filters.Add(new FileFilter(null, "m3u"));

            Assert.AreEqual("Music|*.mp3;*.flac;*.m4a\nEvery file|*.*\n*.m3u|*.m3u", options.FilterText(),
                "an extension is written however the caller writes it - bare, dotted or starred - "
                + "and a filter with none is every file, a filter with no name is named by its pattern");
        }

        [TestMethod]
        public void ANameCannotBreakTheLineFormat()
        {
            var options = new FilePickerOptions();

            options.Filters.Add(new FileFilter("A|B\nC", "txt"));

            Assert.AreEqual("A B C|*.txt", options.FilterText(),
                "the native side splits on | and on new lines, so neither may survive in a name");
        }
    }
}
