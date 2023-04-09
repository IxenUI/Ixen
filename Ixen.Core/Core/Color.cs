using SkiaSharp;
using System;

namespace Ixen.Core
{
    public class Color
    {
        internal SKColor SKColor { get; }

        public Color(byte r, byte g, byte b, byte a = 0xFF)
        {
            SKColor = new SKColor(r, g, b, a);
        }

        public Color(string hexColor)
        {
            if (SKColor.TryParse(hexColor, out SKColor skColor))
            {
                SKColor = skColor;
            }
            else
            {
                SKColor = SKColor.Empty;
            }
        }


        public string ToRGBHexColor()
        {
            return $"#{SKColor.Red.ToString("X2")}{SKColor.Green.ToString("X2")}{SKColor.Blue.ToString("X2")}";
        }

        public string ToRGBAHexColor()
        {
            return $"#{SKColor.Red.ToString("X2")}{SKColor.Green.ToString("X2")}{SKColor.Blue.ToString("X2")}{SKColor.Alpha.ToString("X2")}";
        }

        public static Color Transparent          => new Color(0x00, 0x00, 0x00, 0x00);

        public static Color AliceBlue            => new Color(0xF0, 0xF8, 0xFF);
        public static Color AntiqueWhite         => new Color(0xFA, 0xEB, 0xD7);
        public static Color Aqua                 => new Color(0x0, 0xFF, 0xFF);
        public static Color Aquamarine           => new Color(0x7F, 0xFF, 0xD4);
        public static Color Azure                => new Color(0xF0, 0xFF, 0xFF);
        public static Color Beige                => new Color(0xF5, 0xF5, 0xDC);
        public static Color Bisque               => new Color(0xFF, 0xE4, 0xC4);
        public static Color Black                => new Color(0x0, 0x0, 0x0);
        public static Color BlanchedAlmond       => new Color(0xFF, 0xFF, 0xCD);
        public static Color Blue                 => new Color(0x0, 0x0, 0xFF);
        public static Color BlueViolet           => new Color(0x8A, 0x2B, 0xE2);
        public static Color Brown                => new Color(0xA5, 0x2A, 0x2A);
        public static Color BurlyWood            => new Color(0xDE, 0xB8, 0x87);
        public static Color CadetBlue            => new Color(0x5F, 0x9E, 0xA0);
        public static Color Chartreuse           => new Color(0x7F, 0xFF, 0x0);
        public static Color Chocolate            => new Color(0xD2, 0x69, 0x1E);
        public static Color Coral                => new Color(0xFF, 0x7F, 0x50);
        public static Color CornflowerBlue       => new Color(0x64, 0x95, 0xED);
        public static Color Cornsilk             => new Color(0xFF, 0xF8, 0xDC);
        public static Color Crimson              => new Color(0xDC, 0x14, 0x3C);
        public static Color Cyan                 => new Color(0x0, 0xFF, 0xFF);
        public static Color DarkBlue             => new Color(0x0, 0x0, 0x8B);
        public static Color DarkCyan             => new Color(0x0, 0x8B, 0x8B);
        public static Color DarkGoldenrod        => new Color(0xB8, 0x86, 0xB);
        public static Color DarkGray             => new Color(0xA9, 0xA9, 0xA9);
        public static Color DarkGreen            => new Color(0x0, 0x64, 0x0);
        public static Color DarkKhaki            => new Color(0xBD, 0xB7, 0x6B);
        public static Color DarkMagena           => new Color(0x8B, 0x0, 0x8B);
        public static Color DarkOliveGreen       => new Color(0x55, 0x6B, 0x2F);
        public static Color DarkOrange           => new Color(0xFF, 0x8C, 0x0);
        public static Color DarkOrchid           => new Color(0x99, 0x32, 0xCC);
        public static Color DarkRed              => new Color(0x8B, 0x0, 0x0);
        public static Color DarkSalmon           => new Color(0xE9, 0x96, 0x7A);
        public static Color DarkSeaGreen         => new Color(0x8F, 0xBC, 0x8F);
        public static Color DarkSlateBlue        => new Color(0x48, 0x3D, 0x8B);
        public static Color DarkSlateGray        => new Color(0x28, 0x4F, 0x4F);
        public static Color DarkTurquoise        => new Color(0x0, 0xCE, 0xD1);
        public static Color DarkViolet           => new Color(0x94, 0x0, 0xD3);
        public static Color DeepPink             => new Color(0xFF, 0x14, 0x93);
        public static Color DeepSkyBlue          => new Color(0x0, 0xBF, 0xFF);
        public static Color DimGray              => new Color(0x69, 0x69, 0x69);
        public static Color DodgerBlue           => new Color(0x1E, 0x90, 0xFF);
        public static Color Firebrick            => new Color(0xB2, 0x22, 0x22);
        public static Color FloralWhite          => new Color(0xFF, 0xFA, 0xF0);
        public static Color ForestGreen          => new Color(0x22, 0x8B, 0x22);
        public static Color Fuschia              => new Color(0xFF, 0x0, 0xFF);
        public static Color Gainsboro            => new Color(0xDC, 0xDC, 0xDC);
        public static Color GhostWhite           => new Color(0xF8, 0xF8, 0xFF);
        public static Color Gold                 => new Color(0xFF, 0xD7, 0x0);
        public static Color Goldenrod            => new Color(0xDA, 0xA5, 0x20);
        public static Color Gray                 => new Color(0x80, 0x80, 0x80);
        public static Color Green                => new Color(0x0, 0x80, 0x0);
        public static Color GreenYellow          => new Color(0xAD, 0xFF, 0x2F);
        public static Color Honeydew             => new Color(0xF0, 0xFF, 0xF0);
        public static Color HotPink              => new Color(0xFF, 0x69, 0xB4);
        public static Color IndianRed            => new Color(0xCD, 0x5C, 0x5C);
        public static Color Indigo               => new Color(0x4B, 0x0, 0x82);
        public static Color Ivory                => new Color(0xFF, 0xF0, 0xF0);
        public static Color Khaki                => new Color(0xF0, 0xE6, 0x8C);
        public static Color Lavender             => new Color(0xE6, 0xE6, 0xFA);
        public static Color LavenderBlush        => new Color(0xFF, 0xF0, 0xF5);
        public static Color LawnGreen            => new Color(0x7C, 0xFC, 0x0);
        public static Color LemonChiffon         => new Color(0xFF, 0xFA, 0xCD);
        public static Color LightBlue            => new Color(0xAD, 0xD8, 0xE6);
        public static Color LightCoral           => new Color(0xF0, 0x80, 0x80);
        public static Color LightCyan            => new Color(0xE0, 0xFF, 0xFF);
        public static Color LightGoldenrodYellow => new Color(0xFA, 0xFA, 0xD2);
        public static Color LightGray            => new Color(0xD3, 0xD3, 0xD3);
        public static Color LightGreen           => new Color(0x90, 0xEE, 0x90);
        public static Color LightPink            => new Color(0xFF, 0xB6, 0xC1);
        public static Color LightSalmon          => new Color(0xFF, 0xA0, 0x7A);
        public static Color LightSeaGreen        => new Color(0x20, 0xB2, 0xAA);
        public static Color LightSkyBlue         => new Color(0x87, 0xCE, 0xFA);
        public static Color LightSlateGray       => new Color(0x77, 0x88, 0x99);
        public static Color LightSteelBlue       => new Color(0xB0, 0xC4, 0xDE);
        public static Color LightYellow          => new Color(0xFF, 0xFF, 0xE0);
        public static Color Lime                 => new Color(0x0, 0xFF, 0x0);
        public static Color LimeGreen            => new Color(0x32, 0xCD, 0x32);
        public static Color Linen                => new Color(0xFA, 0xF0, 0xE6);
        public static Color Magenta              => new Color(0xFF, 0x0, 0xFF);
        public static Color Maroon               => new Color(0x80, 0x0, 0x0);
        public static Color MediumAquamarine     => new Color(0x66, 0xCD, 0xAA);
        public static Color MediumBlue           => new Color(0x0, 0x0, 0xCD);
        public static Color MediumOrchid         => new Color(0xBA, 0x55, 0xD3);
        public static Color MediumPurple         => new Color(0x93, 0x70, 0xDB);
        public static Color MediumSeaGreen       => new Color(0x3C, 0xB3, 0x71);
        public static Color MediumSlateBlue      => new Color(0x7B, 0x68, 0xEE);
        public static Color MediumSpringGreen    => new Color(0x0, 0xFA, 0x9A);
        public static Color MediumTurquoise      => new Color(0x48, 0xD1, 0xCC);
        public static Color MediumVioletRed      => new Color(0xC7, 0x15, 0x70);
        public static Color MidnightBlue         => new Color(0x19, 0x19, 0x70);
        public static Color MintCream            => new Color(0xF5, 0xFF, 0xFA);
        public static Color MistyRose            => new Color(0xFF, 0xE4, 0xE1);
        public static Color Moccasin             => new Color(0xFF, 0xE4, 0xB5);
        public static Color NavajoWhite          => new Color(0xFF, 0xDE, 0xAD);
        public static Color Navy                 => new Color(0x0, 0x0, 0x80);
        public static Color OldLace              => new Color(0xFD, 0xF5, 0xE6);
        public static Color Olive                => new Color(0x80, 0x80, 0x0);
        public static Color OliveDrab            => new Color(0x6B, 0x8E, 0x2D);
        public static Color Orange               => new Color(0xFF, 0xA5, 0x0);
        public static Color OrangeRed            => new Color(0xFF, 0x45, 0x0);
        public static Color Orchid               => new Color(0xDA, 0x70, 0xD6);
        public static Color PaleGoldenrod        => new Color(0xEE, 0xE8, 0xAA);
        public static Color PaleGreen            => new Color(0x98, 0xFB, 0x98);
        public static Color PaleTurquoise        => new Color(0xAF, 0xEE, 0xEE);
        public static Color PaleVioletRed        => new Color(0xDB, 0x70, 0x93);
        public static Color PapayaWhip           => new Color(0xFF, 0xEF, 0xD5);
        public static Color PeachPuff            => new Color(0xFF, 0xDA, 0x9B);
        public static Color Peru                 => new Color(0xCD, 0x85, 0x3F);
        public static Color Pink                 => new Color(0xFF, 0xC0, 0xCB);
        public static Color Plum                 => new Color(0xDD, 0xA0, 0xDD);
        public static Color PowderBlue           => new Color(0xB0, 0xE0, 0xE6);
        public static Color Purple               => new Color(0x80, 0x0, 0x80);
        public static Color Red                  => new Color(0xFF, 0x0, 0x0);
        public static Color RosyBrown            => new Color(0xBC, 0x8F, 0x8F);
        public static Color RoyalBlue            => new Color(0x41, 0x69, 0xE1);
        public static Color SaddleBrown          => new Color(0x8B, 0x45, 0x13);
        public static Color Salmon               => new Color(0xFA, 0x80, 0x72);
        public static Color SandyBrown           => new Color(0xF4, 0xA4, 0x60);
        public static Color SeaGreen             => new Color(0x2E, 0x8B, 0x57);
        public static Color Seashell             => new Color(0xFF, 0xF5, 0xEE);
        public static Color Sienna               => new Color(0xA0, 0x52, 0x2D);
        public static Color Silver               => new Color(0xC0, 0xC0, 0xC0);
        public static Color SkyBlue              => new Color(0x87, 0xCE, 0xEB);
        public static Color SlateBlue            => new Color(0x6A, 0x5A, 0xCD);
        public static Color SlateGray            => new Color(0x70, 0x80, 0x90);
        public static Color Snow                 => new Color(0xFF, 0xFA, 0xFA);
        public static Color SpringGreen          => new Color(0x0, 0xFF, 0x7F);
        public static Color SteelBlue            => new Color(0x46, 0x82, 0xB4);
        public static Color Tan                  => new Color(0xD2, 0xB4, 0x8C);
        public static Color Teal                 => new Color(0x0, 0x80, 0x80);
        public static Color Thistle              => new Color(0xD8, 0xBF, 0xD8);
        public static Color Tomato               => new Color(0xFD, 0x63, 0x47);
        public static Color Turquoise            => new Color(0x40, 0xE0, 0xD0);
        public static Color Violet               => new Color(0xEE, 0x82, 0xEE);
        public static Color Wheat                => new Color(0xF5, 0xDE, 0xB3);
        public static Color White                => new Color(0xFF, 0xFF, 0xFF);
        public static Color WhiteSmoke           => new Color(0xF5, 0xF5, 0xF5);
        public static Color Yellow               => new Color(0xFF, 0xFF, 0x0);
        public static Color YellowGreen          => new Color(0x9A, 0xCD, 0x32);
    }
}
