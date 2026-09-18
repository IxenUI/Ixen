#ifndef _NATIVE_WINDOW_H_
#define _NATIVE_WINDOW_H_

#ifdef __OBJC__
#import <Cocoa/Cocoa.h>
#endif

namespace IxenMacNative
{
    typedef void (*PaintCallBack)(int width, int height);
    typedef void (*PointerCallBack)(int kind, int x, int y, int button);
    typedef void (*KeyCallBack)(int kind, int keyCode, int modifiers, int repeat);
    typedef void (*ImeCallBack)(int kind, const char* text, int caret);
    typedef void (*WheelCallBack)(int x, int y, int deltaX, int deltaY, int modifiers);
    typedef void (*DropCallBack)(int x, int y, const char* paths);
    typedef void (*TimerCallBack)(long id);
    typedef int (*AccessibilityCallBack)(int identifier, int action, const char* value);

    struct NativeWindow
    {
        void* window;
        void* view;

        void* pixels;
        int pixelsWidth;
        int pixelsHeight;
        int pixelsRowBytes;

        PaintCallBack paintCallBack;
        PointerCallBack pointerCallBack;
        KeyCallBack keyCallBack;
        ImeCallBack imeCallBack;
        WheelCallBack wheelCallBack;
        DropCallBack dropCallBack;
        AccessibilityCallBack accessibilityCallBack;

        void* accessibilityNodes;
        int accessibilityRoot;
        int accessibilityAsked;
    };

    NativeWindow* CreateNativeWindow(const char* title, int width, int height);
    int RunNativeWindow(NativeWindow* window);
    void ReleaseNativeWindow(NativeWindow* window);
    void SetNativeWindowTitle(NativeWindow* window, const char* title);
    void SetNativeWindowPixels(NativeWindow* window, void* pixels, int width, int height, int rowBytes);
    void InvalidateNativeWindow(NativeWindow* window);
    unsigned int GetNativeWindowDpi(NativeWindow* window);
    int IsNativeWindowPresentable(NativeWindow* window);
    void SetNativeWindowCursor(NativeWindow* window, int kind);
    void SetNativeWindowCursorImage(NativeWindow* window, const void* bytes, int length, int hotspotX, int hotspotY);
    void SetNativeWindowAcceptsFiles(NativeWindow* window, int accepts);

    const char* GetPasteboardText();
    void SetPasteboardText(const char* text);

    long ScheduleCallBack(int delayMilliseconds, int repeat, TimerCallBack callBack);
    void CancelCallBack(long id);

    int PrefersReducedMotion();

    int IsAccessibilityActive(NativeWindow* window);
    void UpdateAccessibilityNode(NativeWindow* window, int identifier, int parent, const char* role, int states, int actions, int toggle, int x, int y, int width, int height, const char* name, const char* value, const char* help);
    void CommitAccessibility(NativeWindow* window, int root, const int* order, int count);
    void NotifyAccessibility(NativeWindow* window, int identifier, int kind, const char* text);
}

#endif
