using Android.Content;
using Android.Graphics;
using Android.Views;
using Ixen.Core;
using Ixen.Core.Visual.Styles.Descriptors;
using SkiaSharp.Views.Android;
using System;
using System.Collections.Generic;
using System.Runtime.Versioning;

namespace Ixen.View.Android
{
    [SupportedOSPlatform("android24.0")]
    internal sealed class AndroidCursors
    {
        private readonly Dictionary<CursorImage, PointerIcon> _pictures = new Dictionary<CursorImage, PointerIcon>();

        public PointerIcon Get(Context context, CursorKind kind, CursorImage image)
        {
            if (kind == CursorKind.Image && image?.Bitmap != null)
            {
                return Picture(image) ?? PointerIcon.GetSystemIcon(context, PointerIconType.Arrow);
            }

            return PointerIcon.GetSystemIcon(context, TypeOf(kind));
        }

        private PointerIcon Picture(CursorImage image)
        {
            if (_pictures.TryGetValue(image, out PointerIcon icon))
            {
                return icon;
            }

            Bitmap bitmap = image.Bitmap.ToBitmap();

            if (bitmap == null)
            {
                return null;
            }

            float x = Math.Max(0, Math.Min(image.HotspotX, bitmap.Width - 1));
            float y = Math.Max(0, Math.Min(image.HotspotY, bitmap.Height - 1));

            icon = PointerIcon.Create(bitmap, x, y);
            _pictures[image] = icon;

            return icon;
        }

        internal static PointerIconType TypeOf(CursorKind kind)
        {
            switch (kind)
            {
                case CursorKind.Hand:
                    return PointerIconType.Hand;
                case CursorKind.Text:
                    return PointerIconType.Text;
                case CursorKind.Wait:
                case CursorKind.Progress:
                    return PointerIconType.Wait;
                case CursorKind.Crosshair:
                    return PointerIconType.Crosshair;
                case CursorKind.ResizeHorizontal:
                    return PointerIconType.HorizontalDoubleArrow;
                case CursorKind.ResizeVertical:
                    return PointerIconType.VerticalDoubleArrow;
                case CursorKind.ResizeDiagonalUp:
                    return PointerIconType.TopRightDiagonalDoubleArrow;
                case CursorKind.ResizeDiagonalDown:
                    return PointerIconType.TopLeftDiagonalDoubleArrow;
                case CursorKind.Move:
                    return PointerIconType.AllScroll;
                case CursorKind.NotAllowed:
                    return PointerIconType.NoDrop;
                case CursorKind.Help:
                    return PointerIconType.Help;
                case CursorKind.Hidden:
                    return PointerIconType.Null;
                default:
                    return PointerIconType.Arrow;
            }
        }
    }
}
