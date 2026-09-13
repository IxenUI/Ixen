#import "native_window.h"

#include <map>
#include <string>

#define IXEN_POINTER_MOVE 0
#define IXEN_POINTER_DOWN 1
#define IXEN_POINTER_UP 2
#define IXEN_POINTER_LEAVE 3
#define IXEN_POINTER_CAPTURELOST 4

#define IXEN_BUTTON_NONE 0
#define IXEN_BUTTON_LEFT 1
#define IXEN_BUTTON_MIDDLE 2
#define IXEN_BUTTON_RIGHT 3

#define IXEN_KEY_DOWN 0
#define IXEN_KEY_UP 1
#define IXEN_KEY_CHAR 2

#define IXEN_MOD_SHIFT 1
#define IXEN_MOD_CONTROL 2
#define IXEN_MOD_ALT 4
#define IXEN_MOD_META 8

#define IXEN_CURSOR_DEFAULT 0
#define IXEN_CURSOR_HAND 1
#define IXEN_CURSOR_TEXT 2
#define IXEN_CURSOR_WAIT 3
#define IXEN_CURSOR_CROSSHAIR 4
#define IXEN_CURSOR_RESIZE_H 5
#define IXEN_CURSOR_RESIZE_V 6
#define IXEN_CURSOR_RESIZE_DIAGONAL_UP 7
#define IXEN_CURSOR_RESIZE_DIAGONAL_DOWN 8
#define IXEN_CURSOR_MOVE 9
#define IXEN_CURSOR_NOT_ALLOWED 10
#define IXEN_CURSOR_HELP 11
#define IXEN_CURSOR_PROGRESS 12
#define IXEN_CURSOR_HIDDEN 13

#define NOTCH_PER_POINT 1
#define NOTCH_PER_LINE 10

using namespace IxenMacNative;

static int ModifiersOf(NSEventModifierFlags flags)
{
    int modifiers = 0;

    if (flags & NSEventModifierFlagShift)
    {
        modifiers |= IXEN_MOD_SHIFT;
    }

    if (flags & NSEventModifierFlagControl)
    {
        modifiers |= IXEN_MOD_CONTROL;
    }

    if (flags & NSEventModifierFlagOption)
    {
        modifiers |= IXEN_MOD_ALT;
    }

    if (flags & NSEventModifierFlagCommand)
    {
        modifiers |= IXEN_MOD_META;
    }

    return modifiers;
}

@interface IxenContentView : NSView
{
@public
    NativeWindow* owner;
    NSTrackingArea* tracking;
}
@end

@implementation IxenContentView

- (BOOL)isFlipped
{
    return YES;
}

- (BOOL)acceptsFirstResponder
{
    return YES;
}

- (void)updateTrackingAreas
{
    if (tracking != nil)
    {
        [self removeTrackingArea:tracking];
    }

    NSTrackingAreaOptions options = NSTrackingMouseMoved
        | NSTrackingMouseEnteredAndExited
        | NSTrackingActiveInKeyWindow
        | NSTrackingInVisibleRect;

    tracking = [[NSTrackingArea alloc] initWithRect:[self bounds]
                                            options:options
                                              owner:self
                                           userInfo:nil];

    [self addTrackingArea:tracking];

    [super updateTrackingAreas];
}

- (CGFloat)scale
{
    NSWindow* window = [self window];

    return window == nil ? 1.0 : [window backingScaleFactor];
}

