using Foundation;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using System;
using UIKit;

namespace Ixen.View.iOS
{
    public partial class IxenView
    {
        private readonly UIView _noKeyboard = new UIView();

        private TextField _editing;
        private bool _syncPending;

        private int _canvasHeight;

        private TextField Field => _host.FocusedElement as TextField;

        private void FollowKeyboard()
        {
            UIKeyboardLayoutGuide keyboard = KeyboardLayoutGuide;

            if (OperatingSystem.IsIOSVersionAtLeast(17))
            {
                keyboard.UsesBottomSafeArea = false;
            }

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _canvasView.LeadingAnchor.ConstraintEqualTo(LeadingAnchor),
                _canvasView.TrailingAnchor.ConstraintEqualTo(TrailingAnchor),
                _canvasView.TopAnchor.ConstraintEqualTo(TopAnchor),
                _canvasView.BottomAnchor.ConstraintEqualTo(keyboard.TopAnchor),
            });
        }

        private void RevealFieldIfShrunk(int height)
        {
            bool shrunk = _canvasHeight > 0 && height < _canvasHeight;

            _canvasHeight = height;

            TextField field = Field;

            if (!shrunk || field == null)
            {
                return;
            }

            if (ScrollNavigator.IntoView(field))
            {
                BeginInvokeOnMainThread(_canvasView.SetNeedsDisplay);
            }
        }

        public override bool CanBecomeFirstResponder => true;

        public override UIView InputView => Field == null ? _noKeyboard : null;

        public override void MovedToWindow()
        {
            base.MovedToWindow();

            if (Window != null)
            {
                BecomeFirstResponder();
            }
        }

        private void RequestKeyboardSync()
        {
            if (_syncPending || Field == _editing)
            {
                return;
            }

            _syncPending = true;

            BeginInvokeOnMainThread(SyncKeyboard);
        }

        private void SyncKeyboard()
        {
            _syncPending = false;

            TextField field = Field;

            if (field == _editing)
            {
                return;
            }

            _editing = field;

            if (!IsFirstResponder)
            {
                BecomeFirstResponder();
            }

            ReloadInputViews();
        }

        [Export("hasText")]
        public bool HasText => !string.IsNullOrEmpty(Field?.Text);

        [Export("insertText:")]
        public void InsertText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (text == "\n")
            {
                Press(Key.Enter);
                return;
            }

            _host.TextInput(text);
        }

        [Export("deleteBackward")]
        public void DeleteBackward()
        {
            Press(Key.Backspace);
        }

        private void Press(Key key)
        {
            _host.KeyDown(key, KeyModifiers.None, false);
            _host.KeyUp(key, KeyModifiers.None);
        }

        [Export("secureTextEntry")]
        public bool SecureTextEntry => Field?.IsMasked ?? false;

        [Export("autocorrectionType")]
        public UITextAutocorrectionType AutocorrectionType => UITextAutocorrectionType.No;

        [Export("autocapitalizationType")]
        public UITextAutocapitalizationType AutocapitalizationType => UITextAutocapitalizationType.None;

        [Export("spellCheckingType")]
        public UITextSpellCheckingType SpellCheckingType => UITextSpellCheckingType.No;

        [Export("smartQuotesType")]
        public UITextSmartQuotesType SmartQuotesType => UITextSmartQuotesType.No;

        [Export("smartDashesType")]
        public UITextSmartDashesType SmartDashesType => UITextSmartDashesType.No;

        [Export("smartInsertDeleteType")]
        public UITextSmartInsertDeleteType SmartInsertDeleteType => UITextSmartInsertDeleteType.No;

        [Export("returnKeyType")]
        public UIReturnKeyType ReturnKeyType => Field?.Multiline == true ? UIReturnKeyType.Default : UIReturnKeyType.Done;

        public override void PressesBegan(NSSet<UIPress> presses, UIPressesEvent evt)
        {
            if (!Route(presses, true))
            {
                base.PressesBegan(presses, evt);
            }
        }

        public override void PressesEnded(NSSet<UIPress> presses, UIPressesEvent evt)
        {
            if (!Route(presses, false))
            {
                base.PressesEnded(presses, evt);
            }
        }

        public override void PressesCancelled(NSSet<UIPress> presses, UIPressesEvent evt)
        {
            if (!Route(presses, false))
            {
                base.PressesCancelled(presses, evt);
            }
        }

        private bool Route(NSSet<UIPress> presses, bool down)
        {
            bool text = false;

            foreach (UIPress press in presses.ToArray())
            {
                UIKey uiKey = press.Key;

                if (uiKey == null)
                {
                    return false;
                }

                Key key = IosKeys.ToKey(uiKey);
                KeyModifiers modifiers = IosKeys.ToModifiers(uiKey.ModifierFlags);

                if (down)
                {
                    _host.KeyDown(key, modifiers);
                }
                else
                {
                    _host.KeyUp(key, modifiers);
                }

                text |= IosKeys.ProducesText(key, modifiers);
            }

            return !text;
        }
    }
}
