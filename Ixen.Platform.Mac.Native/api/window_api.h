#ifndef _WINDOW_API_H_
#define _WINDOW_API_H_

#include "../window/native_window.h"

using namespace IxenMacNative;

#define IXEN_API_ENTRY extern "C" __attribute__((visibility("default")))

IXEN_API_ENTRY NativeWindow* WA_CreateWindow(const char* title, int width, int height);
IXEN_API_ENTRY int WA_ShowWindow(NativeWindow* window);
IXEN_API_ENTRY void WA_DestroyWindow(NativeWindow* window);
IXEN_API_ENTRY void WA_SetWindowTitle(NativeWindow* window, const char* title);
IXEN_API_ENTRY void WA_SetWindowPixelsBuffer(NativeWindow* window, void* buffer, int width, int height, int rowBytes);
IXEN_API_ENTRY void WA_InvalidateWindow(NativeWindow* window);
IXEN_API_ENTRY unsigned int WA_GetWindowDpi(NativeWindow* window);
IXEN_API_ENTRY int WA_IsWindowPresentable(NativeWindow* window);
IXEN_API_ENTRY void WA_SetWindowCursor(NativeWindow* window, int kind);
IXEN_API_ENTRY void WA_SetWindowCursorImage(NativeWindow* window, const void* bytes, int length, int hotspotX, int hotspotY);
IXEN_API_ENTRY void WA_SetWindowAcceptsFiles(NativeWindow* window, int accepts);
IXEN_API_ENTRY void WA_RegisterPaintCallBack(NativeWindow* window, void callBack(int, int));
IXEN_API_ENTRY void WA_RegisterPointerCallBack(NativeWindow* window, void callBack(int, int, int, int));
IXEN_API_ENTRY void WA_RegisterKeyCallBack(NativeWindow* window, void callBack(int, int, int, int));
IXEN_API_ENTRY void WA_RegisterImeCallBack(NativeWindow* window, void callBack(int, const char*, int));
IXEN_API_ENTRY void WA_RegisterWheelCallBack(NativeWindow* window, void callBack(int, int, int, int, int));
IXEN_API_ENTRY void WA_RegisterDropCallBack(NativeWindow* window, void callBack(int, int, const char*));
IXEN_API_ENTRY const char* WA_GetClipboardText();
IXEN_API_ENTRY void WA_SetClipboardText(const char* text);
IXEN_API_ENTRY long WA_Schedule(int delayMilliseconds, int repeat, void callBack(long));
IXEN_API_ENTRY void WA_Cancel(long identifier);
IXEN_API_ENTRY int WA_PrefersReducedMotion();

#endif