- (void)drawRect:(NSRect)dirty
{
    if (owner == nullptr)
    {
        return;
    }

    CGFloat scale = [self scale];
    NSRect bounds = [self bounds];

    int width = (int)(bounds.size.width * scale);
    int height = (int)(bounds.size.height * scale);

    if (owner->paintCallBack != nullptr && width > 0 && height > 0)
    {
        owner->paintCallBack(width, height);
    }

    if (owner->pixels == nullptr || owner->pixelsWidth <= 0 || owner->pixelsHeight <= 0)
    {
        return;
    }

    CGContextRef context = (CGContextRef)[[NSGraphicsContext currentContext] CGContext];

    if (context == nullptr)
    {
        return;
    }

    size_t bytes = (size_t)owner->pixelsRowBytes * (size_t)owner->pixelsHeight;

    CGDataProviderRef provider = CGDataProviderCreateWithData(nullptr, owner->pixels, bytes, nullptr);

    if (provider == nullptr)
    {
        return;
    }

    CGColorSpaceRef space = CGColorSpaceCreateDeviceRGB();
    CGBitmapInfo info = kCGBitmapByteOrder32Little | kCGImageAlphaPremultipliedFirst;

    CGImageRef image = CGImageCreate((size_t)owner->pixelsWidth,
                                     (size_t)owner->pixelsHeight,
                                     8,
                                     32,
                                     (size_t)owner->pixelsRowBytes,
                                     space,
                                     info,
                                     provider,
                                     nullptr,
                                     false,
                                     kCGRenderingIntentDefault);

    if (image != nullptr)
    {
        CGContextSaveGState(context);
        CGContextTranslateCTM(context, 0, bounds.size.height);
        CGContextScaleCTM(context, 1.0, -1.0);
        CGContextSetInterpolationQuality(context, kCGInterpolationNone);
        CGContextDrawImage(context, CGRectMake(0, 0, bounds.size.width, bounds.size.height), image);
        CGContextRestoreGState(context);

        CGImageRelease(image);
    }

    CGColorSpaceRelease(space);
    CGDataProviderRelease(provider);
}

- (void)sendPointer:(NSEvent*)event kind:(int)kind button:(int)button
{
    if (owner == nullptr || owner->pointerCallBack == nullptr)
    {
        return;
    }

    NSPoint point = [self convertPoint:[event locationInWindow] fromView:nil];
    CGFloat scale = [self scale];

    owner->pointerCallBack(kind, (int)(point.x * scale), (int)(point.y * scale), button);
}

- (void)mouseMoved:(NSEvent*)event
{
    [self sendPointer:event kind:IXEN_POINTER_MOVE button:IXEN_BUTTON_NONE];
}

- (void)mouseDragged:(NSEvent*)event
{
    [self sendPointer:event kind:IXEN_POINTER_MOVE button:IXEN_BUTTON_NONE];
}

- (void)rightMouseDragged:(NSEvent*)event
{
    [self sendPointer:event kind:IXEN_POINTER_MOVE button:IXEN_BUTTON_NONE];
}

- (void)otherMouseDragged:(NSEvent*)event
{
    [self sendPointer:event kind:IXEN_POINTER_MOVE button:IXEN_BUTTON_NONE];
}

- (void)mouseDown:(NSEvent*)event
{
    [self sendPointer:event kind:IXEN_POINTER_DOWN button:IXEN_BUTTON_LEFT];
}

- (void)mouseUp:(NSEvent*)event
{
    [self sendPointer:event kind:IXEN_POINTER_UP button:IXEN_BUTTON_LEFT];
}

- (void)rightMouseDown:(NSEvent*)event
{
    [self sendPointer:event kind:IXEN_POINTER_DOWN button:IXEN_BUTTON_RIGHT];
}

- (void)rightMouseUp:(NSEvent*)event
{
    [self sendPointer:event kind:IXEN_POINTER_UP button:IXEN_BUTTON_RIGHT];
}

- (void)otherMouseDown:(NSEvent*)event
{
    [self sendPointer:event kind:IXEN_POINTER_DOWN button:IXEN_BUTTON_MIDDLE];
}

- (void)otherMouseUp:(NSEvent*)event
{
    [self sendPointer:event kind:IXEN_POINTER_UP button:IXEN_BUTTON_MIDDLE];
}

- (void)mouseExited:(NSEvent*)event
{
    if (owner != nullptr && owner->pointerCallBack != nullptr)
    {
        owner->pointerCallBack(IXEN_POINTER_LEAVE, 0, 0, IXEN_BUTTON_NONE);
    }
}

- (void)scrollWheel:(NSEvent*)event
{
    if (owner == nullptr || owner->wheelCallBack == nullptr)
    {
        return;
    }

    NSPoint point = [self convertPoint:[event locationInWindow] fromView:nil];
    CGFloat scale = [self scale];

    CGFloat deltaX = [event scrollingDeltaX];
    CGFloat deltaY = [event scrollingDeltaY];

    CGFloat factor = [event hasPreciseScrollingDeltas] ? NOTCH_PER_POINT : NOTCH_PER_LINE;

    deltaX = deltaX * factor;
    deltaY = deltaY * factor;

    if ([event isDirectionInvertedFromDevice])
    {
        deltaX = -deltaX;
        deltaY = -deltaY;
    }

    owner->wheelCallBack((int)(point.x * scale),
                         (int)(point.y * scale),
                         (int)deltaX,
                         (int)deltaY,
                         ModifiersOf([event modifierFlags]));
}

