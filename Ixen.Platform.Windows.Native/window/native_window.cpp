#include "native_window.h"

#include <windows.h>
#include <windowsx.h>
#include <iostream>
#include <map>
#include <string>
#include <GL/gl.h>
#include <imm.h>
#include <shellapi.h>
#include <ole2.h>
#include <shlobj.h>

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

#define IXEN_VK_MEDIA_PAUSE 256

#define IXEN_AREA_CLIENT 0
#define IXEN_AREA_CAPTION 1
#define IXEN_AREA_MAXIMIZE 2

#define IXEN_MOD_SHIFT 1
#define IXEN_MOD_CONTROL 2
#define IXEN_MOD_ALT 4
#define IXEN_MOD_META 8

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

#define IXEN_GL_NONE 0
#define IXEN_GL_CURRENT 1
#define IXEN_GL_RECREATED 2

#define IXEN_BUTTON_NONE 0
#define IXEN_BUTTON_LEFT 1
#define IXEN_BUTTON_MIDDLE 2
#define IXEN_BUTTON_RIGHT 3

#define IXEN_CHANGE_PREFERENCES 1
#define IXEN_CHANGE_DPI 2

#define IXEN_DRAG_OVER 0
#define IXEN_DRAG_LEAVE 1

using namespace std;
using namespace IxenWindowsNative;

namespace IxenWindowsNative
{
    class FileDropTarget : public IDropTarget
    {
    private:
        LONG _references = 1;
        NativeWindow* _window;
        IDropTargetHelper* _helper = nullptr;
        wstring _paths;
        bool _accepted = false;

        static wstring PathsOf(IDataObject* data)
        {
            wstring paths;

            if (data == nullptr)
            {
                return paths;
            }

            FORMATETC format = { CF_HDROP, nullptr, DVASPECT_CONTENT, -1, TYMED_HGLOBAL };
            STGMEDIUM medium = {};

            if (FAILED(data->GetData(&format, &medium)))
            {
                return paths;
            }

            HDROP drop = (HDROP)GlobalLock(medium.hGlobal);

            if (drop != nullptr)
            {
                UINT count = DragQueryFileW(drop, 0xFFFFFFFF, nullptr, 0);

                for (UINT index = 0; index < count; index++)
                {
                    UINT length = DragQueryFileW(drop, index, nullptr, 0);

                    if (length == 0)
                    {
                        continue;
                    }

                    wstring path(length, L'\0');
                    DragQueryFileW(drop, index, &path[0], length + 1);

                    if (!paths.empty())
                    {
                        paths += L'\n';
                    }

                    paths += path;
                }

                GlobalUnlock(medium.hGlobal);
            }

            ReleaseStgMedium(&medium);

            return paths;
        }

        DWORD EffectFor(DWORD allowed) const
        {
            if (!_accepted)
            {
                return DROPEFFECT_NONE;
            }

            if (allowed & DROPEFFECT_COPY)
            {
                return DROPEFFECT_COPY;
            }

            if (allowed & DROPEFFECT_LINK)
            {
                return DROPEFFECT_LINK;
            }

            return allowed & DROPEFFECT_MOVE;
        }

        void Track(POINTL point, DWORD* effect)
        {
            _accepted = !_paths.empty() && _window->DragFilesAt(point, _paths.c_str());
            *effect = EffectFor(*effect);
        }

    public:
        FileDropTarget(NativeWindow* window)
            : _window(window)
        {
            if (FAILED(CoCreateInstance(CLSID_DragDropHelper, nullptr, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(&_helper))))
            {
                _helper = nullptr;
            }
        }

        virtual ~FileDropTarget()
        {
            if (_helper != nullptr)
            {
                _helper->Release();
            }
        }

        HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** object) override
        {
            if (object == nullptr)
            {
                return E_POINTER;
            }

            if (riid == IID_IUnknown || riid == IID_IDropTarget)
            {
                *object = static_cast<IDropTarget*>(this);
                AddRef();
                return S_OK;
            }

            *object = nullptr;
            return E_NOINTERFACE;
        }

        ULONG STDMETHODCALLTYPE AddRef() override
        {
            return InterlockedIncrement(&_references);
        }

        ULONG STDMETHODCALLTYPE Release() override
        {
            LONG left = InterlockedDecrement(&_references);

            if (left == 0)
            {
                delete this;
            }

            return left;
        }

