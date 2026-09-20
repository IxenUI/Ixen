using Ixen.Core.Visual.Classes;
using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace Ixen.Platform.Windows
{
    internal static class SystemPreferences
    {
        private const uint SPI_GETCLIENTAREAANIMATION = 0x1042;
        private const uint SPI_GETHIGHCONTRAST = 0x0042;

        private const uint HCF_HIGHCONTRASTON = 0x00000001;

        private const string ACCESSIBILITY_KEY = @"Software\Microsoft\Accessibility";
        private const string TEXT_SCALE_VALUE = "TextScaleFactor";

        private static readonly IntPtr HKEY_CURRENT_USER = new IntPtr(unchecked((int)0x80000001));

        private const uint RRF_RT_REG_DWORD = 0x00000010;

        private const int PERCENT = 100;
        private const float MIN_TEXT_SCALE = 0.5f;
        private const float MAX_TEXT_SCALE = 4f;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(uint action, uint param, out bool value, uint update);

        [DllImport("user32.dll", EntryPoint = "SystemParametersInfoW", SetLastError = true)]
        private static extern bool SystemParametersInfoContrast(uint action, uint param, ref HIGHCONTRAST value, uint update);

        private const int COLOR_WINDOW = 5;
        private const int COLOR_WINDOWTEXT = 8;
        private const int COLOR_HIGHLIGHT = 13;
        private const int COLOR_HIGHLIGHTTEXT = 14;
        private const int COLOR_BTNFACE = 15;
        private const int COLOR_GRAYTEXT = 17;
        private const int COLOR_BTNTEXT = 18;

        [DllImport("user32.dll")]
        private static extern uint GetSysColor(int index);

        [DllImport("advapi32.dll", EntryPoint = "RegGetValueW", CharSet = CharSet.Unicode)]
        private static extern int RegGetValue(IntPtr key, string subKey, string value, uint flags,
            IntPtr type, out int data, ref uint size);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct HIGHCONTRAST
        {
            internal uint Size;
            internal uint Flags;
            internal IntPtr DefaultScheme;
        }

        internal static bool PrefersReducedMotion()
        {
            try
            {
                if (!SystemParametersInfo(SPI_GETCLIENTAREAANIMATION, 0, out bool animations, 0))
                {
                    return false;
                }

                return !animations;
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
            catch (DllNotFoundException)
            {
                return false;
            }
        }

        internal static float TextScale()
        {
            try
            {
                uint size = sizeof(int);

                if (RegGetValue(HKEY_CURRENT_USER, ACCESSIBILITY_KEY, TEXT_SCALE_VALUE,
                    RRF_RT_REG_DWORD, IntPtr.Zero, out int percent, ref size) != 0)
                {
                    return 1;
                }

                float scale = percent / (float)PERCENT;

                if (scale < MIN_TEXT_SCALE || scale > MAX_TEXT_SCALE)
                {
                    return 1;
                }

                return scale;
            }
            catch (EntryPointNotFoundException)
            {
                return 1;
            }
            catch (DllNotFoundException)
            {
                return 1;
            }
        }

        internal static SystemPalette SystemColors()
        {
            try
            {
                return new SystemPalette
                {
                    Surface = Hex(COLOR_WINDOW),
                    Text = Hex(COLOR_WINDOWTEXT),
                    Control = Hex(COLOR_BTNFACE),
                    ControlText = Hex(COLOR_BTNTEXT),
                    Accent = Hex(COLOR_HIGHLIGHT),
                    AccentText = Hex(COLOR_HIGHLIGHTTEXT),
                    Disabled = Hex(COLOR_GRAYTEXT)
                };
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

        private static string Hex(int index)
        {
            uint color = GetSysColor(index);

            return "#" + (color & 0xFF).ToString("X2", CultureInfo.InvariantCulture)
                + ((color >> 8) & 0xFF).ToString("X2", CultureInfo.InvariantCulture)
                + ((color >> 16) & 0xFF).ToString("X2", CultureInfo.InvariantCulture);
        }

        internal static bool PrefersHighContrast()
        {
            try
            {
                HIGHCONTRAST contrast = new HIGHCONTRAST
                {
                    Size = (uint)Marshal.SizeOf(typeof(HIGHCONTRAST))
                };

                if (!SystemParametersInfoContrast(SPI_GETHIGHCONTRAST, contrast.Size, ref contrast, 0))
                {
                    return false;
                }

                return (contrast.Flags & HCF_HIGHCONTRASTON) != 0;
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
            catch (DllNotFoundException)
            {
                return false;
            }
        }
    }
}
