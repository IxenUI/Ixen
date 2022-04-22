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
