using System;
using System.Runtime.InteropServices;

namespace Ixen.Platform.Windows
{
    internal static class SystemPreferences
    {
        private const uint SPI_GETCLIENTAREAANIMATION = 0x1042;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SystemParametersInfo(uint action, uint param, out bool value, uint update);

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
    }
}