- (void)keyDown:(NSEvent*)event
{
    if (owner != nullptr && owner->keyCallBack != nullptr)
    {
        owner->keyCallBack(IXEN_KEY_DOWN,
                           (int)[event keyCode],
                           ModifiersOf([event modifierFlags]),
                           [event isARepeat] ? 1 : 0);
    }

    [self interpretKeyEvents:@[ event ]];
}

- (void)keyUp:(NSEvent*)event
{
    if (owner != nullptr && owner->keyCallBack != nullptr)
    {
        owner->keyCallBack(IXEN_KEY_UP,
                           (int)[event keyCode],
                           ModifiersOf([event modifierFlags]),
                           0);
    }
}

- (void)flagsChanged:(NSEvent*)event
{
    if (owner == nullptr || owner->keyCallBack == nullptr)
    {
        return;
    }

    owner->keyCallBack(IXEN_KEY_DOWN,
                       (int)[event keyCode],
                       ModifiersOf([event modifierFlags]),
                       0);
}

- (void)insertText:(id)text replacementRange:(NSRange)range
{
    if (owner == nullptr || owner->keyCallBack == nullptr)
    {
        return;
    }

    NSString* value = [text isKindOfClass:[NSAttributedString class]] ? [text string] : (NSString*)text;

    if (value == nil)
    {
        return;
    }

    NSUInteger length = [value length];

    for (NSUInteger index = 0; index < length; index++)
    {
        unichar unit = [value characterAtIndex:index];

        owner->keyCallBack(IXEN_KEY_CHAR, (int)unit, 0, 0);
    }
}

- (void)doCommandBySelector:(SEL)selector
{
}

@end

namespace IxenMacNative
{
    static void EnsureApplication()
    {
        [NSApplication sharedApplication];
        [NSApp setActivationPolicy:NSApplicationActivationPolicyRegular];
    }

    NativeWindow* CreateNativeWindow(const char* title, int width, int height)
    {
        EnsureApplication();

        NativeWindow* result = new NativeWindow();

        result->pixels = nullptr;
        result->pixelsWidth = 0;
        result->pixelsHeight = 0;
        result->pixelsRowBytes = 0;
        result->paintCallBack = nullptr;
        result->pointerCallBack = nullptr;
        result->keyCallBack = nullptr;
        result->wheelCallBack = nullptr;

        NSRect frame = NSMakeRect(0, 0, width, height);

        NSWindowStyleMask style = NSWindowStyleMaskTitled
            | NSWindowStyleMaskClosable
            | NSWindowStyleMaskMiniaturizable
            | NSWindowStyleMaskResizable;

        NSWindow* window = [[NSWindow alloc] initWithContentRect:frame
                                                      styleMask:style
                                                        backing:NSBackingStoreBuffered
                                                          defer:NO];

        IxenContentView* view = [[IxenContentView alloc] initWithFrame:frame];

        view->owner = result;

        [window setContentView:view];
        [window makeFirstResponder:view];
        [window setReleasedWhenClosed:NO];
        [window center];

        if (title != nullptr)
        {
            [window setTitle:[NSString stringWithUTF8String:title]];
        }

        result->window = (void*)CFBridgingRetain(window);
        result->view = (void*)CFBridgingRetain(view);

        return result;
    }

    int RunNativeWindow(NativeWindow* window)
    {
        if (window == nullptr)
        {
            return -1;
        }

        NSWindow* handle = (__bridge NSWindow*)window->window;

        [handle makeKeyAndOrderFront:nil];
        [NSApp activateIgnoringOtherApps:YES];
        [NSApp run];

        return 0;
    }

    void ReleaseNativeWindow(NativeWindow* window)
    {
        if (window == nullptr)
        {
            return;
        }

        if (window->view != nullptr)
        {
            IxenContentView* view = (IxenContentView*)CFBridgingRelease(window->view);

            view->owner = nullptr;
            window->view = nullptr;
        }

        if (window->window != nullptr)
        {
            CFBridgingRelease(window->window);
            window->window = nullptr;
        }

        delete window;
    }

    void SetNativeWindowTitle(NativeWindow* window, const char* title)
    {
        if (window == nullptr || title == nullptr)
        {
            return;
        }

        NSWindow* handle = (__bridge NSWindow*)window->window;

        [handle setTitle:[NSString stringWithUTF8String:title]];
    }

