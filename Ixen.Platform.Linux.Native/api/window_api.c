#include "window_api.h"

NativeWindow* WA_CreateWindow(const char* title, int width, int height)
{
    return NW_Create(title, width, height);
}

const char* WA_GetWindowBackend(NativeWindow* window)
{
    return NW_Backend(window);
}

int WA_ShowWindow(NativeWindow* window)
{
    return NW_Run(window);
}

void WA_DestroyWindow(NativeWindow* window)
{
    NW_Destroy(window);
}

void WA_SetWindowTitle(NativeWindow* window, const char* title)
{
    NW_SetTitle(window, title);
}

void WA_SetWindowPixelsBuffer(NativeWindow* window, void* buffer, int width, int height, int rowBytes)
{
    NW_SetPixelsBuffer(window, buffer, width, height, rowBytes);
}

void WA_InvalidateWindow(NativeWindow* window)
{
    NW_Invalidate(window);
}

void WA_CloseWindow(NativeWindow* window)
{
    NW_Close(window);
}

unsigned int WA_GetWindowDpi(NativeWindow* window)
{
    return NW_GetDpi(window);
}

int WA_IsWindowPresentable(NativeWindow* window)
{
    return NW_IsPresentable(window);
}

void WA_SetWindowCursor(NativeWindow* window, int kind)
{
    NW_SetCursor(window, kind);
}

void WA_SetWindowCursorImage(NativeWindow* window, const void* pixels, int width, int height, int hotspotX, int hotspotY)
{
    NW_SetCursorImage(window, pixels, width, height, hotspotX, hotspotY);
}

void WA_RegisterPaintCallBack(NativeWindow* window, void callBack(int, int))
{
    NW_RegisterPaintCallBack(window, callBack);
}

void WA_RegisterPointerCallBack(NativeWindow* window, void callBack(int, int, int, int))
{
    NW_RegisterPointerCallBack(window, callBack);
}

void WA_RegisterKeyCallBack(NativeWindow* window, void callBack(int, int, int, int))
{
    NW_RegisterKeyCallBack(window, callBack);
}

void WA_RegisterTextCallBack(NativeWindow* window, void callBack(const char*))
{
    NW_RegisterTextCallBack(window, callBack);
}

void WA_RegisterWheelCallBack(NativeWindow* window, void callBack(int, int, int, int, int))
{
    NW_RegisterWheelCallBack(window, callBack);
}

const char* WA_GetClipboardText(NativeWindow* window)
{
    return NW_GetClipboardText(window);
}

void WA_SetClipboardText(NativeWindow* window, const char* text)
{
    NW_SetClipboardText(window, text);
}

long WA_Schedule(int delayMilliseconds, int repeat, void callBack(long))
{
    return NW_Schedule(delayMilliseconds, repeat, callBack);
}

void WA_Cancel(long identifier)
{
    NW_Cancel(identifier);
}

int WA_PrefersReducedMotion(void)
{
    return NW_PrefersReducedMotion();
}

int WA_PrefersHighContrast(void)
{
    return NW_PrefersHighContrast();
}

int WA_TextScale(void)
{
    return NW_TextScale();
}

void WA_RegisterAccessibilityCallBack(NativeWindow* window, int callBack(int, int, const char*))
{
    NW_RegisterAccessibilityCallBack(window, callBack);
}

int WA_AccessibilityIsActive(NativeWindow* window)
{
    return NW_AccessibilityIsActive(window);
}

void WA_AccessibilityUpdateNode(NativeWindow* window, int identifier, int parent, int role,
    long long states, int actions, int x, int y, int width, int height,
    const char* name, const char* description, const char* value, const char* shortcut)
{
    NW_AccessibilityUpdateNode(window, identifier, parent, role, states, actions,
        x, y, width, height, name, description, value, shortcut);
}

void WA_AccessibilityCommit(NativeWindow* window, int root, const int* order, int count)
{
    NW_AccessibilityCommit(window, root, order, count);
}

void WA_AccessibilityNotify(NativeWindow* window, int identifier, int kind, const char* text)
{
    NW_AccessibilityNotify(window, identifier, kind, text);
}
