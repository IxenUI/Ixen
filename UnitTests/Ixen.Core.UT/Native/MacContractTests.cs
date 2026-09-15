using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Ixen.Core.UT.Native
{
    [TestClass]
    public class MacContractTests
    {
        private const string NATIVE = @"Ixen.Platform.Mac.Native\window\native_window.mm";
        private const string HEADER = @"Ixen.Platform.Mac.Native\api\window_api.h";
        private const string KINDS = @"Ixen.Platform.Mac\NativeApi\MacKinds.cs";
        private const string KEYS = @"Ixen.Platform.Mac\NativeApi\MacKeys.cs";
        private const string API = @"Ixen.Platform.Mac\NativeApi\MacApi.cs";

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
            { "IXEN_KEY_CHAR", "Char" },

            { "IXEN_IME_UPDATE", "Update" },
            { "IXEN_IME_COMMIT", "Commit" },
            { "IXEN_IME_CANCEL", "Cancel" },
            { "IXEN_IME_FINISH", "Finish" },

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
                + "The macOS sources are read as text because nothing on a Windows machine can "
                + "compile Objective-C++, so this is the only guard the wire format can have here.");

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
                return "MacPointerKind." + name;
            }

            if (define.StartsWith("IXEN_BUTTON_"))
            {
                return "MacPointerButton." + name;
            }

            if (define.StartsWith("IXEN_KEY_"))
            {
                return "MacKeyKind." + name;
            }

            if (define.StartsWith("IXEN_IME_"))
            {
                return "MacImeKind." + name;
            }

            return name;
        }

        private static Dictionary<string, int> ManagedValues()
        {
            var found = new Dictionary<string, int>();

            string kinds = Read(KINDS);
            string keys = Read(KEYS);

            ReadEnum(kinds, "MacPointerKind", found);
            ReadEnum(kinds, "MacPointerButton", found);
            ReadEnum(kinds, "MacKeyKind", found);
            ReadEnum(kinds, "MacImeKind", found);

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
                    $"{pair.Key} is gone from native_window.mm");

                string name = Managed(pair.Key, pair.Value);

                Assert.IsTrue(managed.TryGetValue(name, out int actual),
                    $"{name} is gone from the managed side");

                Assert.AreEqual(expected, actual,
                    $"{pair.Key} is {expected} in native_window.mm and {name} is {actual} "
                        + "on the managed side. Nothing but this test can notice that on a "
                        + "machine with no Objective-C++ compiler.");
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
                "the .mm is parsed by regex, so a change of shape has to fail loudly rather "
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
                    $"MacApi asks for {entry}, which window_api.h does not export. On a Mac that "
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
                Assert.IsTrue(managed.Success, entry + " is gone from MacApi.cs");

                Assert.AreEqual(ArgumentsOf(native.Groups[1].Value),
                    ArgumentsOf(managed.Groups[1].Value),
                    $"{entry} takes a different number of arguments on each side. Widening one "
                        + "alone is a compile error on neither, so the managed delegate would "
                        + "read whatever the Objective-C++ never pushed.");
            }
        }

        [TestMethod]
        public void TheLetterCodesAgreeWithTheKeycodeTable()
        {
            Match body = Regex.Match(Read(NATIVE),
                @"static const int LetterCodes\[26\]\s*=\s*\{([^}]*)\}");

            Assert.IsTrue(body.Success,
                "no LetterCodes table to read; the parser needs updating");

            MatchCollection codes = Regex.Matches(body.Groups[1].Value, @"0x([0-9A-Fa-f]{2})");

            Assert.AreEqual(26, codes.Count, "the table is one entry per letter");

            var table = new Dictionary<int, string>();

            foreach (Match match in Regex.Matches(Read(KEYS),
                @"case 0x([0-9A-Fa-f]{2}): return Key\.([A-Z]);"))
            {
                table[Convert.ToInt32(match.Groups[1].Value, 16)] = match.Groups[2].Value;
            }

            for (int index = 0; index < 26; index++)
            {
                string letter = ((char)('A' + index)).ToString();
                int code = Convert.ToInt32(codes[index].Groups[1].Value, 16);

                Assert.IsTrue(table.TryGetValue(code, out string mapped),
                    $"native_window.mm says {letter} is keycode 0x{code:X2}, which MacKeys "
                        + "maps to nothing at all");

                Assert.AreEqual(letter, mapped,
                    $"native_window.mm says {letter} is keycode 0x{code:X2} while MacKeys "
                        + $"reads that as {mapped}. A letter key would then run the wrong "
                        + "command on any keyboard whose layout is not the one the table "
                        + "was written against.");
            }
        }
    }
}
