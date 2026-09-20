#ifndef _NATIVE_BACKEND_H_
#define _NATIVE_BACKEND_H_

#include "native_window.h"

#define CURSOR_COUNT 14

typedef struct NativeBackend
{
    const char* name;

    int (*run)(NativeWindow*);
    void (*destroy)(NativeWindow*);
    void (*setTitle)(NativeWindow*, const char*);
    void (*setPixelsBuffer)(NativeWindow*, void*, int, int, int);
    void (*invalidate)(NativeWindow*);
    void (*close)(NativeWindow*);
    unsigned int (*getDpi)(NativeWindow*);
    int (*isPresentable)(NativeWindow*);
    void (*setCursor)(NativeWindow*, int);
    void (*setCursorImage)(NativeWindow*, const void*, int, int, int, int);
    const char* (*getClipboardText)(NativeWindow*);
    void (*setClipboardText)(NativeWindow*, const char*);
    void (*registerPaintCallBack)(NativeWindow*, void (*)(int, int));
    void (*registerPointerCallBack)(NativeWindow*, void (*)(int, int, int, int));
    void (*registerKeyCallBack)(NativeWindow*, void (*)(int, int, int, int));
    void (*registerTextCallBack)(NativeWindow*, void (*)(const char*));
    void (*registerWheelCallBack)(NativeWindow*, void (*)(int, int, int, int, int));
    void (*registerAccessibilityCallBack)(NativeWindow*, int (*)(int, int, const char*));
    int (*accessibilityIsActive)(NativeWindow*);
    void (*accessibilityUpdateNode)(NativeWindow*, int, int, int, long long, int,
        int, int, int, int, const char*, const char*, const char*, const char*);
    void (*accessibilityCommit)(NativeWindow*, int, const int*, int);
    void (*accessibilityNotify)(NativeWindow*, int, int, const char*);
} NativeBackend;

struct NativeWindow
{
    const NativeBackend* backend;
};

extern const NativeBackend X11_Backend;
extern const NativeBackend WL_Backend;

NativeWindow* X11_Create(const char* title, int width, int height);
NativeWindow* WL_Create(const char* title, int width, int height);

extern const char* const CURSOR_NAMES[CURSOR_COUNT];

long long NT_Now(void);
void NT_Fire(const int* stop);
int NT_Timeout(int immediate);

#endif
