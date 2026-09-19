using Ixen.Core;
using Ixen.Platform.Linux;
using Ixen.Platform.Mac;
using Ixen.Platform.Windows;
using System;
using System.Runtime.InteropServices;

namespace Ixen.Platform.Desktop
{
    public static class IxenDesktopApplication
    {
        public static int CreateWindow(IxenSurface surface)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return IxenWindowsApplication.CreateWindow(surface);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return IxenMacApplication.CreateWindow(surface);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return IxenLinuxApplication.CreateWindow(surface);
            }

            throw Unsupported();
        }

        public static int CaptureWindow(IxenSurface surface, string path, int paints = 2)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return IxenWindowsApplication.CaptureWindow(surface, path, paints);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return IxenLinuxApplication.CaptureWindow(surface, path, paints);
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                throw new PlatformNotSupportedException(
                    "The macOS host cannot hand over the frame it drew yet, so there is nothing to "
                        + "capture. Windows and Linux both implement it.");
            }

            throw Unsupported();
        }

        private static PlatformNotSupportedException Unsupported()
        {
            return new PlatformNotSupportedException(
                $"Ixen has no desktop host for {RuntimeInformation.OSDescription}. Windows, macOS "
                    + "and Linux are the three that exist.");
        }
    }
}
