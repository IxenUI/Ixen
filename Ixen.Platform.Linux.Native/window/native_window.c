#include "native_window.h"
#include "native_backend.h"

#include <stdlib.h>
#include <string.h>
#include <time.h>

#define MAX_FIRES_PER_PASS 8

const char* const CURSOR_NAMES[CURSOR_COUNT] =
{
    "default", "pointer", "text", "wait", "crosshair", "ew-resize", "ns-resize",
    "nesw-resize", "nwse-resize", "move", "not-allowed", "help", "progress", NULL
};

typedef struct NativeTimer
{
    long identifier;
    int repeat;
    int interval;
    long long due;
    void (*callBack)(long);
    struct NativeTimer* next;
} NativeTimer;

static NativeTimer* _timers = NULL;
static long _nextTimerIdentifier = 1;

long long NT_Now(void)
{
    struct timespec moment;

    clock_gettime(CLOCK_MONOTONIC, &moment);

    return (long long)moment.tv_sec * 1000 + moment.tv_nsec / 1000000;
}

long NW_Schedule(int delayMilliseconds, int repeat, void callBack(long))
{
    if (callBack == NULL)
    {
        return 0;
    }

    NativeTimer* timer = calloc(1, sizeof(NativeTimer));

    if (timer == NULL)
    {
        return 0;
    }

    timer->identifier = _nextTimerIdentifier++;
    timer->repeat = repeat;
    timer->interval = delayMilliseconds < 1 ? 1 : delayMilliseconds;
    timer->due = NT_Now() + timer->interval;
    timer->callBack = callBack;
    timer->next = _timers;

    _timers = timer;

    return timer->identifier;
}

void NW_Cancel(long identifier)
{
    NativeTimer* previous = NULL;
    NativeTimer* timer = _timers;

    while (timer != NULL)
    {
        if (timer->identifier == identifier)
        {
            if (previous == NULL)
            {
                _timers = timer->next;
            }
            else
            {
                previous->next = timer->next;
            }

            free(timer);

            return;
        }

        previous = timer;
        timer = timer->next;
    }
}

void NT_Fire(const int* stop)
{
    for (int fired = 0; fired < MAX_FIRES_PER_PASS; fired++)
    {
        long long now = NT_Now();
        NativeTimer* earliest = NULL;
        NativeTimer* timer = _timers;

        while (timer != NULL)
        {
            if (timer->due <= now && (earliest == NULL || timer->due < earliest->due))
            {
                earliest = timer;
            }

            timer = timer->next;
        }

        if (earliest == NULL)
        {
            return;
        }

        long identifier = earliest->identifier;
        void (*callBack)(long) = earliest->callBack;

        if (earliest->repeat)
        {
            earliest->due = now + earliest->interval;
        }
        else
        {
            NW_Cancel(identifier);
        }

        callBack(identifier);

        if (stop != NULL && *stop)
        {
            return;
        }
    }
}

int NT_Timeout(int immediate)
{
    if (immediate)
    {
        return 0;
    }

    long long soonest = -1;
    NativeTimer* timer = _timers;

    while (timer != NULL)
    {
        if (soonest < 0 || timer->due < soonest)
        {
            soonest = timer->due;
        }

        timer = timer->next;
    }

    if (soonest < 0)
    {
        return -1;
    }

    long long wait = soonest - NT_Now();

    return wait <= 0 ? 0 : (int)wait;
}

static int ReadFlag(const char* name)
{
    const char* preference = getenv(name);

    if (preference == NULL)
    {
        return 0;
    }

    return preference[0] == 0x31 || preference[0] == 0x74 || preference[0] == 0x54 ? 1 : 0;
}

int NW_PrefersReducedMotion(void)
{
    return ReadFlag("IXEN_REDUCED_MOTION");
}

int NW_PrefersHighContrast(void)
{
    return ReadFlag("IXEN_HIGH_CONTRAST");
}

int NW_TextScale(void)
{
    const char* preference = getenv("IXEN_FONT_SCALE");
    char* end = NULL;
    long percent;

    if (preference == NULL)
    {
        return 100;
    }

    percent = strtol(preference, &end, 10);

    if (end == preference || percent < 50 || percent > 400)
    {
        return 100;
    }

    return (int)percent;
}

static int PrefersWayland(void)
{
    const char* forced = getenv("IXEN_BACKEND");

    if (forced != NULL)
    {
        return strcmp(forced, "wayland") == 0;
    }

    const char* display = getenv("WAYLAND_DISPLAY");

    return display != NULL && display[0] != 0;
}