        HRESULT STDMETHODCALLTYPE DragEnter(IDataObject* data, DWORD keys, POINTL point, DWORD* effect) override
        {
            _paths = PathsOf(data);
            Track(point, effect);

            if (_helper != nullptr)
            {
                POINT at = { point.x, point.y };
                _helper->DragEnter(_window->GetHandle(), data, &at, *effect);
            }

            return S_OK;
        }

        HRESULT STDMETHODCALLTYPE DragOver(DWORD keys, POINTL point, DWORD* effect) override
        {
            Track(point, effect);

            if (_helper != nullptr)
            {
                POINT at = { point.x, point.y };
                _helper->DragOver(&at, *effect);
            }

            return S_OK;
        }

        HRESULT STDMETHODCALLTYPE DragLeave() override
        {
            _paths.clear();
            _accepted = false;
            _window->LeaveFiles();

            if (_helper != nullptr)
            {
                _helper->DragLeave();
            }

            return S_OK;
        }

        HRESULT STDMETHODCALLTYPE Drop(IDataObject* data, DWORD keys, POINTL point, DWORD* effect) override
        {
            wstring paths = _paths.empty() ? PathsOf(data) : _paths;

            *effect = EffectFor(*effect);

            if (_helper != nullptr)
            {
                POINT at = { point.x, point.y };
                _helper->Drop(data, &at, *effect);
            }

            _paths.clear();
            _accepted = false;

            if (!paths.empty())
            {
                _window->DropFilesAt(point, paths.c_str());
            }

            return S_OK;
        }
    };
}

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

    _logicalWidth = logicalWidth;
    _logicalHeight = logicalHeight;

    RECT rect = { 0, 0,
        MulDiv(logicalWidth, dpi, USER_DEFAULT_SCREEN_DPI),
        MulDiv(logicalHeight, dpi, USER_DEFAULT_SCREEN_DPI) };

    AdjustWindowRectExForDpi(&rect, WS_OVERLAPPEDWINDOW, FALSE, WS_EX_WINDOWEDGE, dpi);

    if (_extended)
    {
        rect.top = 0;
    }

    SetWindowPos(_handle, nullptr, 0, 0,
        rect.right - rect.left,
        rect.bottom - rect.top,
        SWP_NOMOVE | SWP_NOZORDER | SWP_NOACTIVATE);
}

void NativeWindow::SetExtended(bool value)
{
    if (_extended == value || _handle == nullptr)
    {
        return;
    }

    _extended = value;

    ApplyLogicalSize(_logicalWidth, _logicalHeight);

    SetWindowPos(_handle, nullptr, 0, 0, 0, 0,
        SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
}

int NativeWindow::FrameThickness()
{
    UINT dpi = GetDpi();

    return GetSystemMetricsForDpi(SM_CYFRAME, dpi) + GetSystemMetricsForDpi(SM_CXPADDEDBORDER, dpi);
}

LRESULT NativeWindow::HandleNcCalcSize(WPARAM wParam, LPARAM lParam)
{
    if (!_extended || !wParam)
    {
        return DefWindowProc(_handle, WM_NCCALCSIZE, wParam, lParam);
    }

    NCCALCSIZE_PARAMS* sizes = reinterpret_cast<NCCALCSIZE_PARAMS*>(lParam);
    LONG top = sizes->rgrc[0].top;
    LRESULT result = DefWindowProc(_handle, WM_NCCALCSIZE, wParam, lParam);

    sizes->rgrc[0].top = top;

    if (IsZoomed(_handle))
    {
        sizes->rgrc[0].top += FrameThickness();
    }

    return result;
}

LRESULT NativeWindow::HandleNcHitTest(LPARAM lParam)
{
    LRESULT hit = DefWindowProc(_handle, WM_NCHITTEST, 0, lParam);

    if (!_extended || hit != HTCLIENT)
    {
        return hit;
    }

    POINT point = { GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam) };

    ScreenToClient(_handle, &point);

    if (!IsZoomed(_handle) && point.y < FrameThickness())
    {
        RECT client = {};
        int frame = FrameThickness();

        GetClientRect(_handle, &client);

        if (point.x < frame)
        {
            return HTTOPLEFT;
        }

        if (point.x >= client.right - frame)
        {
            return HTTOPRIGHT;
        }

        return HTTOP;
    }

    int area = _hitTestCallBack != nullptr ? _hitTestCallBack(point.x, point.y) : IXEN_AREA_CLIENT;

    if (area == IXEN_AREA_CAPTION)
    {
        return HTCAPTION;
    }

    if (area == IXEN_AREA_MAXIMIZE)
    {
        return HTMAXBUTTON;
    }

    return HTCLIENT;
}

