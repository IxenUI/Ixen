#include "native_window.h"

#include <windows.h>
#include <windowsx.h>
#include <iostream>
#include <map>
#include <string>
#include <GL/gl.h>
#include <imm.h>

#define IXEN_POINTER_MOVE 0
#define IXEN_POINTER_DOWN 1
#define IXEN_POINTER_UP 2
#define IXEN_POINTER_LEAVE 3
#define IXEN_POINTER_CAPTURELOST 4

#define IXEN_IME_UPDATE 0
#define IXEN_IME_COMMIT 1
#define IXEN_IME_CANCEL 2

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
#define IXEN_CURSOR_RESIZE_DIAGONAL_UP 7
#define IXEN_CURSOR_RESIZE_DIAGONAL_DOWN 8
#define IXEN_CURSOR_MOVE 9
#define IXEN_CURSOR_NOT_ALLOWED 10
#define IXEN_CURSOR_HELP 11
#define IXEN_CURSOR_PROGRESS 12
#define IXEN_CURSOR_HIDDEN 13

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
    wc.style = CS_HREDRAW | CS_VREDRAW | CS_OWNDC;
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

typedef BOOL(WINAPI* PFN_wglSwapInterval)(int);

bool NativeWindow::CreateGlContext()
{
    if (_glContext != nullptr)
    {
        return true;
    }

    if (_handle == nullptr)
    {
        return false;
    }

    _deviceContext = GetDC(_handle);

    if (_deviceContext == nullptr)
    {
        return false;
    }

    PIXELFORMATDESCRIPTOR descriptor = {};
    descriptor.nSize = sizeof(PIXELFORMATDESCRIPTOR);
    descriptor.nVersion = 1;
    descriptor.dwFlags = PFD_DRAW_TO_WINDOW | PFD_SUPPORT_OPENGL | PFD_DOUBLEBUFFER;
    descriptor.iPixelType = PFD_TYPE_RGBA;
    descriptor.cColorBits = 32;
    descriptor.cAlphaBits = 8;
    descriptor.cStencilBits = 8;
    descriptor.iLayerType = PFD_MAIN_PLANE;

    int format = ChoosePixelFormat(_deviceContext, &descriptor);

    if (format == 0 || !SetPixelFormat(_deviceContext, format, &descriptor))
    {
        ReleaseDC(_handle, _deviceContext);
        _deviceContext = nullptr;

        return false;
    }

    _glContext = wglCreateContext(_deviceContext);

    if (_glContext == nullptr || !wglMakeCurrent(_deviceContext, _glContext))
    {
        DestroyGlContext();

        return false;
    }

    PFN_wglSwapInterval swapInterval = (PFN_wglSwapInterval)wglGetProcAddress("wglSwapIntervalEXT");

    if (swapInterval != nullptr)
    {
        swapInterval(0);
    }

    return true;
}

void NativeWindow::SwapGlBuffers()
{
    if (_deviceContext != nullptr)
    {
        SwapBuffers(_deviceContext);
    }
}

void NativeWindow::DestroyGlContext()
{
    if (_glContext != nullptr)
    {
        wglMakeCurrent(nullptr, nullptr);
        wglDeleteContext(_glContext);
        _glContext = nullptr;
    }

    if (_deviceContext != nullptr)
    {
        ReleaseDC(_handle, _deviceContext);
        _deviceContext = nullptr;
    }
}

LRESULT NativeWindow::HandleDestroy()
{
    DestroyGlContext();

    PostQuitMessage(0);
    return 0;
}

