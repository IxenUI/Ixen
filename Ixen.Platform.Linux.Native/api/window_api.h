#ifndef _WINDOW_API_H_
#define _WINDOW_API_H_

#include "../window/native_window.h"

#define IXEN_API_ENTRY __attribute__((visibility("default")))

IXEN_API_ENTRY NativeWindow* WA_CreateWindow(const char* title, int width, int height);
IXEN_API_ENTRY const char* WA_GetWindowBackend(NativeWindow* window);
IXEN_API_ENTRY int WA_ShowWindow(NativeWindow* window);
IXEN_API_ENTRY void WA_DestroyWindow(NativeWindow* window);
IXEN_API_ENTRY void WA_SetWindowTitle(NativeWindow* window, const char* title);
IXEN_API_ENTRY void WA_SetWindowPixelsBuffer(NativeWindow* window, void* buffer, int width, int height, int rowBytes);
IXEN_API_ENTRY void WA_InvalidateWindow(NativeWindow* window);
IXEN_API_ENTRY void WA_CloseWindow(NativeWindow* window);
IXEN_API_ENTRY unsigned int WA_GetWindowDpi(NativeWindow* window);
IXEN_API_ENTRY int WA_IsWindowPresentable(NativeWindow* window);
IXEN_API_ENTRY void WA_SetWindowCursor(NativeWindow* window, int kind);
IXEN_API_ENTRY void WA_SetWindowCursorImage(NativeWindow* window, const void* pixels, int width, int height, int hotspotX, int hotspotY);
IXEN_API_ENTRY void WA_RegisterPaintCallBack(NativeWindow* window, void callBack(int, int));
IXEN_API_ENTRY void WA_RegisterPointerCallBack(NativeWindow* window, void callBack(int, int, int, int));
IXEN_API_ENTRY void WA_RegisterKeyCallBack(NativeWindow* window, void callBack(int, int, int, int));
IXEN_API_ENTRY void WA_RegisterTextCallBack(NativeWindow* window, void callBack(const char*));
IXEN_API_ENTRY void WA_RegisterWheelCallBack(NativeWindow* window, void callBack(int, int, int, int, int));
IXEN_API_ENTRY const char* WA_GetClipboardText(NativeWindow* window);
IXEN_API_ENTRY void WA_SetClipboardText(NativeWindow* window, const char* text);
IXEN_API_ENTRY void WA_RegisterAccessibilityCallBack(NativeWindow* window, int callBack(int, int, const char*));
IXEN_API_ENTRY int WA_AccessibilityIsActive(NativeWindow* window);
IXEN_API_ENTRY void WA_AccessibilityUpdateNode(NativeWindow* window, int identifier, int parent, int role, long long states, int actions, int x, int y, int width, int height, const char* name, const char* description, const char* value, const char* shortcut);
IXEN_API_ENTRY void WA_AccessibilityCommit(NativeWindow* window, int root, const int* order, int count);
IXEN_API_ENTRY void WA_AccessibilityNotify(NativeWindow* window, int identifier, int kind, const char* text);

IXEN_API_ENTRY long WA_Schedule(int delayMilliseconds, int repeat, void callBack(long));
IXEN_API_ENTRY void WA_Cancel(long identifier);
IXEN_API_ENTRY int WA_PrefersReducedMotion(void);
IXEN_API_ENTRY int WA_PrefersHighContrast(void);
IXEN_API_ENTRY int WA_TextScale(void);

#endif
