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
                QuietAssistant(Field == null);
                BecomeFirstResponder();
            }
        }

        private void RequestKeyboardSync()
        {
            if (_syncPending || (Field == null && _editing == null))
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
                TellInputAboutOutsideChanges();
                return;
            }

            _editing = field;
            _known = Snapshot();

            if (!IsFirstResponder)
            {
                BecomeFirstResponder();
            }

            QuietAssistant(field == null);
            ReloadInputViews();
        }

        private UIBarButtonItemGroup[] _leading;
        private UIBarButtonItemGroup[] _trailing;

        private void QuietAssistant(bool quiet)
        {
            UITextInputAssistantItem assistant = InputAssistantItem;

            if (assistant == null)
            {
                return;
            }

            if (_leading == null)
            {
                _leading = assistant.LeadingBarButtonGroups ?? new UIBarButtonItemGroup[0];
                _trailing = assistant.TrailingBarButtonGroups ?? new UIBarButtonItemGroup[0];
            }

            assistant.LeadingBarButtonGroups = quiet ? new UIBarButtonItemGroup[0] : _leading;
            assistant.TrailingBarButtonGroups = quiet ? new UIBarButtonItemGroup[0] : _trailing;
        }

        [Export("hasText")]
        public bool HasText => Document.Length > 0;

        [Export("insertText:")]
        public void InsertText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (Field?.IsComposing == true)
            {
                _host.CommitComposition(text);
                Edited();
                return;
            }

            if (text == "\n")
            {
                Press(Key.Enter);
            }
            else
            {
                _host.TextInput(text);
            }

            Edited();
        }

        [Export("deleteBackward")]
        public void DeleteBackward()
        {
            Press(Key.Backspace);
            Edited();
        }

        private void Press(Key key)
        {
            _host.KeyDown(key, KeyModifiers.None, false);
            _host.KeyUp(key, KeyModifiers.None);
        }

        private bool Masked => Field == null || Field.IsMasked;

        [Export("secureTextEntry")]
        public bool SecureTextEntry => Field?.IsMasked ?? false;

        [Export("autocorrectionType")]
        public UITextAutocorrectionType AutocorrectionType
            => Masked ? UITextAutocorrectionType.No : UITextAutocorrectionType.Default;

        [Export("autocapitalizationType")]
        public UITextAutocapitalizationType AutocapitalizationType
            => Masked ? UITextAutocapitalizationType.None : UITextAutocapitalizationType.Sentences;

        [Export("spellCheckingType")]
        public UITextSpellCheckingType SpellCheckingType
            => Masked ? UITextSpellCheckingType.No : UITextSpellCheckingType.Default;

        [Export("smartQuotesType")]
        public UITextSmartQuotesType SmartQuotesType
            => Masked ? UITextSmartQuotesType.No : UITextSmartQuotesType.Default;

        [Export("smartDashesType")]
        public UITextSmartDashesType SmartDashesType
            => Masked ? UITextSmartDashesType.No : UITextSmartDashesType.Default;

        [Export("smartInsertDeleteType")]
        public UITextSmartInsertDeleteType SmartInsertDeleteType
            => Masked ? UITextSmartInsertDeleteType.No : UITextSmartInsertDeleteType.Default;

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
