#import "window_api.h"

NativeWindow* WA_CreateWindow(const char* title, int width, int height)
{
    return CreateNativeWindow(title, width, height);
}

int WA_ShowWindow(NativeWindow* window)
{
    return RunNativeWindow(window);
}

void WA_DestroyWindow(NativeWindow* window)
{
    ReleaseNativeWindow(window);
}

void WA_SetWindowTitle(NativeWindow* window, const char* title)
{
    SetNativeWindowTitle(window, title);
}

void WA_SetWindowPixelsBuffer(NativeWindow* window, void* buffer, int width, int height, int rowBytes)
{
    SetNativeWindowPixels(window, buffer, width, height, rowBytes);
}

void WA_InvalidateWindow(NativeWindow* window)
{
    InvalidateNativeWindow(window);
}

unsigned int WA_GetWindowDpi(NativeWindow* window)
{
    return GetNativeWindowDpi(window);
}

int WA_IsWindowPresentable(NativeWindow* window)
{
    return IsNativeWindowPresentable(window);
}

void WA_SetWindowCursor(NativeWindow* window, int kind)
{
    SetNativeWindowCursor(window, kind);
}

void WA_SetWindowCursorImage(NativeWindow* window, const void* bytes, int length,
    int hotspotX, int hotspotY)
{
    SetNativeWindowCursorImage(window, bytes, length, hotspotX, hotspotY);
}

void WA_SetWindowAcceptsFiles(NativeWindow* window, int accepts)
{
    SetNativeWindowAcceptsFiles(window, accepts);
}

void WA_RegisterPaintCallBack(NativeWindow* window, void callBack(int, int))
{
    if (window != nullptr)
    {
        window->paintCallBack = callBack;
    }
}

void WA_RegisterPointerCallBack(NativeWindow* window, void callBack(int, int, int, int))
{
    if (window != nullptr)
    {
        window->pointerCallBack = callBack;
    }
}

void WA_RegisterKeyCallBack(NativeWindow* window, void callBack(int, int, int, int))
{
    if (window != nullptr)
    {
        window->keyCallBack = callBack;
    }
}

void WA_RegisterImeCallBack(NativeWindow* window, void callBack(int, const char*, int))
{
    if (window != nullptr)
    {
        window->imeCallBack = callBack;
    }
}

void WA_RegisterWheelCallBack(NativeWindow* window, void callBack(int, int, int, int, int))
{
    if (window != nullptr)
    {
        window->wheelCallBack = callBack;
    }
}

void WA_RegisterDropCallBack(NativeWindow* window, void callBack(int, int, const char*))
{
    if (window != nullptr)
    {
        window->dropCallBack = callBack;
    }
}

const char* WA_GetClipboardText()
{
    return GetPasteboardText();
}

void WA_SetClipboardText(const char* text)
{
    SetPasteboardText(text);
}

long WA_Schedule(int delayMilliseconds, int repeat, void callBack(long))
{
    return ScheduleCallBack(delayMilliseconds, repeat, callBack);
}

void WA_Cancel(long identifier)
{
    CancelCallBack(identifier);
}

int WA_PrefersReducedMotion()
{
    return PrefersReducedMotion();
}