bool NativeWindow::HandleNcPointer(int kind, WPARAM wParam, LPARAM lParam)
{
    if (!_extended || wParam != HTMAXBUTTON)
    {
        return false;
    }

    POINT point = { GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam) };

    ScreenToClient(_handle, &point);

    if (kind == IXEN_POINTER_MOVE && !_trackingNonClient)
    {
        TRACKMOUSEEVENT tme = {};
        tme.cbSize = sizeof(TRACKMOUSEEVENT);
        tme.dwFlags = TME_LEAVE | TME_NONCLIENT;
        tme.hwndTrack = _handle;

        if (TrackMouseEvent(&tme))
        {
            _trackingNonClient = true;
        }
    }

    HandlePointer(kind, kind == IXEN_POINTER_MOVE ? IXEN_BUTTON_NONE : IXEN_BUTTON_LEFT, MAKELPARAM(point.x, point.y));

    return true;
}

LRESULT NativeWindow::HandleNcMouseLeave()
{
    _trackingNonClient = false;

    if (_pointerCallBack != nullptr && GetCapture() != _handle)
    {
        _pointerCallBack(IXEN_POINTER_LEAVE, 0, 0, IXEN_BUTTON_NONE);
    }

    return 0;
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
    NotifySystemChange(IXEN_CHANGE_DPI);

    return 0;
}

void NativeWindow::NotifySystemChange(int kind)
{
    if (_systemCallBack != nullptr)
    {
        _systemCallBack(kind);
    }
}

LRESULT NativeWindow::HandleDisplayChange()
{
    ClampToWorkArea();
    Invalidate();

    return 0;
}

void NativeWindow::ClampToWorkArea()
{
    if (_handle == nullptr || IsIconic(_handle) || IsZoomed(_handle))
    {
        return;
    }

    HMONITOR monitor = MonitorFromWindow(_handle, MONITOR_DEFAULTTONEAREST);
    MONITORINFO info = {};
    info.cbSize = sizeof(MONITORINFO);

    RECT window = {};

    if (monitor == nullptr || !GetMonitorInfo(monitor, &info) || !GetWindowRect(_handle, &window))
    {
        return;
    }

    RECT work = info.rcWork;

    int width = window.right - window.left;
    int height = window.bottom - window.top;
    int available = work.right - work.left;
    int room = work.bottom - work.top;

    if (width > available)
    {
        width = available;
    }

    if (height > room)
    {
        height = room;
    }

    int left = window.left < work.left ? work.left : window.left;
    int top = window.top < work.top ? work.top : window.top;

    if (left + width > work.right)
    {
        left = work.right - width;
    }

    if (top + height > work.bottom)
    {
        top = work.bottom - height;
    }

    if (left == window.left && top == window.top
        && width == window.right - window.left && height == window.bottom - window.top)
    {
        return;
    }

    SetWindowPos(_handle, nullptr, left, top, width, height, SWP_NOZORDER | SWP_NOACTIVATE);
}

