#include "window_api.h"
#include "../window/native_window.h"

#include <windows.h>
#include <iostream>

using namespace IxenWindowsNative;

NativeWindow* WA_CreateWindow(LPCWSTR title, int width, int height)
{
    return new NativeWindow(title, width, height);
}

LRESULT WA_ShowWindow(NativeWindow* window)
{
    if (!window)
    {
        return 1;
    }

    return window->Show();
}

LPWSTR WA_GetWindowTitle(NativeWindow* window)
{
    if (!window)
    {
        return nullptr;
    }

    return PreMarshalString(window->GetTitle());
}

void WA_SetWindowTitle(NativeWindow* window, LPCWSTR value)
{
    if (!window)
    {
        return;
    }

    window->SetTitle(value);
}

void WA_SetWindowPixelsBuffer(NativeWindow* window, void* buffer)
{
    if (!window)
    {
        return;
    }

    window->SetPixelsBuffer(buffer);
}

void WA_RegisterPaintCallBack(NativeWindow* window, void __stdcall callBack(int, int))
{
    if (!window)
    {
        return;
    }

    window->SetOnPaintCallBack(callBack);
}

void WA_RegisterPointerCallBack(NativeWindow* window, void __stdcall callBack(int, int, int, int))
{
    if (!window)
    {
        return;
    }

    window->SetOnPointerCallBack(callBack);
}

void WA_RegisterKeyCallBack(NativeWindow* window, void __stdcall callBack(int, int, int, int))
{
    if (!window)
    {
        return;
    }

    window->SetOnKeyCallBack(callBack);
}

void WA_RegisterImeCallBack(NativeWindow* window, void __stdcall callBack(int, const wchar_t*, int))
{
    if (!window)
    {
        return;
    }

    window->SetOnImeCallBack(callBack);
}

void WA_RegisterWheelCallBack(NativeWindow* window, void __stdcall callBack(int, int, int, int, int))
{
    if (!window)
    {
        return;
    }

    window->SetOnWheelCallBack(callBack);
}

void WA_InvalidateWindow(NativeWindow* window)
{
    if (!window)
    {
        return;
    }

    window->Invalidate();
}

void WA_SetWindowCursor(NativeWindow* window, int kind)
{
    if (!window)
    {
        return;
    }

    window->SetCursorKind(kind);
}

unsigned int WA_GetWindowDpi(NativeWindow* window)
{
    if (!window)
    {
        return USER_DEFAULT_SCREEN_DPI;
    }

    return window->GetDpi();
}

void WA_RegisterAccessibilityCallBack(NativeWindow* window, __int64 __stdcall callBack(__int64, __int64))
{
    if (window)
    {
        window->SetOnAccessibilityCallBack(callBack);
    }
}

void* WA_GetWindowHandle(NativeWindow* window)
{
    return window ? (void*)window->GetHandle() : nullptr;
}

int WA_CreateGlContext(NativeWindow* window)
{
    if (!window)
    {
        return 0;
    }

    return window->CreateGlContext() ? 1 : 0;
}

void WA_SwapGlBuffers(NativeWindow* window)
{
    if (window)
    {
        window->SwapGlBuffers();
    }
}