    void SetNativeWindowPixels(NativeWindow* window, void* pixels, int width, int height, int rowBytes)
    {
        if (window == nullptr)
        {
            return;
        }

        window->pixels = pixels;
        window->pixelsWidth = width;
        window->pixelsHeight = height;
        window->pixelsRowBytes = rowBytes;
    }

    void InvalidateNativeWindow(NativeWindow* window)
    {
        if (window == nullptr || window->view == nullptr)
        {
            return;
        }

        NSView* view = (__bridge NSView*)window->view;

        dispatch_async(dispatch_get_main_queue(), ^{
            [view setNeedsDisplay:YES];
        });
    }

    unsigned int GetNativeWindowDpi(NativeWindow* window)
    {
        if (window == nullptr || window->window == nullptr)
        {
            return 96;
        }

        NSWindow* handle = (__bridge NSWindow*)window->window;

        return (unsigned int)([handle backingScaleFactor] * 96.0);
    }

    int IsNativeWindowPresentable(NativeWindow* window)
    {
        if (window == nullptr || window->window == nullptr)
        {
            return 0;
        }

        NSWindow* handle = (__bridge NSWindow*)window->window;

        return ([handle isMiniaturized] || ![handle isVisible]) ? 0 : 1;
    }

    void SetNativeWindowCursor(NativeWindow* window, int kind)
    {
        if (kind == IXEN_CURSOR_HIDDEN)
        {
            [NSCursor hide];

            return;
        }

        [NSCursor unhide];

        NSCursor* cursor = nil;

        switch (kind)
        {
        case IXEN_CURSOR_HAND: cursor = [NSCursor pointingHandCursor]; break;
        case IXEN_CURSOR_TEXT: cursor = [NSCursor IBeamCursor]; break;
        case IXEN_CURSOR_CROSSHAIR: cursor = [NSCursor crosshairCursor]; break;
        case IXEN_CURSOR_RESIZE_H: cursor = [NSCursor resizeLeftRightCursor]; break;
        case IXEN_CURSOR_RESIZE_V: cursor = [NSCursor resizeUpDownCursor]; break;
        case IXEN_CURSOR_MOVE: cursor = [NSCursor closedHandCursor]; break;
        case IXEN_CURSOR_NOT_ALLOWED: cursor = [NSCursor operationNotAllowedCursor]; break;
        default: cursor = [NSCursor arrowCursor]; break;
        }

        [cursor set];
    }

    static std::string _pasteboardText;

    const char* GetPasteboardText()
    {
        NSPasteboard* board = [NSPasteboard generalPasteboard];
        NSString* value = [board stringForType:NSPasteboardTypeString];

        if (value == nil)
        {
            _pasteboardText.clear();

            return nullptr;
        }

        _pasteboardText.assign([value UTF8String]);

        return _pasteboardText.c_str();
    }

    void SetPasteboardText(const char* text)
    {
        if (text == nullptr)
        {
            return;
        }

        NSPasteboard* board = [NSPasteboard generalPasteboard];

        [board clearContents];
        [board setString:[NSString stringWithUTF8String:text] forType:NSPasteboardTypeString];
    }

    static long _nextTimer = 1;
    static std::map<long, NSTimer*> _timers;

    long ScheduleCallBack(int delayMilliseconds, int repeat, TimerCallBack callBack)
    {
        if (callBack == nullptr)
        {
            return 0;
        }

        long identifier = _nextTimer++;
        NSTimeInterval interval = delayMilliseconds / 1000.0;

        NSTimer* timer = [NSTimer scheduledTimerWithTimeInterval:interval
                                                         repeats:(repeat != 0)
                                                           block:^(NSTimer* fired) {
            if (repeat == 0)
            {
                _timers.erase(identifier);
            }

            callBack(identifier);
        }];

        _timers[identifier] = timer;

        return identifier;
    }

    void CancelCallBack(long identifier)
    {
        std::map<long, NSTimer*>::iterator found = _timers.find(identifier);

        if (found == _timers.end())
        {
            return;
        }

        [found->second invalidate];

        _timers.erase(found);
    }

    int PrefersReducedMotion()
    {
        return [[NSWorkspace sharedWorkspace] accessibilityDisplayShouldReduceMotion] ? 1 : 0;
    }
}
