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
