#ifndef _NATIVE_WINDOW_H_
#define _NATIVE_WINDOW_H_

#include <windows.h>
#include <map>

using namespace std;

namespace IxenWindowsNative
{
    class NativeWindow
    {
    private:
        static int _windowNum;
        static map<HWND, NativeWindow*> _windowsByHandle;
        static LRESULT CALLBACK WindowProc(HWND handle, UINT msg, WPARAM wParam, LPARAM lParam);

        HWND _handle = nullptr;
        void (*_paintCallBack)(int, int) = nullptr;
        void (*_pointerCallBack)(int, int, int, int) = nullptr;
        void* _pixelsBuffer = nullptr;

        bool _trackingMouse = false;

        RECT _clientRect = {};
        BITMAPINFOHEADER _bitmapInfoHeader = {};

        LRESULT StartEventLoop();
        LRESULT CALLBACK Proc(UINT msg, WPARAM wParam, LPARAM lParam);
        LRESULT HandleDestroy();
        LRESULT HandlePaint();
        LRESULT HandlePointer(int kind, int button, LPARAM lParam);
        LRESULT HandleMouseLeave();

    public:
        NativeWindow(LPCWSTR title, int width, int height);

        static NativeWindow* GetFromHandle(HWND handle);

        LRESULT Show();
        LPWSTR GetTitle();
        void SetTitle(LPCWSTR value);
        void Invalidate();

        HWND GetHandle() { return _handle; }
        void SetPixelsBuffer(void* buffer) { _pixelsBuffer = buffer; }
        void SetOnPaintCallBack(void __stdcall callback(int, int)) { _paintCallBack = callback; }
        void SetOnPointerCallBack(void __stdcall callback(int, int, int, int)) { _pointerCallBack = callback; }
    };
}

#endif