NativeWindow* NW_Create(const char* title, int width, int height)
{
    if (PrefersWayland())
    {
        NativeWindow* window = WL_Create(title, width, height);

        if (window != NULL)
        {
            return window;
        }
    }

    return X11_Create(title, width, height);
}

const char* NW_Backend(NativeWindow* window)
{
    return window == NULL ? NULL : window->backend->name;
}

int NW_Run(NativeWindow* window)
{
    return window == NULL ? 0 : window->backend->run(window);
}

void NW_Destroy(NativeWindow* window)
{
    if (window != NULL)
    {
        window->backend->destroy(window);
    }
}

void NW_SetTitle(NativeWindow* window, const char* title)
{
    if (window != NULL)
    {
        window->backend->setTitle(window, title);
    }
}

void NW_SetPixelsBuffer(NativeWindow* window, void* buffer, int width, int height, int rowBytes)
{
    if (window != NULL)
    {
        window->backend->setPixelsBuffer(window, buffer, width, height, rowBytes);
    }
}

void NW_Invalidate(NativeWindow* window)
{
    if (window != NULL)
    {
        window->backend->invalidate(window);
    }
}

void NW_Close(NativeWindow* window)
{
    if (window != NULL)
    {
        window->backend->close(window);
    }
}

unsigned int NW_GetDpi(NativeWindow* window)
{
    return window == NULL ? 96 : window->backend->getDpi(window);
}

int NW_IsPresentable(NativeWindow* window)
{
    return window == NULL ? 0 : window->backend->isPresentable(window);
}

void NW_SetCursor(NativeWindow* window, int kind)
{
    if (window != NULL)
    {
        window->backend->setCursor(window, kind);
    }
}

void NW_SetCursorImage(NativeWindow* window, const void* pixels, int width, int height,
    int hotspotX, int hotspotY)
{
    if (window != NULL)
    {
        window->backend->setCursorImage(window, pixels, width, height, hotspotX, hotspotY);
    }
}

const char* NW_GetClipboardText(NativeWindow* window)
{
    return window == NULL ? NULL : window->backend->getClipboardText(window);
}

void NW_SetClipboardText(NativeWindow* window, const char* text)
{
    if (window != NULL)
    {
        window->backend->setClipboardText(window, text);
    }
}

void NW_RegisterPaintCallBack(NativeWindow* window, void callBack(int, int))
{
    if (window != NULL)
    {
        window->backend->registerPaintCallBack(window, callBack);
    }
}

void NW_RegisterPointerCallBack(NativeWindow* window, void callBack(int, int, int, int))
{
    if (window != NULL)
    {
        window->backend->registerPointerCallBack(window, callBack);
    }
}

void NW_RegisterKeyCallBack(NativeWindow* window, void callBack(int, int, int, int))
{
    if (window != NULL)
    {
        window->backend->registerKeyCallBack(window, callBack);
    }
}

void NW_RegisterTextCallBack(NativeWindow* window, void callBack(const char*))
{
    if (window != NULL)
    {
        window->backend->registerTextCallBack(window, callBack);
    }
}

void NW_RegisterWheelCallBack(NativeWindow* window, void callBack(int, int, int, int, int))
{
    if (window != NULL)
    {
        window->backend->registerWheelCallBack(window, callBack);
    }
}

void NW_RegisterAccessibilityCallBack(NativeWindow* window, int callBack(int, int, const char*))
{
    if (window != NULL)
    {
        window->backend->registerAccessibilityCallBack(window, callBack);
    }
}

int NW_AccessibilityIsActive(NativeWindow* window)
{
    return window == NULL ? 0 : window->backend->accessibilityIsActive(window);
}

void NW_AccessibilityUpdateNode(NativeWindow* window, int identifier, int parent, int role,
    long long states, int actions, int x, int y, int width, int height,
    const char* name, const char* description, const char* value, const char* shortcut)
{
    if (window != NULL)
    {
        window->backend->accessibilityUpdateNode(window, identifier, parent, role, states,
            actions, x, y, width, height, name, description, value, shortcut);
    }
}

void NW_AccessibilityCommit(NativeWindow* window, int root, const int* order, int count)
{
    if (window != NULL)
    {
        window->backend->accessibilityCommit(window, root, order, count);
    }
}

void NW_AccessibilityNotify(NativeWindow* window, int identifier, int kind, const char* text)
{
    if (window != NULL)
    {
        window->backend->accessibilityNotify(window, identifier, kind, text);
    }
}
