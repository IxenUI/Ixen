#include "native_window.h"

#include <windows.h>
#include <windowsx.h>
#include <iostream>
#include <map>
#include <string>

#define IXEN_POINTER_MOVE 0
#define IXEN_POINTER_DOWN 1
#define IXEN_POINTER_UP 2
#define IXEN_POINTER_LEAVE 3
#define IXEN_POINTER_CAPTURELOST 4

#define IXEN_KEY_DOWN 0
#define IXEN_KEY_UP 1
#define IXEN_KEY_CHAR 2

#define IXEN_MOD_SHIFT 1
#define IXEN_MOD_CONTROL 2
#define IXEN_MOD_ALT 4

#define IXEN_CURSOR_DEFAULT 0
#define IXEN_CURSOR_HAND 1
#define IXEN_CURSOR_TEXT 2
#define IXEN_CURSOR_WAIT 3
#define IXEN_CURSOR_CROSSHAIR 4
#define IXEN_CURSOR_RESIZE_H 5
#define IXEN_CURSOR_RESIZE_V 6

#define IXEN_BUTTON_NONE 0
#define IXEN_BUTTON_LEFT 1
#define IXEN_BUTTON_MIDDLE 2
#define IXEN_BUTTON_RIGHT 3

using namespace std;
using namespace IxenWindowsNative;

int NativeWindow::_windowNum = 0;
bool NativeWindow::_dpiAwarenessSet = false;
map<HWND, NativeWindow*> NativeWindow::_windowsByHandle;

void NativeWindow::EnsureDpiAwareness()
{
    if (_dpiAwarenessSet)
    {
        return;
    }

    _dpiAwarenessSet = true;

    SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
}

void NativeWindow::ApplyLogicalSize(int logicalWidth, int logicalHeight)
{
    UINT dpi = GetDpi();

    RECT rect = { 0, 0,
        MulDiv(logicalWidth, dpi, USER_DEFAULT_SCREEN_DPI),
        MulDiv(logicalHeight, dpi, USER_DEFAULT_SCREEN_DPI) };

    AdjustWindowRectExForDpi(&rect, WS_OVERLAPPEDWINDOW, FALSE, WS_EX_WINDOWEDGE, dpi);

    SetWindowPos(_handle, nullptr, 0, 0,
        rect.right - rect.left,
        rect.bottom - rect.top,
        SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE);
}

UINT NativeWindow::GetDpi()
{
    if (!_handle)
    {
        return USER_DEFAULT_SCREEN_DPI;
    }

    UINT dpi = GetDpiForWindow(_handle);

    return dpi == 0 ? USER_DEFAULT_SCREEN_DPI : dpi;
}

LRESULT NativeWindow::HandleDpiChanged(LPARAM lParam)
{
    RECT* suggested = reinterpret_cast<RECT*>(lParam);

    if (suggested != nullptr)
    {
        SetWindowPos(_handle, nullptr,
            suggested->left, suggested->top,
            suggested->right - suggested->left,
            suggested->bottom - suggested->top,
            SWP_NOZORDER | SWP_NOACTIVATE);
    }

    Invalidate();

    return 0;
}

NativeWindow::NativeWindow(LPCWSTR title, int width, int height)
{
    EnsureDpiAwareness();

    wstring className = L"IxenWindow#" + to_wstring(++_windowNum);

    WNDCLASSEX wc = { 0 };
    wc.hInstance = nullptr;
    wc.lpszClassName = className.c_str();
    wc.cbSize = sizeof(WNDCLASSEX);
    wc.hIcon = LoadIcon(nullptr, IDI_APPLICATION);
    wc.hCursor = nullptr;
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
        ApplyLogicalSize(width, height);
    }

    _bitmapInfoHeader = {};
    _bitmapInfoHeader.biSize = sizeof(BITMAPINFOHEADER);
    _bitmapInfoHeader.biCompression = 0;
    _bitmapInfoHeader.biBitCount = 32;
    _bitmapInfoHeader.biPlanes = 1;
}

