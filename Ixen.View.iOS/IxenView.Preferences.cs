using Foundation;
using Ixen.Core.Visual.Classes;
using System;
using System.Collections.Generic;
using System.Globalization;
using UIKit;

namespace Ixen.View.iOS
{
    public partial class IxenView
    {
        private readonly List<NSObject> _preferenceObservers = new List<NSObject>();

        private void AttachPreferences()
        {
            NSNotificationCenter center = NSNotificationCenter.DefaultCenter;

            _preferenceObservers.Add(center.AddObserver(UIView.ReduceMotionStatusDidChangeNotification,
                _ => OnPreferencesChanged()));
            _preferenceObservers.Add(center.AddObserver(UIView.DarkerSystemColorsStatusDidChangeNotification,
                _ => OnPreferencesChanged()));
            _preferenceObservers.Add(center.AddObserver(UIApplication.ContentSizeCategoryChangedNotification,
                _ => OnPreferencesChanged()));

            if (OperatingSystem.IsIOSVersionAtLeast(17) || OperatingSystem.IsMacCatalystVersionAtLeast(17))
            {
                RegisterForTraitChanges(new[] { typeof(UITraitUserInterfaceStyle), typeof(UITraitAccessibilityContrast) },
                    (IUITraitEnvironment environment, UITraitCollection previous) => OnPreferencesChanged());
            }

            ReadPreferences();
        }

        private void OnPreferencesChanged()
        {
            ReadPreferences();
            _canvasView.SetNeedsDisplay();
        }

        private void ReadPreferences()
        {
            UITraitCollection traits = TraitCollection;

            _host.Surface.ReducedMotion = UIAccessibility.IsReduceMotionEnabled;
            _host.Surface.HighContrast = UIAccessibility.DarkerSystemColorsEnabled
                || traits.AccessibilityContrast == UIAccessibilityContrast.High;
            _host.Surface.FontScale = ReadFontScale();
            _host.Surface.SystemColors = SystemPalette(traits);
        }

        private SystemPalette SystemPalette(UITraitCollection traits)
        {
            UIColor ground = UIColor.SystemBackground.GetResolvedColor(traits);
            UIColor accent = (TintColor ?? UIColor.SystemBlue).GetResolvedColor(traits);

            return new SystemPalette
            {
                Surface = Hex(ground, ground),
                Text = Hex(UIColor.Label.GetResolvedColor(traits), ground),
                Control = Hex(UIColor.SecondarySystemBackground.GetResolvedColor(traits), ground),
                ControlText = Hex(UIColor.Label.GetResolvedColor(traits), ground),
                Accent = Hex(accent, ground),
                AccentText = Hex(UIColor.White, accent),
                Disabled = Hex(UIColor.TertiaryLabel.GetResolvedColor(traits), ground)
            };
        }

        private static string Hex(UIColor color, UIColor under)
        {
            color.GetRGBA(out nfloat red, out nfloat green, out nfloat blue, out nfloat alpha);
            under.GetRGBA(out nfloat groundRed, out nfloat groundGreen, out nfloat groundBlue, out _);

            return "#" + Channel(red, groundRed, alpha) + Channel(green, groundGreen, alpha)
                + Channel(blue, groundBlue, alpha);
        }

        private static string Channel(nfloat value, nfloat ground, nfloat alpha)
        {
            double mixed = value * alpha + ground * (1 - alpha);
            int byteValue = (int)Math.Round(Math.Min(1, Math.Max(0, mixed)) * 255);

            return byteValue.ToString("X2", CultureInfo.InvariantCulture);
        }
    }
}
