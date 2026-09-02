using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Ixen.Core.UT.Native
{
    [TestClass]
    public class NativeContractTests
    {
        private const string NATIVE = @"Ixen.Platform.Windows.Native\window\native_window.cpp";
        private const string KINDS = @"Ixen.Platform.Windows\NativeApi\NativePointerKind.cs";
        private const string KEYS = @"Ixen.Platform.Windows\NativeApi\NativeKeys.cs";

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

            { "IXEN_IME_UPDATE", "Update" },
            { "IXEN_IME_COMMIT", "Commit" },
            { "IXEN_IME_CANCEL", "Cancel" },

            { "IXEN_KEY_DOWN", "Down" },
            { "IXEN_KEY_UP", "Up" },
            { "IXEN_KEY_CHAR", "Char" },

            { "IXEN_MOD_SHIFT", "MOD_SHIFT" },
            { "IXEN_MOD_CONTROL", "MOD_CONTROL" },
            { "IXEN_MOD_ALT", "MOD_ALT" },

            { "IXEN_CURSOR_DEFAULT", "Default" },
            { "IXEN_CURSOR_HAND", "Hand" },
            { "IXEN_CURSOR_TEXT", "Text" },
            { "IXEN_CURSOR_WAIT", "Wait" },
            { "IXEN_CURSOR_CROSSHAIR", "Crosshair" },
            { "IXEN_CURSOR_RESIZE_H", "ResizeHorizontal" },
            { "IXEN_CURSOR_RESIZE_V", "ResizeVertical" }
        };

        private const string HEADER = @"Ixen.Platform.Windows.Native\window\native_window.h";
        private const string API = @"Ixen.Platform.Windows\NativeApi\WindowApi.cs";

        private static readonly Dictionary<string, string> _callbacks = new Dictionary<string, string>
        {
            { "_paintCallBack", "OnPaintCallBack" },
            { "_pointerCallBack", "OnPointerCallBack" },
            { "_keyCallBack", "OnKeyCallBack" },
            { "_imeCallBack", "OnImeCallBack" },
            { "_wheelCallBack", "OnWheelCallBack" },
            { "_accessibilityCallBack", "OnAccessibilityCallBack" }
        };

        private static readonly Dictionary<string, string> _returns
            = new Dictionary<string, string>
        {
            { "void", "void" },
            { "__int64", "IntPtr" }
        };

        [TestMethod]
        public void EveryCallbackTakesTheSameNumberOfArguments()
        {
            string header = Read(HEADER);
            string api = Read(API);

            foreach (KeyValuePair<string, string> pair in _callbacks)
            {
                int nativeAt = header.IndexOf(pair.Key + ")(", StringComparison.Ordinal);
                Match managed = Regex.Match(api,
                    @"delegate\s+(\w+)\s+" + pair.Value + @"\s*\(");

                Assert.IsTrue(nativeAt >= 0, pair.Key + " is gone from native_window.h");
                Assert.IsTrue(managed.Success, pair.Value + " is gone from WindowApi");

                int managedAt = managed.Index;

                int expected = Arity(header, nativeAt + pair.Key.Length + 1);
                int actual = Arity(api, managedAt);

                Assert.AreEqual(expected, actual,
                    $"{pair.Key} takes {expected} arguments in native_window.h and {pair.Value} "
                    + $"takes {actual} in WindowApi. Widening one without the other is not a "
                    + "compile error on either side: the managed delegate simply reads whatever "
                    + "is on the stack past what the C++ pushed.");

                string nativeReturn = ReturnTypeOf(header, pair.Key);

                Assert.IsTrue(_returns.ContainsKey(nativeReturn),
                    $"{pair.Key} returns {nativeReturn} in native_window.h and this test does not "
                    + "say what that maps to on the managed side. Say so rather than widening the "
                    + "regex to swallow it.");

                Assert.AreEqual(_returns[nativeReturn], managed.Groups[1].Value,
                    $"{pair.Key} returns {nativeReturn} in native_window.h while {pair.Value} "
                    + $"returns {managed.Groups[1].Value} in WindowApi. A return type is no more a "
                    + "compile error across the boundary than an argument is.");
            }
        }

        [TestMethod]
        public void NoCallbackIsLeftOutOfThisTest()
        {
            string header = Read(HEADER);

            MatchCollection found = Regex.Matches(header, @"\(\*(_\w+CallBack)\)\(");

            Assert.AreNotEqual(0, found.Count, "the parser found nothing, so it has stopped "
                + "matching the shape of native_window.h rather than proving anything");

            foreach (Match match in found)
            {
                string name = match.Groups[1].Value;

                Assert.IsTrue(_callbacks.ContainsKey(name),
                    $"{name} is a callback in native_window.h that this test says nothing about. "
                    + "Adding one to the wire format must fail here until it is mapped, the same "
                    + "way an unmapped IXEN_* define does.");
            }

            Assert.AreEqual(_callbacks.Count, found.Count,
                "the table names a callback native_window.h does not declare");
        }

        private static string ReturnTypeOf(string header, string name)
        {
            Match match = Regex.Match(header, @"(\w+)\s*\(\*" + name + @"\)\(");

            return match.Success ? match.Groups[1].Value : string.Empty;
        }

        private static int Arity(string source, int from)
        {
            int open = source.IndexOf('(', from);
            int depth = 0;
            int commas = 0;
            bool empty = true;

            for (int index = open; index < source.Length; index++)
            {
                char c = source[index];

                if (c == '(')
                {
                    depth++;
                    continue;
                }

                if (c == ')')
                {
                    depth--;

                    if (depth == 0)
                    {
                        return empty ? 0 : commas + 1;
                    }

                    continue;
                }

                if (!char.IsWhiteSpace(c))
                {
                    empty = false;
                }

                if (c == ',' && depth == 1)
                {
                    commas++;
                }
            }

            Assert.Fail("unbalanced parentheses while reading an argument list; the parser needs "
                + "updating rather than the declaration being reshaped to suit it");

            return -1;
        }

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
                + "This test reads the sources as text on purpose: referencing "
                + "Ixen.Platform.Windows would pull in the .vcxproj, which the dotnet CLI cannot "
                + "build, so dotnet test would stop working.");

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

        private static Dictionary<string, int> ManagedValues()
        {
            var found = new Dictionary<string, int>();

            string kinds = Read(KINDS);
            string keys = Read(KEYS);

            ReadEnum(kinds, "NativePointerKind", found);
            ReadEnum(kinds, "NativePointerButton", found);
            ReadEnum(keys, "NativeKeyKind", found);
            ReadEnum(keys, "NativeImeKind", found);

            foreach (Match match in Regex.Matches(keys, @"const int (MOD_[A-Z]+)\s*=\s*(\d+)"))
            {
                found[match.Groups[1].Value] = int.Parse(match.Groups[2].Value);
            }

            foreach (Match match in Regex.Matches(keys, @"CursorKind\.(\w+): return (\d+);"))
            {
                found["Cursor." + match.Groups[1].Value] = int.Parse(match.Groups[2].Value);
            }

            Match fallback = Regex.Match(keys, @"default: return (\d+);");

            Assert.IsTrue(fallback.Success, "no default cursor arm to read; the parser needs updating");

            found["Cursor.Default"] = int.Parse(fallback.Groups[1].Value);

            return found;
        }

        private static string Managed(string define, string suffix)
        {
            if (define.StartsWith("IXEN_POINTER_", StringComparison.Ordinal))
            {
                return "NativePointerKind." + suffix;
            }

            if (define.StartsWith("IXEN_BUTTON_", StringComparison.Ordinal))
            {
                return "NativePointerButton." + suffix;
            }

            if (define.StartsWith("IXEN_IME_", StringComparison.Ordinal))
            {
                return "NativeImeKind." + suffix;
            }

            if (define.StartsWith("IXEN_KEY_", StringComparison.Ordinal))
            {
                return "NativeKeyKind." + suffix;
            }

            if (define.StartsWith("IXEN_CURSOR_", StringComparison.Ordinal))
            {
                return "Cursor." + suffix;
            }

            return suffix;
        }

        [TestMethod]
        public void EveryNativeDefineHasTheSameValueOnTheManagedSide()
        {
            Dictionary<string, int> native = NativeDefines();
            Dictionary<string, int> managed = ManagedValues();

            foreach (KeyValuePair<string, string> pair in _pairs)
            {
                Assert.IsTrue(native.TryGetValue(pair.Key, out int expected),
                    $"{pair.Key} is gone from native_window.cpp");

                string name = Managed(pair.Key, pair.Value);

                Assert.IsTrue(managed.TryGetValue(name, out int actual),
                    $"{name} is gone from the managed side");

                Assert.AreEqual(expected, actual,
                    $"{pair.Key} is {expected} in native_window.cpp and {name} is {actual} in C#. "
                    + "The wire format between the two is duplicated by hand, so reordering either "
                    + "side silently sends pointer moves as capture-lost, or a hand cursor as a "
                    + "crosshair.");
            }
        }

        [TestMethod]
        public void NoNativeDefineIsLeftOutOfThisTest()
        {
            var missing = new List<string>();

            foreach (KeyValuePair<string, int> entry in NativeDefines())
            {
                if (!_pairs.ContainsKey(entry.Key))
                {
                    missing.Add(entry.Key);
                }
            }

            Assert.AreEqual(0, missing.Count,
                $"native_window.cpp declares {string.Join(", ", missing)}, which nothing here "
                + "checks. Add the pair rather than widening the regex: the point of this test is "
                + "that adding a value to the wire format makes you say what it maps to.");
        }

        [TestMethod]
        public void TheParsersActuallyFoundSomething()
        {
            Assert.AreEqual(_pairs.Count, NativeDefines().Count,
                "the .cpp is parsed by regex, so a change of shape has to fail loudly rather than "
                + "quietly finding nothing and passing");

            Assert.AreEqual(_pairs.Count, ManagedValues().Count,
                "same for the C# side");
        }
    }
}
