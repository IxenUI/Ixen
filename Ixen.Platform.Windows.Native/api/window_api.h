#ifndef _WINDOW_API_H_
#define _WINDOW_API_H_

#include "../window/native_window.h"
#include "api_utils.h"

using namespace IxenWindowsNative;

IXEN_API_ENTRY NativeWindow* WA_CreateWindow(LPCWSTR title, int width, int height);
IXEN_API_ENTRY int  WA_ShowWindow(NativeWindow* window);
IXEN_API_ENTRY LPWSTR WA_GetWindowTitle(NativeWindow* window);
IXEN_API_ENTRY void WA_SetWindowTitle(NativeWindow* window, LPCWSTR value);
IXEN_API_ENTRY void WA_SetWindowPixelsBuffer(NativeWindow* window, void* buffer);
IXEN_API_ENTRY void WA_RegisterPaintCallBack(NativeWindow* window, void __stdcall callBack(int, int));

#endif