using Ixen.Core.Input;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Ixen.Core.UT.Native
{
    [TestClass]
    public class LinuxContractTests
    {
        private const string NATIVE = @"Ixen.Platform.Linux.Native\window\native_window.h";
        private const string WINDOW = @"Ixen.Platform.Linux.Native\window\native_window.c";
        private const string HEADER = @"Ixen.Platform.Linux.Native\api\window_api.h";
        private const string KINDS = @"Ixen.Platform.Linux\NativeApi\LinuxKinds.cs";
        private const string KEYS = @"Ixen.Platform.Linux\NativeApi\LinuxKeys.cs";
        private const string API = @"Ixen.Platform.Linux\NativeApi\LinuxApi.cs";

        private static readonly Dictionary<string, string> _pairs = new Dictionary<string, string>
        {
            { "IXEN_POINTER_MOVE", "Move" },
            { "IXEN_POINTER_DOWN", "Down" },
            { "IXEN_POINTER_UP", "Up" },
            { "IXEN_POINTER_LEAVE", "Leave" },
            { "IXEN_POINTER_CAPTURELOST", "CaptureLost" },

            { "IXEN_BUTTON_NONE", "None" },
            { "IXEN_BUTTON_LEFT", "Left" },
            { "IXEN_BUTTON_MIDDLE", "Middle" },
            { "IXEN_BUTTON_RIGHT", "Right" },

            { "IXEN_KEY_DOWN", "Down" },
            { "IXEN_KEY_UP", "Up" },

            { "IXEN_MOD_SHIFT", "MOD_SHIFT" },
            { "IXEN_MOD_CONTROL", "MOD_CONTROL" },
            { "IXEN_MOD_ALT", "MOD_ALT" },
            { "IXEN_MOD_META", "MOD_META" },

            { "IXEN_CURSOR_DEFAULT", "Cursor.Default" },
            { "IXEN_CURSOR_HAND", "Cursor.Hand" },
            { "IXEN_CURSOR_TEXT", "Cursor.Text" },
            { "IXEN_CURSOR_WAIT", "Cursor.Wait" },
            { "IXEN_CURSOR_CROSSHAIR", "Cursor.Crosshair" },
            { "IXEN_CURSOR_RESIZE_H", "Cursor.ResizeHorizontal" },
            { "IXEN_CURSOR_RESIZE_V", "Cursor.ResizeVertical" },
            { "IXEN_CURSOR_RESIZE_DIAGONAL_UP", "Cursor.ResizeDiagonalUp" },
            { "IXEN_CURSOR_RESIZE_DIAGONAL_DOWN", "Cursor.ResizeDiagonalDown" },
            { "IXEN_CURSOR_MOVE", "Cursor.Move" },
            { "IXEN_CURSOR_NOT_ALLOWED", "Cursor.NotAllowed" },
            { "IXEN_CURSOR_HELP", "Cursor.Help" },
            { "IXEN_CURSOR_PROGRESS", "Cursor.Progress" },
            { "IXEN_CURSOR_HIDDEN", "Cursor.Hidden" }
        };

        private static readonly Dictionary<string, string> _cursorNames = new Dictionary<string, string>
        {
            { "IXEN_CURSOR_DEFAULT", "default" },
            { "IXEN_CURSOR_HAND", "pointer" },
            { "IXEN_CURSOR_TEXT", "text" },
            { "IXEN_CURSOR_WAIT", "wait" },
            { "IXEN_CURSOR_CROSSHAIR", "crosshair" },
            { "IXEN_CURSOR_RESIZE_H", "ew-resize" },
            { "IXEN_CURSOR_RESIZE_V", "ns-resize" },
            { "IXEN_CURSOR_RESIZE_DIAGONAL_UP", "nesw-resize" },
            { "IXEN_CURSOR_RESIZE_DIAGONAL_DOWN", "nwse-resize" },
            { "IXEN_CURSOR_MOVE", "move" },
            { "IXEN_CURSOR_NOT_ALLOWED", "not-allowed" },
            { "IXEN_CURSOR_HELP", "help" },
            { "IXEN_CURSOR_PROGRESS", "progress" }
        };

        private static string Read(string relative)
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null)
            {
                string candidate = Path.Combine(directory.FullName, relative);

                if (File.Exists(candidate))
                {
                    return File.ReadAllText(candidate);
                }

                directory = directory.Parent;
            }

            Assert.Fail($"could not find {relative} by walking up from {AppContext.BaseDirectory}. "
                + "The Linux sources are read as text because a Windows machine builds the managed "
                + "half and never the .so, so this is the only guard the wire format has here.");

            return null;
        }

        private static Dictionary<string, int> NativeDefines()
        {
            var found = new Dictionary<string, int>();

            foreach (Match match in Regex.Matches(Read(NATIVE), @"#define\s+(IXEN_[A-Z_]+)\s+(\d+)"))
            {
                found[match.Groups[1].Value] = int.Parse(match.Groups[2].Value);
            }

            return found;
        }

        private static void ReadEnum(string source, string name, Dictionary<string, int> into)
        {
            Match body = Regex.Match(source, @"enum\s+" + name + @"\s*\{([^}]*)\}");

            Assert.IsTrue(body.Success, $"no enum {name} to read; the parser needs updating");

            foreach (Match match in Regex.Matches(body.Groups[1].Value, @"(\w+)\s*=\s*(\d+)"))
            {
                into[name + "." + match.Groups[1].Value] = int.Parse(match.Groups[2].Value);
            }
        }

        private static string Managed(string define, string name)
        {
            if (define.StartsWith("IXEN_POINTER_"))
            {
                return "LinuxPointerKind." + name;
            }

            if (define.StartsWith("IXEN_BUTTON_"))
            {
                return "LinuxPointerButton." + name;
            }

            if (define.StartsWith("IXEN_KEY_"))
            {
                return "LinuxKeyKind." + name;
            }

            return name;
        }

        private static Dictionary<string, int> ManagedValues()
        {
            var found = new Dictionary<string, int>();

            string kinds = Read(KINDS);
            string keys = Read(KEYS);

            ReadEnum(kinds, "LinuxPointerKind", found);
            ReadEnum(kinds, "LinuxPointerButton", found);
            ReadEnum(kinds, "LinuxKeyKind", found);

            foreach (Match match in Regex.Matches(keys, @"const int (MOD_[A-Z]+)\s*=\s*(\d+)"))
            {
                found[match.Groups[1].Value] = int.Parse(match.Groups[2].Value);
            }

            foreach (Match match in Regex.Matches(kinds, @"CursorKind\.(\w+): return (\d+);"))
            {
                found["Cursor." + match.Groups[1].Value] = int.Parse(match.Groups[2].Value);
            }

            Match fallback = Regex.Match(kinds, @"default: return (\d+);");

            Assert.IsTrue(fallback.Success, "no default cursor arm to read; the parser needs updating");

            found["Cursor.Default"] = int.Parse(fallback.Groups[1].Value);

            return found;
        }

        [TestMethod]
        public void EveryNativeDefineHasTheSameValueOnTheManagedSide()
        {
            Dictionary<string, int> native = NativeDefines();
            Dictionary<string, int> managed = ManagedValues();

            foreach (KeyValuePair<string, string> pair in _pairs)
            {
                Assert.IsTrue(native.TryGetValue(pair.Key, out int expected),
                    $"{pair.Key} is gone from native_window.h");

                string name = Managed(pair.Key, pair.Value);

                Assert.IsTrue(managed.TryGetValue(name, out int actual),
                    $"{name} is gone from the managed side");

                Assert.AreEqual(expected, actual,
                    $"{pair.Key} is {expected} in native_window.h and {name} is {actual} "
                        + "on the managed side. Nothing but this test can notice that on a "
                        + "machine that never compiles the .so.");
            }
        }

        [TestMethod]
        public void NoNativeDefineIsLeftOutOfThisTest()
        {
            var missing = new List<string>();

            foreach (string name in NativeDefines().Keys)
            {
                if (!_pairs.ContainsKey(name))
                {
                    missing.Add(name);
                }
            }

            Assert.AreEqual(0, missing.Count,
                "add what it maps to rather than widening the regex to swallow it: "
                    + string.Join(", ", missing));
        }

        [TestMethod]
        public void TheParsersActuallyFoundSomething()
        {
            Assert.AreEqual(_pairs.Count, NativeDefines().Count,
                "the header is parsed by regex, so a change of shape has to fail loudly rather "
                    + "than quietly finding nothing and passing");
        }

        private static List<string> NativeExports()
        {
            var found = new List<string>();

            foreach (Match match in Regex.Matches(Read(HEADER), @"IXEN_API_ENTRY[^;]*?\b(WA_\w+)\s*\("))
            {
                found.Add(match.Groups[1].Value);
            }

            return found;
        }

        private static List<string> ManagedEntryPoints()
        {
            var found = new List<string>();

            foreach (Match match in Regex.Matches(Read(API), @"EntryPoint\s*=\s*""(WA_\w+)"""))
            {
                found.Add(match.Groups[1].Value);
            }

            return found;
        }

        [TestMethod]
        public void EveryEntryPointTheManagedSideAsksForIsExported()
        {
            List<string> exports = NativeExports();

            Assert.AreNotEqual(0, exports.Count, "window_api.h could not be parsed at all");

            foreach (string entry in ManagedEntryPoints())
            {
                Assert.IsTrue(exports.Contains(entry),
                    $"LinuxApi asks for {entry}, which window_api.h does not export. On Linux that "
                        + "is an EntryPointNotFoundException at the first call rather than a "
                        + "build error, so this is the only place it can be caught.");
            }
        }

        [TestMethod]
        public void NoExportIsLeftWithNothingCallingIt()
        {
            List<string> managed = ManagedEntryPoints();
            var unused = new List<string>();

            foreach (string export in NativeExports())
            {
                if (!managed.Contains(export))
                {
                    unused.Add(export);
                }
            }

            Assert.AreEqual(0, unused.Count,
                "an export nothing calls is either a missing DllImport or dead native code: "
                    + string.Join(", ", unused));
        }

        private static int ArgumentsOf(string list)
        {
            int depth = 0;
            int count = 0;
            bool any = false;

            foreach (char character in list)
            {
                if (character == '(')
                {
                    depth++;
                    continue;
                }

                if (character == ')')
                {
                    depth--;
                    continue;
                }

                if (!char.IsWhiteSpace(character))
                {
                    any = true;
                }

                if (character == ',' && depth == 0)
                {
                    count++;
                }
            }

            return any ? count + 1 : 0;
        }

        [TestMethod]
        public void EveryEntryPointTakesTheSameNumberOfArguments()
        {
            string header = Read(HEADER);
            string api = Read(API);

            foreach (string entry in ManagedEntryPoints())
            {
                Match native = Regex.Match(header,
                    @"IXEN_API_ENTRY[^;]*?\b" + entry + @"\s*\(([^;]*?)\)\s*;");

                Match managed = Regex.Match(api,
                    @"EntryPoint\s*=\s*""" + entry
                        + @"""[^;]*?\bextern\s+[\w<>\[\]. ]+?\s*\(([^;]*?)\)\s*;");

                Assert.IsTrue(native.Success, entry + " is gone from window_api.h");
                Assert.IsTrue(managed.Success, entry + " is gone from LinuxApi.cs");

                int nativeCount = ArgumentsOf(native.Groups[1].Value);

                if (nativeCount == 1 && native.Groups[1].Value.Trim() == "void")
                {
                    nativeCount = 0;
                }

                Assert.AreEqual(nativeCount, ArgumentsOf(managed.Groups[1].Value),
                    $"{entry} takes a different number of arguments on each side. Widening one "
                        + "alone is a compile error on neither, so the managed delegate would "
                        + "read whatever the C never pushed.");
            }
        }

        [TestMethod]
        public void TheCursorNamesAreInTheOrderTheDefinesSay()
        {
            Match body = Regex.Match(Read(WINDOW),
                @"CURSOR_NAMES\[CURSOR_COUNT\]\s*=\s*\{([^}]*)\}");

            Assert.IsTrue(body.Success, "no CURSOR_NAMES table to read; the parser needs updating");

            MatchCollection names = Regex.Matches(body.Groups[1].Value, @"""([^""]*)""");

            Assert.AreEqual(_cursorNames.Count, names.Count,
                "the table is one name per cursor, plus a null for the hidden one");

            Dictionary<string, int> defines = NativeDefines();

            foreach (KeyValuePair<string, string> expected in _cursorNames)
            {
                int index = defines[expected.Key];

                Assert.AreEqual(expected.Value, names[index].Groups[1].Value,
                    $"{expected.Key} is {index}, and CURSOR_NAMES[{index}] is "
                        + $"{names[index].Groups[1].Value} rather than {expected.Value}. The "
                        + "table is indexed by the define, so one entry out of order silently "
                        + "shows a different shape for every cursor after it.");
            }
        }
    }
}
