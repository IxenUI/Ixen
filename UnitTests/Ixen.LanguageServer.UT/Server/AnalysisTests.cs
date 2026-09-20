using System.Collections.Generic;
using Ixen.Core.Language.Base;
using Ixen.LanguageServer.Server;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Ixen.LanguageServer.UT.Server
{
    [TestClass]
    public class AnalysisTests
    {
        private static string CodesOf(IReadOnlyList<LanguageError> errors)
        {
            List<string> codes = new List<string>();

            foreach (LanguageError error in errors)
            {
                codes.Add(error.Code);
            }

            return string.Join(",", codes);
        }

        [TestMethod]
        public void AGoodStylesheetReportsNothing()
        {
            Assert.AreEqual(0, Analysis.Diagnose("a.xns", "panel {\n    width: 200px\n}\n").Count);
        }

        [TestMethod]
        public void AGoodViewReportsNothing()
        {
            Assert.AreEqual(0, Analysis.Diagnose("a.xnl", "root {} [\n    child { text: \"Hi\" }\n]\n").Count);
        }

        [TestMethod]
        public void AnUnknownStyleIsReported()
        {
            IReadOnlyList<LanguageError> errors = Analysis.Diagnose("a.xns", "panel {\n    widht: 200px\n}\n");

            Assert.AreEqual(LanguageErrorCode.UNKNOWN_STYLE, CodesOf(errors));
        }

        [TestMethod]
        public void AnInvalidValueIsReported()
        {
            IReadOnlyList<LanguageError> errors = Analysis.Diagnose("a.xns", "panel {\n    width: nonsense\n}\n");

            Assert.AreEqual(LanguageErrorCode.INVALID_STYLE_VALUE, CodesOf(errors));
        }

        [TestMethod]
        public void TheStylesheetIsCompiledRatherThanOnlyTokenized()
        {
            IReadOnlyList<LanguageError> errors = Analysis.Diagnose("a.xns", "panel {\n    widht: 200px\n}\n");

            Assert.AreEqual(1, errors.Count,
                "XN002 comes from the compiler, so a tokenize-only analysis would report nothing");
        }

        [TestMethod]
        public void AnErrorPointsAtWhatIsWrong()
        {
            IReadOnlyList<LanguageError> errors = Analysis.Diagnose("a.xns", "panel {\n    widht: 200px\n}\n");

            Assert.AreEqual(12, errors[0].Index);
            Assert.AreEqual(5, errors[0].Length);
        }

        [TestMethod]
        public void ASyntaxErrorInAViewIsReported()
        {
            IReadOnlyList<LanguageError> errors = Analysis.Diagnose("a.xnl", "root {} [\n    child { text: \"open\n]\n");

            Assert.IsTrue(errors.Count > 0);
            Assert.AreEqual(LanguageErrorCode.SYNTAX, errors[0].Code);
        }

        [TestMethod]
        public void AnUnknownExtensionIsNotAnalysed()
        {
            Assert.AreEqual(0, Analysis.Diagnose("a.txt", "!!!").Count);
        }

        [TestMethod]
        public void AnalysingTwiceGivesTheSameAnswer()
        {
            string text = "panel {\n    widht: 200px\n}\n";

            Assert.AreEqual(CodesOf(Analysis.Diagnose("a.xns", text)), CodesOf(Analysis.Diagnose("a.xns", text)));
        }
    }
}
