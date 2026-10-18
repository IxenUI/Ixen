using Ixen.Core.Visual.Classes;
using Ixen.Platform.Mac.NativeApi;
using System;
using System.Globalization;

namespace Ixen.Platform.Mac
{
    internal static class MacPreferences
    {
        private const int COUNT = 7;

        internal static SystemPalette SystemColors()
        {
            try
            {
                var colors = new int[COUNT];

                MacApi.GetSystemColors(colors, COUNT);

                return From(colors);
            }
            catch (EntryPointNotFoundException)
            {
                return null;
            }
            catch (DllNotFoundException)
            {
                return null;
            }
        }

        internal static SystemPalette From(int[] colors)
        {
            if (colors == null || colors.Length < COUNT)
            {
                return null;
            }

            var palette = new SystemPalette
            {
                Surface = Hex(colors[0]),
                Text = Hex(colors[1]),
                Control = Hex(colors[2]),
                ControlText = Hex(colors[3]),
                Accent = Hex(colors[4]),
                AccentText = Hex(colors[5]),
                Disabled = Hex(colors[6])
            };

            return palette.IsComplete ? palette : null;
        }

        private static string Hex(int rgb)
            => rgb < 0 ? null : "#" + (rgb & 0xFFFFFF).ToString("X6", CultureInfo.InvariantCulture);
    }
}
