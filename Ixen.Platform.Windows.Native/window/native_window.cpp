#include "native_window.h"

#include <windows.h>
#include <iostream>
#include <map>
#include <string>

using namespace std;
using namespace IxenWindowsNative;

int NativeWindow::_windowNum = 0;
map<HWND, NativeWindow*> NativeWindow::_windowsByHandle;

NativeWindow::NativeWindow(LPCWSTR title, int width, int height)
{
    string className = "IxenWindow#" + to_string(++_windowNum);

    WNDCLASSEX wc = { 0 };
    wc.hInstance = nullptr;
    wc.lpszClassName = (LPWSTR)(className.c_str());
    wc.cbSize = sizeof(WNDCLASSEX);
    wc.hIcon = LoadIcon(nullptr, IDI_APPLICATION);
    wc.hCursor = LoadCursor(nullptr, IDC_ARROW);
    wc.style = CS_HREDRAW | CS_VREDRAW;
    wc.lpfnWndProc = &WindowProc;

    if (!RegisterClassEx(&wc))
    {
        return;
    }

    _handle = CreateWindowEx(WS_EX_WINDOWEDGE, wc.lpszClassName, title, WS_OVERLAPPEDWINDOW, CW_USEDEFAULT, CW_USEDEFAULT, width, height, nullptr, nullptr, nullptr, this);

    if (_handle)
    {
        _windowsByHandle.insert({ _handle, this });
    }

    _bitmapInfoHeader = {};
    _bitmapInfoHeader.biSize = sizeof(BITMAPINFOHEADER);
    _bitmapInfoHeader.biCompression = 0;
    _bitmapInfoHeader.biBitCount = 32;
    _bitmapInfoHeader.biPlanes = 1;
}

NativeWindow* NativeWindow::GetFromHandle(HWND handle)
{
    std::map<HWND, NativeWindow*>::iterator nwIt = _windowsByHandle.find(handle);

    if (nwIt != _windowsByHandle.end())
    {
        return nwIt->second;
    }

    return nullptr;
}

int NativeWindow::Show()
{
    ShowWindow(_handle, SW_SHOWNORMAL);

    MSG msg = {};
    while (GetMessage(&msg, nullptr, 0, 0))
    {
        TranslateMessage(&msg);
        DispatchMessage(&msg);
    }

    return 0;
}

LPWSTR NativeWindow::GetTitle()
{
    int length = GetWindowTextLengthW(_handle) + 1;
    LPWSTR value = new wchar_t[length];
    GetWindowTextW(_handle, value, length);

    return value;
}

void NativeWindow::SetTitle(LPCWSTR value)
{
    SetWindowTextW(_handle, value);
}

LRESULT NativeWindow::HandleDestroy()
{
    PostQuitMessage(0);
    return 0;
}

LRESULT NativeWindow::HandlePaint()
{
    PAINTSTRUCT ps;
    auto hdc = BeginPaint(_handle, &ps);

    GetClientRect(_handle, &_clientRect);
    FillRect(hdc, &_clientRect, (HBRUSH)(COLOR_WINDOW));

    if (_paintCallBack != nullptr)
    {
        _paintCallBack(_clientRect.right, _clientRect.bottom);
    }

    if (_pixelsBuffer != nullptr)
    {
        _bitmapInfoHeader.biWidth = _clientRect.right;
        _bitmapInfoHeader.biHeight = -_clientRect.bottom;
        
        SetDIBitsToDevice(hdc, 0, 0, _clientRect.right, _clientRect.bottom, 0, 0, 0, _clientRect.bottom, _pixelsBuffer, (BITMAPINFO*)&_bitmapInfoHeader, 0);
    }

    EndPaint(_handle, &ps);
    return 0;
}

LRESULT CALLBACK NativeWindow::Proc(UINT msg, WPARAM wParam, LPARAM lParam)
{
    switch (msg)
    {
    case WM_ERASEBKGND:
        return 1;
    case WM_DESTROY:
        return HandleDestroy();
    case WM_PAINT:
        return HandlePaint();
    }

    return DefWindowProc(_handle, msg, wParam, lParam);
}

LRESULT CALLBACK NativeWindow::WindowProc(HWND handle, UINT msg, WPARAM wParam, LPARAM lParam)
{
    NativeWindow *window = GetFromHandle(handle);

    if (window)
    {
        return window->Proc(msg, wParam, lParam);
    }

    return DefWindowProc(handle, msg, wParam, lParam);
}