LRESULT NativeWindow::HandlePaint()
{
    PAINTSTRUCT ps;
    auto hdc = BeginPaint(_handle, &ps);

    GetClientRect(_handle, &_clientRect);

    if (_pixelsBuffer == nullptr && _glContext == nullptr)
    {
        FillRect(hdc, &_clientRect, GetSysColorBrush(COLOR_WINDOW));
    }

    if (_paintCallBack != nullptr)
    {
        _paintCallBack(_clientRect.right, _clientRect.bottom);
    }

    if (_pixelsBuffer != nullptr && _glContext == nullptr)
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

    _wheelCallBack(point.x, point.y, horizontal ? delta : 0, horizontal ? 0 : delta, GetModifiers());

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
    case IXEN_CURSOR_RESIZE_DIAGONAL_UP: name = IDC_SIZENESW; break;
    case IXEN_CURSOR_RESIZE_DIAGONAL_DOWN: name = IDC_SIZENWSE; break;
    case IXEN_CURSOR_MOVE: name = IDC_SIZEALL; break;
    case IXEN_CURSOR_NOT_ALLOWED: name = IDC_NO; break;
    case IXEN_CURSOR_HELP: name = IDC_HELP; break;
    case IXEN_CURSOR_PROGRESS: name = IDC_APPSTARTING; break;
    case IXEN_CURSOR_HIDDEN: name = nullptr; break;
    }

    _cursor = name == nullptr ? nullptr : LoadCursor(nullptr, name);

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

LRESULT NativeWindow::HandleGetObject(WPARAM wParam, LPARAM lParam)
{
    if (_accessibilityCallBack == nullptr)
    {
        return DefWindowProc(_handle, WM_GETOBJECT, wParam, lParam);
    }

    LRESULT answered = (LRESULT)_accessibilityCallBack((__int64)wParam, (__int64)lParam);

    if (answered == 0)
    {
        return DefWindowProc(_handle, WM_GETOBJECT, wParam, lParam);
    }

    return answered;
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

static std::wstring ReadComposition(HIMC context, DWORD which)
{
    LONG bytes = ImmGetCompositionStringW(context, which, nullptr, 0);

    if (bytes <= 0)
    {
        return std::wstring();
    }

    std::wstring text(bytes / sizeof(wchar_t), L'\0');

    ImmGetCompositionStringW(context, which, &text[0], bytes);

    return text;
}

LRESULT NativeWindow::HandleComposition(LPARAM lParam)
{
    if (_imeCallBack == nullptr)
    {
        return 0;
    }

    HIMC context = ImmGetContext(_handle);

    if (context == nullptr)
    {
        return 0;
    }

    if ((lParam & GCS_RESULTSTR) != 0)
    {
        std::wstring done = ReadComposition(context, GCS_RESULTSTR);

        _imeCallBack(IXEN_IME_COMMIT, done.c_str(), 0);
    }

    if ((lParam & GCS_COMPSTR) != 0)
    {
        std::wstring running = ReadComposition(context, GCS_COMPSTR);
        LONG caret = ImmGetCompositionStringW(context, GCS_CURSORPOS, nullptr, 0);

        _imeCallBack(IXEN_IME_UPDATE, running.c_str(), caret < 0 ? 0 : (int)caret);
    }

    ImmReleaseContext(_handle, context);

    return 0;
}

LRESULT NativeWindow::HandleEndComposition()
{
    if (_imeCallBack != nullptr)
    {
        _imeCallBack(IXEN_IME_CANCEL, L"", 0);
    }

    return 0;
}

LRESULT NativeWindow::HandleKey(int kind, WPARAM wParam, LPARAM lParam)
{
    if (_keyCallBack != nullptr)
    {
        int repeat = (lParam & (1 << 30)) != 0 ? 1 : 0;

        _keyCallBack(kind, (int)wParam, GetModifiers(), repeat);
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
    case WM_GETOBJECT:
        return HandleGetObject(wParam, lParam);
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

    case WM_IME_COMPOSITION:
        return HandleComposition(lParam);
    case WM_IME_ENDCOMPOSITION:
        return HandleEndComposition();

    case WM_KEYDOWN:
        return HandleKey(IXEN_KEY_DOWN, wParam, lParam);
    case WM_KEYUP:
        return HandleKey(IXEN_KEY_UP, wParam, lParam);
    case WM_CHAR:
        return HandleKey(IXEN_KEY_CHAR, wParam, lParam);

    case WM_SYSKEYDOWN:
        HandleKey(IXEN_KEY_DOWN, wParam, lParam);
        break;
    case WM_SYSKEYUP:
        HandleKey(IXEN_KEY_UP, wParam, lParam);
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