using Ixen.Core;
using Ixen.Visual;
using System;
using WinApi.Desktop;
using WinApi.Windows;
using WinApi.Windows.Helpers;

namespace Ixen.Windows.Core
{
    public class IxenWindowsApplication : IxenApplication
    {
        public int Init(IxenSurface surface)
        {
            try
            {
                ApplicationHelpers.SetupDefaultExceptionHandlers();
                var factory = WindowFactory.Create(hBgBrush: IntPtr.Zero);
                using
                (
                    var window = factory.CreateWindow
                    (
                        () => new IxenWindow(surface),
                        surface.Title,
                        width: surface.InitOptions.Width,
                        height: surface.InitOptions.Height,
                        constructionParams: new FrameWindowConstructionParams())
                    )
                {
                    window.Show();
                    return new EventLoop().Run(window);
                }
            }
            catch (Exception ex)
            {
                MessageBoxHelpers.ShowError(ex);
                return 1;
            }
        }
    }
}