LRESULT NativeWindow::StartEventLoop()
{
    MSG msg = {};

    while (GetMessage(&msg, nullptr, 0, 0))
    {
        TranslateMessage(&msg);
        DispatchMessage(&msg);
    }

    return 0;
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

LRESULT NativeWindow::Show()
{
    ShowWindow(_handle, SW_SHOWNORMAL);
    return StartEventLoop();
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

void NativeWindow::Invalidate()
{
    if (_handle)
    {
        InvalidateRect(_handle, nullptr, FALSE);
    }
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

    if (_pixelsBuffer == nullptr)
    {
        FillRect(hdc, &_clientRect, GetSysColorBrush(COLOR_WINDOW));
    }

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

LRESULT NativeWindow::HandlePointer(int kind, int button, LPARAM lParam)
{
    if (kind == IXEN_POINTER_DOWN)
    {
        SetCapture(_handle);
    }

    if (kind == IXEN_POINTER_MOVE && !_trackingMouse)
    {
        TRACKMOUSEEVENT tme = {};
        tme.cbSize = sizeof(TRACKMOUSEEVENT);
        tme.dwFlags = TME_LEAVE;
        tme.hwndTrack = _handle;

        if (TrackMouseEvent(&tme))
        {
            _trackingMouse = true;
        }
    }

    if (_pointerCallBack != nullptr)
    {
        _pointerCallBack(kind, GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam), button);
    }

    if (kind == IXEN_POINTER_UP)
    {
        ReleaseCapture();
    }

    return 0;
}

LRESULT NativeWindow::HandleWheel(WPARAM wParam, LPARAM lParam, bool horizontal)
{
    if (_wheelCallBack == nullptr)
    {
        return 0;
    }

    POINT point = { GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam) };
    ScreenToClient(_handle, &point);

    int delta = GET_WHEEL_DELTA_WPARAM(wParam);

    _wheelCallBack(point.x, point.y, horizontal ? delta : 0, horizontal ? 0 : delta);

    return 0;
}

void NativeWindow::SetCursorKind(int kind)
{
    LPCWSTR name = IDC_ARROW;

    switch (kind)
    {
    case IXEN_CURSOR_HAND: name = IDC_HAND; break;
    case IXEN_CURSOR_TEXT: name = IDC_IBEAM; break;
    case IXEN_CURSOR_WAIT: name = IDC_WAIT; break;
    case IXEN_CURSOR_CROSSHAIR: name = IDC_CROSS; break;
    case IXEN_CURSOR_RESIZE_H: name = IDC_SIZEWE; break;
    case IXEN_CURSOR_RESIZE_V: name = IDC_SIZENS; break;
    }

    _cursor = LoadCursor(nullptr, name);

    POINT point = {};

    if (GetCursorPos(&point) && WindowFromPoint(point) == _handle)
    {
        SetCursor(_cursor);
    }
}

LRESULT NativeWindow::HandleSetCursor(LPARAM lParam)
{
    if (LOWORD(lParam) != HTCLIENT)
    {
        return DefWindowProc(_handle, WM_SETCURSOR, (WPARAM)_handle, lParam);
    }

    SetCursor(_cursor != nullptr ? _cursor : LoadCursor(nullptr, IDC_ARROW));

    return TRUE;
}

int NativeWindow::GetModifiers()
{
    int modifiers = 0;

    if (GetKeyState(VK_SHIFT) & 0x8000)
    {
        modifiers |= IXEN_MOD_SHIFT;
    }

    if (GetKeyState(VK_CONTROL) & 0x8000)
    {
        modifiers |= IXEN_MOD_CONTROL;
    }

    if (GetKeyState(VK_MENU) & 0x8000)
    {
        modifiers |= IXEN_MOD_ALT;
    }

    return modifiers;
}

LRESULT NativeWindow::HandleKey(int kind, WPARAM wParam)
{
    if (_keyCallBack != nullptr)
    {
        _keyCallBack(kind, (int)wParam, GetModifiers());
    }

    return 0;
}

LRESULT NativeWindow::HandleCaptureLost()
{
    if (_pointerCallBack != nullptr)
    {
        _pointerCallBack(IXEN_POINTER_CAPTURELOST, 0, 0, IXEN_BUTTON_NONE);
    }

    return 0;
}

LRESULT NativeWindow::HandleMouseLeave()
{
    _trackingMouse = false;

    if (_pointerCallBack != nullptr)
    {
        _pointerCallBack(IXEN_POINTER_LEAVE, 0, 0, IXEN_BUTTON_NONE);
    }

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

    case WM_MOUSEMOVE:
        return HandlePointer(IXEN_POINTER_MOVE, IXEN_BUTTON_NONE, lParam);
    case WM_MOUSELEAVE:
        return HandleMouseLeave();
    case WM_CAPTURECHANGED:
        return HandleCaptureLost();
    case WM_DPICHANGED:
        return HandleDpiChanged(lParam);

    case WM_KEYDOWN:
        return HandleKey(IXEN_KEY_DOWN, wParam);
    case WM_KEYUP:
        return HandleKey(IXEN_KEY_UP, wParam);
    case WM_CHAR:
        return HandleKey(IXEN_KEY_CHAR, wParam);

    case WM_SYSKEYDOWN:
        HandleKey(IXEN_KEY_DOWN, wParam);
        break;
    case WM_SYSKEYUP:
        HandleKey(IXEN_KEY_UP, wParam);
        break;

    case WM_SETCURSOR:
        return HandleSetCursor(lParam);

    case WM_MOUSEWHEEL:
        return HandleWheel(wParam, lParam, false);
    case WM_MOUSEHWHEEL:
        return HandleWheel(wParam, lParam, true);

    case WM_LBUTTONDOWN:
        return HandlePointer(IXEN_POINTER_DOWN, IXEN_BUTTON_LEFT, lParam);
    case WM_LBUTTONUP:
        return HandlePointer(IXEN_POINTER_UP, IXEN_BUTTON_LEFT, lParam);

    case WM_MBUTTONDOWN:
        return HandlePointer(IXEN_POINTER_DOWN, IXEN_BUTTON_MIDDLE, lParam);
    case WM_MBUTTONUP:
        return HandlePointer(IXEN_POINTER_UP, IXEN_BUTTON_MIDDLE, lParam);

    case WM_RBUTTONDOWN:
        return HandlePointer(IXEN_POINTER_DOWN, IXEN_BUTTON_RIGHT, lParam);
    case WM_RBUTTONUP:
        return HandlePointer(IXEN_POINTER_UP, IXEN_BUTTON_RIGHT, lParam);
    }

    return DefWindowProc(_handle, msg, wParam, lParam);
}

LRESULT CALLBACK NativeWindow::WindowProc(HWND handle, UINT msg, WPARAM wParam, LPARAM lParam)
{
    NativeWindow* window = GetFromHandle(handle);

    if (window)
    {
        return window->Proc(msg, wParam, lParam);
    }

    return DefWindowProc(handle, msg, wParam, lParam);
}