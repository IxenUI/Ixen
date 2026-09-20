#ifndef _NATIVE_WINDOW_H_
#define _NATIVE_WINDOW_H_

#include <X11/Xlib.h>
#include <X11/Xutil.h>

#define IXEN_POINTER_MOVE 0
#define IXEN_POINTER_DOWN 1
#define IXEN_POINTER_UP 2
#define IXEN_POINTER_LEAVE 3
#define IXEN_POINTER_CAPTURELOST 4

#define IXEN_BUTTON_NONE 0
#define IXEN_BUTTON_LEFT 1
#define IXEN_BUTTON_MIDDLE 2
#define IXEN_BUTTON_RIGHT 3

#define IXEN_KEY_DOWN 0
#define IXEN_KEY_UP 1

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

typedef struct NativeWindow NativeWindow;

NativeWindow* NW_Create(const char* title, int width, int height);
int NW_Run(NativeWindow* window);
void NW_Destroy(NativeWindow* window);
void NW_SetTitle(NativeWindow* window, const char* title);
void NW_SetPixelsBuffer(NativeWindow* window, void* buffer, int width, int height, int rowBytes);
void NW_Invalidate(NativeWindow* window);
void NW_Close(NativeWindow* window);
unsigned int NW_GetDpi(NativeWindow* window);
int NW_IsPresentable(NativeWindow* window);
void NW_SetCursor(NativeWindow* window, int kind);
void NW_SetCursorImage(NativeWindow* window, const void* pixels, int width, int height, int hotspotX, int hotspotY);

void NW_RegisterPaintCallBack(NativeWindow* window, void callBack(int, int));
void NW_RegisterPointerCallBack(NativeWindow* window, void callBack(int, int, int, int));
void NW_RegisterKeyCallBack(NativeWindow* window, void callBack(int, int, int, int));
void NW_RegisterTextCallBack(NativeWindow* window, void callBack(const char*));
void NW_RegisterWheelCallBack(NativeWindow* window, void callBack(int, int, int, int, int));

const char* NW_GetClipboardText(NativeWindow* window);
void NW_SetClipboardText(NativeWindow* window, const char* text);

void NW_RegisterAccessibilityCallBack(NativeWindow* window, int callBack(int, int, const char*));
int NW_AccessibilityIsActive(NativeWindow* window);
void NW_AccessibilityUpdateNode(NativeWindow* window, int identifier, int parent, int role, long long states, int actions, int x, int y, int width, int height, const char* name, const char* description, const char* value, const char* shortcut);
void NW_AccessibilityCommit(NativeWindow* window, int root, const int* order, int count);
void NW_AccessibilityNotify(NativeWindow* window, int identifier, int kind, const char* text);

long NW_Schedule(int delayMilliseconds, int repeat, void callBack(long));
void NW_Cancel(long identifier);
int NW_PrefersReducedMotion(void);
int NW_PrefersHighContrast(void);
int NW_TextScale(void);

#endif
