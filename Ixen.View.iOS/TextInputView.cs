using CoreGraphics;
using Ixen.Core.Visual;
using Ixen.Platform;
using System;
using UIKit;

namespace Ixen.View.iOS
{
    internal sealed partial class TextInputView : UIView, IUITextInput
    {
        private readonly IxenHost _host;
        private readonly Action _repaint;

        internal TextInputView(IxenHost host, Action repaint)
        {
            _host = host;
            _repaint = repaint;

            AutoresizingMask = UIViewAutoresizing.FlexibleWidth | UIViewAutoresizing.FlexibleHeight;
            BackgroundColor = UIColor.Clear;
            IsAccessibilityElement = false;
            AccessibilityElementsHidden = true;
        }

        private TextField Field => _host.FocusedElement as TextField;

        public override UIView HitTest(CGPoint point, UIEvent uievent) => null;
    }
}
