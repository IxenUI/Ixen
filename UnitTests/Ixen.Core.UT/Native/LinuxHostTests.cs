using Ixen.Core.Input;
using Ixen.Platform.Linux.NativeApi;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SkiaSharp;

namespace Ixen.Core.UT.Native
{
    [TestClass]
    public class LinuxHostTests
    {
        private static void AssertNamed(string name, int symbol)
        {
            Assert.AreEqual(name, LinuxKeys.ToKey(symbol).ToString(),
                $"keysym 0x{symbol:X4} has to be the key named {name}, whatever order the Key "
                    + "enum happens to declare its members in");
        }

        [TestMethod]
        public void EveryLowercaseKeysymIsItsLetter()
        {
            for (int index = 0; index < 26; index++)
            {
                AssertNamed(((char)('A' + index)).ToString(), 0x0061 + index);
            }
        }

        [TestMethod]
        public void EveryUppercaseKeysymIsTheSameLetter()
        {
            for (int index = 0; index < 26; index++)
            {
                AssertNamed(((char)('A' + index)).ToString(), 0x0041 + index);
            }
        }

        [TestMethod]
        public void EveryDigitIsItsDigitWhereverItCameFrom()
        {
            for (int index = 0; index < 10; index++)
            {
                AssertNamed("Digit" + index, 0x0030 + index);
                AssertNamed("Digit" + index, 0xFFB0 + index);
            }
        }

        [TestMethod]
        public void TheFunctionKeysRunFromF1()
        {
            for (int index = 0; index < 12; index++)
            {
                AssertNamed("F" + (index + 1), 0xFFBE + index);
            }
        }

        [TestMethod]
        public void TheNamedKeysAreWhatX11CallsThem()
        {
            Assert.AreEqual(Key.Space, LinuxKeys.ToKey(0x0020));
            Assert.AreEqual(Key.Backspace, LinuxKeys.ToKey(0xFF08));
            Assert.AreEqual(Key.Tab, LinuxKeys.ToKey(0xFF09));
            Assert.AreEqual(Key.Enter, LinuxKeys.ToKey(0xFF0D));
            Assert.AreEqual(Key.Escape, LinuxKeys.ToKey(0xFF1B));
            Assert.AreEqual(Key.Delete, LinuxKeys.ToKey(0xFFFF));
            Assert.AreEqual(Key.Home, LinuxKeys.ToKey(0xFF50));
            Assert.AreEqual(Key.Left, LinuxKeys.ToKey(0xFF51));
            Assert.AreEqual(Key.Up, LinuxKeys.ToKey(0xFF52));
            Assert.AreEqual(Key.Right, LinuxKeys.ToKey(0xFF53));
            Assert.AreEqual(Key.Down, LinuxKeys.ToKey(0xFF54));
            Assert.AreEqual(Key.PageUp, LinuxKeys.ToKey(0xFF55));
            Assert.AreEqual(Key.PageDown, LinuxKeys.ToKey(0xFF56));
            Assert.AreEqual(Key.End, LinuxKeys.ToKey(0xFF57));
        }

        [TestMethod]
        public void ShiftTabIsStillTab()
        {
            Assert.AreEqual(Key.Tab, LinuxKeys.ToKey(0xFE20));
        }

        [TestMethod]
        public void BothSidesOfAModifierAreTheSameKey()
        {
            Assert.AreEqual(Key.Shift, LinuxKeys.ToKey(0xFFE1));
            Assert.AreEqual(Key.Shift, LinuxKeys.ToKey(0xFFE2));
            Assert.AreEqual(Key.Control, LinuxKeys.ToKey(0xFFE3));
            Assert.AreEqual(Key.Control, LinuxKeys.ToKey(0xFFE4));
            Assert.AreEqual(Key.Alt, LinuxKeys.ToKey(0xFFE9));
            Assert.AreEqual(Key.Alt, LinuxKeys.ToKey(0xFFEA));
        }

        [TestMethod]
        public void SuperIsMetaBecauseThatIsTheKeyPeopleHave()
        {
            Assert.AreEqual(Key.Meta, LinuxKeys.ToKey(0xFFEB));
            Assert.AreEqual(Key.Meta, LinuxKeys.ToKey(0xFFEC));
        }

        [TestMethod]
        public void AKeysymWithNoKeyIsNone()
        {
            Assert.AreEqual(Key.None, LinuxKeys.ToKey(0));
            Assert.AreEqual(Key.None, LinuxKeys.ToKey(0x002C));
            Assert.AreEqual(Key.None, LinuxKeys.ToKey(0xFF67));
        }

        [TestMethod]
        public void TheModifierBitsAreRead()
        {
            Assert.AreEqual(KeyModifiers.None, LinuxKeys.ToModifiers(0));
            Assert.AreEqual(KeyModifiers.Shift, LinuxKeys.ToModifiers(1));
            Assert.AreEqual(KeyModifiers.Control, LinuxKeys.ToModifiers(2));
            Assert.AreEqual(KeyModifiers.Alt, LinuxKeys.ToModifiers(4));
            Assert.AreEqual(KeyModifiers.Meta, LinuxKeys.ToModifiers(8));
            Assert.AreEqual(KeyModifiers.Control | KeyModifiers.Shift, LinuxKeys.ToModifiers(3));
        }

        private static byte[] Pixels(SKColor colour)
        {
            using (var bitmap = new SKBitmap(1, 1))
            {
                bitmap.SetPixel(0, 0, colour);

                return LinuxCursorImages.Premultiplied(bitmap);
            }
        }

        [TestMethod]
        public void ACursorPixelIsPremultipliedBecauseXcursorWantsItThatWay()
        {
            byte[] pixels = Pixels(new SKColor(255, 0, 0, 128));

            Assert.AreEqual(4, pixels.Length);
            Assert.AreEqual(0, pixels[0]);
            Assert.AreEqual(0, pixels[1]);
            Assert.AreEqual(128, pixels[2]);
            Assert.AreEqual(128, pixels[3]);
        }

        [TestMethod]
        public void AnOpaquePixelIsLeftAlone()
        {
            byte[] pixels = Pixels(new SKColor(10, 20, 30, 255));

            Assert.AreEqual(30, pixels[0]);
            Assert.AreEqual(20, pixels[1]);
            Assert.AreEqual(10, pixels[2]);
            Assert.AreEqual(255, pixels[3]);
        }

        [TestMethod]
        public void AClearPixelIsFourZeroes()
        {
            byte[] pixels = Pixels(new SKColor(255, 255, 255, 0));

            Assert.AreEqual(0, pixels[0]);
            Assert.AreEqual(0, pixels[1]);
            Assert.AreEqual(0, pixels[2]);
            Assert.AreEqual(0, pixels[3]);
        }

        [TestMethod]
        public void TheOrderIsBlueGreenRedAlpha()
        {
            byte[] pixels = Pixels(new SKColor(1, 2, 3, 255));

            Assert.AreEqual(3, pixels[0]);
            Assert.AreEqual(2, pixels[1]);
            Assert.AreEqual(1, pixels[2]);
        }
    }
}