LRESULT NativeWindow::HandlePower(WPARAM wParam)
{
    if (wParam == PBT_APMSUSPEND && _suspendCallBack != nullptr)
    {
        _suspendCallBack();
    }

    if (wParam == PBT_APMRESUMESUSPEND || wParam == PBT_APMRESUMEAUTOMATIC)
    {
        Invalidate();
    }

    return TRUE;
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

int NativeWindow::EnsureGlContext()
{
    if (_glContext == nullptr || _deviceContext == nullptr)
    {
        return IXEN_GL_NONE;
    }

    if (wglMakeCurrent(_deviceContext, _glContext))
    {
        return IXEN_GL_CURRENT;
    }

    DestroyGlContext();

    return CreateGlContext() ? IXEN_GL_RECREATED : IXEN_GL_NONE;
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

    if (_oleDrop)
    {
        RevokeDragDrop(_handle);
        _oleDrop = false;
    }

    if (_fileDropTarget != nullptr)
    {
        _fileDropTarget->Release();
        _fileDropTarget = nullptr;
        OleUninitialize();
    }

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

    ApplyCursor(name == nullptr ? nullptr : LoadCursor(nullptr, name));
}

void NativeWindow::SetCursorHandle(void* cursor)
{
    ApplyCursor((HCURSOR)cursor);
}

void NativeWindow::ApplyCursor(HCURSOR cursor)
{
    _cursor = cursor;

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

    if ((GetKeyState(VK_LWIN) | GetKeyState(VK_RWIN)) & 0x8000)
    {
        modifiers |= IXEN_MOD_META;
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

LRESULT NativeWindow::HandleDropFiles(WPARAM wParam)
{
    HDROP drop = (HDROP)wParam;

    if (_dropCallBack == nullptr)
    {
        DragFinish(drop);
        return 0;
    }

    POINT point = {};
    DragQueryPoint(drop, &point);

    UINT count = DragQueryFileW(drop, 0xFFFFFFFF, nullptr, 0);
    std::wstring paths;

    for (UINT index = 0; index < count; index++)
    {
        UINT length = DragQueryFileW(drop, index, nullptr, 0);

        if (length == 0)
        {
            continue;
        }

        std::wstring path(length, L'\0');
        DragQueryFileW(drop, index, &path[0], length + 1);

        if (!paths.empty())
        {
            paths += L'\n';
        }

        paths += path;
    }

    DragFinish(drop);

    _dropCallBack(point.x, point.y, paths.c_str());

    return 0;
}

void NativeWindow::SetAcceptsFiles(bool value)
{
    if (!_handle || value == _acceptsFiles)
    {
        return;
    }

    _acceptsFiles = value;

    if (!value)
    {
        if (_oleDrop)
        {
            RevokeDragDrop(_handle);
            _oleDrop = false;
        }

        DragAcceptFiles(_handle, FALSE);
        return;
    }

    if (_fileDropTarget == nullptr && SUCCEEDED(OleInitialize(nullptr)))
    {
        _fileDropTarget = new FileDropTarget(this);
    }

    _oleDrop = _fileDropTarget != nullptr && SUCCEEDED(RegisterDragDrop(_handle, _fileDropTarget));

    if (!_oleDrop)
    {
        DragAcceptFiles(_handle, TRUE);
    }
}

static POINT ClientPoint(HWND handle, POINTL screen)
{
    POINT point = { screen.x, screen.y };
    ScreenToClient(handle, &point);

    return point;
}

bool NativeWindow::DragFilesAt(POINTL screen, const wchar_t* paths)
{
    if (_dragCallBack == nullptr)
    {
        return _dropCallBack != nullptr;
    }

    POINT point = ClientPoint(_handle, screen);

    return _dragCallBack(IXEN_DRAG_OVER, point.x, point.y, paths) != 0;
}

void NativeWindow::LeaveFiles()
{
    if (_dragCallBack != nullptr)
    {
        _dragCallBack(IXEN_DRAG_LEAVE, 0, 0, L"");
    }
}

void NativeWindow::DropFilesAt(POINTL screen, const wchar_t* paths)
{
    if (_dropCallBack == nullptr)
    {
        return;
    }

    POINT point = ClientPoint(_handle, screen);

    _dropCallBack(point.x, point.y, paths);
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

bool NativeWindow::HandleKey(int kind, WPARAM wParam, LPARAM lParam)
{
    if (_keyCallBack == nullptr)
    {
        return false;
    }

    int repeat = (lParam & (1 << 30)) != 0 ? 1 : 0;

    return _keyCallBack(kind, (int)wParam, GetModifiers(), repeat) != 0;
}

static bool IsSystemKey(WPARAM key)
{
    return key >= VK_BROWSER_BACK && key <= VK_LAUNCH_APP2;
}

static int KeyOfAppCommand(int command)
{
    switch (command)
    {
    case APPCOMMAND_MEDIA_PLAY_PAUSE: return VK_MEDIA_PLAY_PAUSE;
    case APPCOMMAND_MEDIA_PLAY: return VK_PLAY;
    case APPCOMMAND_MEDIA_PAUSE: return IXEN_VK_MEDIA_PAUSE;
    case APPCOMMAND_MEDIA_STOP: return VK_MEDIA_STOP;
    case APPCOMMAND_MEDIA_NEXTTRACK: return VK_MEDIA_NEXT_TRACK;
    case APPCOMMAND_MEDIA_PREVIOUSTRACK: return VK_MEDIA_PREV_TRACK;
    default: return 0;
    }
}

bool NativeWindow::HandleAppCommand(LPARAM lParam)
{
    int key = KeyOfAppCommand(GET_APPCOMMAND_LPARAM(lParam));

    if (key == 0 || _keyCallBack == nullptr)
    {
        return false;
    }

    bool handled = _keyCallBack(IXEN_KEY_DOWN, key, GetModifiers(), 0) != 0;

    _keyCallBack(IXEN_KEY_UP, key, GetModifiers(), 0);

    return handled;
}

LRESULT NativeWindow::HandleCaptureLost()
{
    if (_pointerCallBack != nullptr)
    {
        _pointerCallBack(IXEN_POINTER_CAPTURELOST, 0, 0, IXEN_BUTTON_NONE);
    }

    return 0;
}

bool NativeWindow::HandleTouch(UINT msg, WPARAM wParam, LPARAM lParam)
{
    if (_touchCallBack == nullptr)
    {
        return false;
    }

    UINT32 id = GET_POINTERID_WPARAM(wParam);
    POINTER_INPUT_TYPE type = PT_POINTER;

    if (!GetPointerType(id, &type) || type != PT_TOUCH)
    {
        return false;
    }

    if (msg == WM_POINTERCAPTURECHANGED)
    {
        _touchCallBack(IXEN_POINTER_CAPTURELOST, 0, 0, (int)id);
        return true;
    }

    POINT point = { GET_X_LPARAM(lParam), GET_Y_LPARAM(lParam) };
    ScreenToClient(_handle, &point);

    int kind = IXEN_POINTER_MOVE;

    if (msg == WM_POINTERDOWN)
    {
        kind = IXEN_POINTER_DOWN;
    }
    else if (msg == WM_POINTERUP)
    {
        kind = IS_POINTER_CANCELED_WPARAM(wParam) ? IXEN_POINTER_CAPTURELOST : IXEN_POINTER_UP;
    }
    else if (!IS_POINTER_INCONTACT_WPARAM(wParam))
    {
        return true;
    }

    _touchCallBack(kind, point.x, point.y, (int)id);

    return true;
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
    case WM_NCCALCSIZE:
        return HandleNcCalcSize(wParam, lParam);
    case WM_NCHITTEST:
        return HandleNcHitTest(lParam);
    case WM_NCMOUSEMOVE:
        if (HandleNcPointer(IXEN_POINTER_MOVE, wParam, lParam))
        {
            return 0;
        }

        break;
    case WM_NCLBUTTONDOWN:
    case WM_NCLBUTTONDBLCLK:
        if (HandleNcPointer(IXEN_POINTER_DOWN, wParam, lParam))
        {
            return 0;
        }

        break;
    case WM_NCLBUTTONUP:
        if (HandleNcPointer(IXEN_POINTER_UP, wParam, lParam))
        {
            return 0;
        }

        break;
    case WM_NCMOUSELEAVE:
        HandleNcMouseLeave();
        break;
    case WM_GETOBJECT:
        return HandleGetObject(wParam, lParam);
    case WM_DESTROY:
        return HandleDestroy();
    case WM_PAINT:
        return HandlePaint();

    case WM_POINTERDOWN:
    case WM_POINTERUPDATE:
    case WM_POINTERUP:
    case WM_POINTERCAPTURECHANGED:
        if (HandleTouch(msg, wParam, lParam))
        {
            return 0;
        }

        break;

    case WM_MOUSEMOVE:
        return HandlePointer(IXEN_POINTER_MOVE, IXEN_BUTTON_NONE, lParam);
    case WM_MOUSELEAVE:
        return HandleMouseLeave();
    case WM_CAPTURECHANGED:
        return HandleCaptureLost();
    case WM_DPICHANGED:
        return HandleDpiChanged(lParam);

    case WM_DISPLAYCHANGE:
        return HandleDisplayChange();

    case WM_POWERBROADCAST:
        return HandlePower(wParam);

    case WM_SETTINGCHANGE:
    case WM_SYSCOLORCHANGE:
    case WM_THEMECHANGED:
        NotifySystemChange(IXEN_CHANGE_PREFERENCES);
        break;

    case WM_DROPFILES:
        return HandleDropFiles(wParam);

    case WM_IME_COMPOSITION:
        return HandleComposition(lParam);
    case WM_IME_ENDCOMPOSITION:
        return HandleEndComposition();

    case WM_KEYDOWN:
        if (HandleKey(IXEN_KEY_DOWN, wParam, lParam) || !IsSystemKey(wParam))
        {
            return 0;
        }

        break;
    case WM_KEYUP:
        if (HandleKey(IXEN_KEY_UP, wParam, lParam) || !IsSystemKey(wParam))
        {
            return 0;
        }

        break;
    case WM_CHAR:
        HandleKey(IXEN_KEY_CHAR, wParam, lParam);
        return 0;
    case WM_APPCOMMAND:
        if (HandleAppCommand(lParam))
        {
            return TRUE;
        }

        break;

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