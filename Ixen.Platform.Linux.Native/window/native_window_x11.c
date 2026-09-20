#include "native_window.h"
#include "native_backend.h"

#include "../accessibility/atspi.h"

#include <X11/Xlib.h>
#include <X11/Xutil.h>
#include <X11/Xatom.h>
#include <X11/Xresource.h>
#include <X11/XKBlib.h>
#include <X11/cursorfont.h>
#include <X11/Xcursor/Xcursor.h>

#include <locale.h>
#include <poll.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/eventfd.h>
#include <time.h>
#include <unistd.h>

#define KEYCODE_COUNT 256
#define CLIPBOARD_TIMEOUT_MS 200
#define PROPERTY_MAX_WORDS (1 << 20)
#define WHEEL_NOTCH 120
#define DEFAULT_DPI 96

typedef struct X11Window X11Window;

static void X11_Destroy(NativeWindow* handle);
static void X11_Invalidate(NativeWindow* handle);

struct X11Window
{
    NativeWindow base;

    Display* display;
    int screen;
    Window root;
    Window window;
    Colormap colormap;
    Visual* visual;
    GC gc;

    XImage* image;
    void* pixels;
    int pixelsWidth;
    int pixelsHeight;
    int pixelsRowBytes;

    XIM inputMethod;
    XIC inputContext;

    Atom wmDelete;
    Atom clipboard;
    Atom utf8;
    Atom text;
    Atom targets;
    Atom incoming;
    Atom resources;

    int width;
    int height;
    int mapped;
    int closed;
    int dirty;
    int grabbed;
    int wake;
    unsigned int dpi;

    Cursor cursors[CURSOR_COUNT];
    Cursor owned;

    char* clipboardText;
    char* incomingText;

    unsigned char down[KEYCODE_COUNT];

    void (*paintCallBack)(int, int);
    void (*pointerCallBack)(int, int, int, int);
    void (*keyCallBack)(int, int, int, int);
    void (*textCallBack)(const char*);
    void (*wheelCallBack)(int, int, int, int, int);

    AtspiBridge* accessibility;
};

static const unsigned int CURSOR_SHAPES[CURSOR_COUNT] =
{
    XC_left_ptr, XC_hand2, XC_xterm, XC_watch, XC_crosshair, XC_sb_h_double_arrow,
    XC_sb_v_double_arrow, XC_bottom_left_corner, XC_bottom_right_corner, XC_fleur,
    XC_pirate, XC_question_arrow, XC_watch, 0
};

static int Modifiers(unsigned int state)
{
    int result = 0;

    if ((state & ShiftMask) != 0)
    {
        result |= IXEN_MOD_SHIFT;
    }

    if ((state & ControlMask) != 0)
    {
        result |= IXEN_MOD_CONTROL;
    }

    if ((state & Mod1Mask) != 0)
    {
        result |= IXEN_MOD_ALT;
    }

    if ((state & Mod4Mask) != 0)
    {
        result |= IXEN_MOD_META;
    }

    return result;
}

static int ButtonOf(unsigned int button)
{
    switch (button)
    {
        case Button1: return IXEN_BUTTON_LEFT;
        case Button2: return IXEN_BUTTON_MIDDLE;
        case Button3: return IXEN_BUTTON_RIGHT;
        default: return IXEN_BUTTON_NONE;
    }
}

static unsigned int ReadDpi(X11Window* window)
{
    Atom type = None;
    int format = 0;
    unsigned long count = 0;
    unsigned long remaining = 0;
    unsigned char* data = NULL;
    unsigned int dpi = 0;

    if (XGetWindowProperty(window->display, window->root, window->resources, 0, PROPERTY_MAX_WORDS,
            False, XA_STRING, &type, &format, &count, &remaining, &data) == Success
        && data != NULL)
    {
        XrmDatabase database = XrmGetStringDatabase((const char*)data);

        if (database != NULL)
        {
            char* kind = NULL;
            XrmValue value;

            if (XrmGetResource(database, "Xft.dpi", "Xft.Dpi", &kind, &value) && value.addr != NULL)
            {
                double read = atof(value.addr);

                if (read >= 24 && read <= 960)
                {
                    dpi = (unsigned int)(read + 0.5);
                }
            }

            XrmDestroyDatabase(database);
        }

        XFree(data);
    }

    if (dpi != 0)
    {
        return dpi;
    }

    const char* scale = getenv("GDK_SCALE");

    if (scale == NULL)
    {
        scale = getenv("QT_SCALE_FACTOR");
    }

    if (scale != NULL)
    {
        double factor = atof(scale);

        if (factor >= 0.5 && factor <= 10)
        {
            return (unsigned int)(DEFAULT_DPI * factor + 0.5);
        }
    }

    return DEFAULT_DPI;
}

static void SyncOrigin(X11Window* window)
{
    if (window->accessibility == NULL)
    {
        return;
    }

    int x = 0;
    int y = 0;
    Window child = 0;

    if (XTranslateCoordinates(window->display, window->window, window->root, 0, 0, &x, &y, &child))
    {
        Atspi_SetOrigin(window->accessibility, x, y);
    }
}

static void ApplyTitle(X11Window* window, const char* title)
{
    if (title == NULL)
    {
        title = "";
    }

    Atom name = XInternAtom(window->display, "_NET_WM_NAME", False);

    Atspi_SetTitle(window->accessibility, title);

    XStoreName(window->display, window->window, title);
    XChangeProperty(window->display, window->window, name, window->utf8, 8, PropModeReplace,
        (const unsigned char*)title, (int)strlen(title));
}

NativeWindow* X11_Create(const char* title, int width, int height)
{
    XInitThreads();
    setlocale(LC_CTYPE, "");
    XSetLocaleModifiers("");

    Display* display = XOpenDisplay(NULL);

    if (display == NULL)
    {
        fprintf(stderr, "Ixen: no X display was reachable. Set DISPLAY to one.\n");
        return NULL;
    }

    int screen = DefaultScreen(display);
    XVisualInfo visual;

    if (!XMatchVisualInfo(display, screen, 24, TrueColor, &visual))
    {
        fprintf(stderr, "Ixen: this X display offers no 24-bit TrueColor visual, and Ixen blits "
            "32-bit BGRA into one.\n");
        XCloseDisplay(display);
        return NULL;
    }

    X11Window* window = calloc(1, sizeof(X11Window));

    if (window == NULL)
    {
        XCloseDisplay(display);
        return NULL;
    }

    window->display = display;
    window->screen = screen;
    window->root = RootWindow(display, screen);
    window->visual = visual.visual;
    window->width = width;
    window->height = height;
    window->wake = eventfd(0, EFD_NONBLOCK | EFD_CLOEXEC);
    window->colormap = XCreateColormap(display, window->root, visual.visual, AllocNone);

    XSetWindowAttributes attributes;

    memset(&attributes, 0, sizeof(attributes));

    attributes.colormap = window->colormap;
    attributes.border_pixel = 0;
    attributes.background_pixmap = None;
    attributes.event_mask = ExposureMask | StructureNotifyMask | KeyPressMask | KeyReleaseMask
        | ButtonPressMask | ButtonReleaseMask | PointerMotionMask | EnterWindowMask
        | LeaveWindowMask | FocusChangeMask;

    window->window = XCreateWindow(display, window->root, 0, 0, width, height, 0, 24, InputOutput,
        visual.visual, CWColormap | CWBorderPixel | CWBackPixmap | CWEventMask, &attributes);

    if (window->window == 0)
    {
        X11_Destroy(&window->base);
        return NULL;
    }

    window->utf8 = XInternAtom(display, "UTF8_STRING", False);
    window->clipboard = XInternAtom(display, "CLIPBOARD", False);
    window->targets = XInternAtom(display, "TARGETS", False);
    window->text = XInternAtom(display, "TEXT", False);
    window->incoming = XInternAtom(display, "IXEN_CLIPBOARD", False);
    window->resources = XInternAtom(display, "RESOURCE_MANAGER", False);
    window->wmDelete = XInternAtom(display, "WM_DELETE_WINDOW", False);

    XSetWMProtocols(display, window->window, &window->wmDelete, 1);
    XSelectInput(display, window->root, PropertyChangeMask);

    XClassHint hint;

    hint.res_name = (char*)"ixen";
    hint.res_class = (char*)"Ixen";

    XSetClassHint(display, window->window, &hint);

    ApplyTitle(window, title);

    window->gc = XCreateGC(display, window->window, 0, NULL);
    window->dpi = ReadDpi(window);

    XkbSetDetectableAutoRepeat(display, True, NULL);

    window->accessibility = Atspi_Create();

    Atspi_SetTitle(window->accessibility, title);

    window->inputMethod = XOpenIM(display, NULL, NULL, NULL);

    if (window->inputMethod != NULL)
    {
        window->inputContext = XCreateIC(window->inputMethod, XNInputStyle,
            XIMPreeditNothing | XIMStatusNothing, XNClientWindow, window->window, (void*)NULL);
    }

    if (window->inputContext != NULL)
    {
        XSetICFocus(window->inputContext);
    }

    window->base.backend = &X11_Backend;

    return &window->base;
}

static void X11_SetTitle(NativeWindow* handle, const char* title)
{
    X11Window* window = (X11Window*)handle;

    if (window == NULL)
    {
        return;
    }

    ApplyTitle(window, title);
}

static void X11_SetPixelsBuffer(NativeWindow* handle, void* buffer, int width, int height, int rowBytes)
{
    X11Window* window = (X11Window*)handle;

    if (window == NULL || buffer == NULL || width <= 0 || height <= 0)
    {
        return;
    }

    if (window->image != NULL && (window->pixels != buffer || window->pixelsWidth != width
        || window->pixelsHeight != height || window->pixelsRowBytes != rowBytes))
    {
        window->image->data = NULL;
        XDestroyImage(window->image);
        window->image = NULL;
    }

    window->pixels = buffer;
    window->pixelsWidth = width;
    window->pixelsHeight = height;
    window->pixelsRowBytes = rowBytes;

    if (window->image == NULL)
    {
        window->image = XCreateImage(window->display, window->visual, 24, ZPixmap, 0, (char*)buffer,
            width, height, 32, rowBytes);

        if (window->image != NULL)
        {
            window->image->byte_order = LSBFirst;
        }
    }
}

static void Blit(X11Window* window)
{
    if (window->image == NULL)
    {
        return;
    }

    int width = window->pixelsWidth < window->width ? window->pixelsWidth : window->width;
    int height = window->pixelsHeight < window->height ? window->pixelsHeight : window->height;

    XPutImage(window->display, window->window, window->gc, window->image, 0, 0, 0, 0, width, height);
}

static void Paint(X11Window* window)
{
    if (window->paintCallBack == NULL || window->width <= 0 || window->height <= 0)
    {
        return;
    }

    window->paintCallBack(window->width, window->height);

    Blit(window);
}

static void X11_Invalidate(NativeWindow* handle)
{
    X11Window* window = (X11Window*)handle;

    if (window == NULL)
    {
        return;
    }

    window->dirty = 1;

    uint64_t one = 1;

    if (write(window->wake, &one, sizeof(one)) != sizeof(one))
    {
        return;
    }
}

static void X11_Close(NativeWindow* handle)
{
    X11Window* window = (X11Window*)handle;

    if (window == NULL)
    {
        return;
    }

    window->closed = 1;

    X11_Invalidate(&window->base);
}

static unsigned int X11_GetDpi(NativeWindow* handle)
{
    X11Window* window = (X11Window*)handle;

    return window == NULL ? DEFAULT_DPI : window->dpi;
}

static int X11_IsPresentable(NativeWindow* handle)
{
    X11Window* window = (X11Window*)handle;

    return window != NULL && window->mapped ? 1 : 0;
}

static Cursor HiddenCursor(X11Window* window)
{
    char bits[8];
    XColor colour;

    memset(bits, 0, sizeof(bits));
    memset(&colour, 0, sizeof(colour));

    Pixmap pixmap = XCreateBitmapFromData(window->display, window->window, bits, 1, 1);
    Cursor cursor = XCreatePixmapCursor(window->display, pixmap, pixmap, &colour, &colour, 0, 0);

    XFreePixmap(window->display, pixmap);

    return cursor;
}

static Cursor CursorOf(X11Window* window, int kind)
{
    if (kind < 0 || kind >= CURSOR_COUNT)
    {
        kind = IXEN_CURSOR_DEFAULT;
    }

    if (window->cursors[kind] != 0)
    {
        return window->cursors[kind];
    }

    Cursor cursor = 0;

    if (kind == IXEN_CURSOR_HIDDEN)
    {
        cursor = HiddenCursor(window);
    }
    else
    {
        cursor = XcursorLibraryLoadCursor(window->display, CURSOR_NAMES[kind]);

        if (cursor == 0)
        {
            cursor = XCreateFontCursor(window->display, CURSOR_SHAPES[kind]);
        }
    }

    window->cursors[kind] = cursor;

    return cursor;
}

static void ApplyCursor(X11Window* window, Cursor cursor)
{
    if (cursor == 0)
    {
        return;
    }

    XDefineCursor(window->display, window->window, cursor);
    XFlush(window->display);
}

static void X11_SetCursor(NativeWindow* handle, int kind)
{
    X11Window* window = (X11Window*)handle;

    if (window == NULL)
    {
        return;
    }

    if (window->owned != 0)
    {
        XFreeCursor(window->display, window->owned);
        window->owned = 0;
    }

    ApplyCursor(window, CursorOf(window, kind));
}

static void X11_SetCursorImage(NativeWindow* handle, const void* pixels, int width, int height,
    int hotspotX, int hotspotY)
{
    X11Window* window = (X11Window*)handle;

    if (window == NULL || pixels == NULL || width <= 0 || height <= 0)
    {
        return;
    }

    XcursorImage* image = XcursorImageCreate(width, height);

    if (image == NULL)
    {
        return;
    }

    image->xhot = hotspotX < 0 ? 0 : (hotspotX >= width ? width - 1 : hotspotX);
    image->yhot = hotspotY < 0 ? 0 : (hotspotY >= height ? height - 1 : hotspotY);

    memcpy(image->pixels, pixels, (size_t)width * (size_t)height * 4);

    Cursor cursor = XcursorImageLoadCursor(window->display, image);

    XcursorImageDestroy(image);

    if (cursor == 0)
    {
        return;
    }

    if (window->owned != 0)
    {
        XFreeCursor(window->display, window->owned);
    }

    window->owned = cursor;

    ApplyCursor(window, cursor);
}

static void ReadSelection(X11Window* window, XSelectionEvent* selection)
{
    if (selection->property == None)
    {
        return;
    }

    Atom type = None;
    int format = 0;
    unsigned long count = 0;
    unsigned long remaining = 0;
    unsigned char* data = NULL;

    if (XGetWindowProperty(window->display, window->window, window->incoming, 0,
            PROPERTY_MAX_WORDS, True, AnyPropertyType, &type, &format, &count, &remaining, &data)
        != Success)
    {
        return;
    }

    if (data == NULL)
    {
        return;
    }

    if (format == 8 && count > 0)
    {
        window->incomingText = malloc(count + 1);

        if (window->incomingText != NULL)
        {
            memcpy(window->incomingText, data, count);
            window->incomingText[count] = 0;
        }
    }

    XFree(data);
}

static void AnswerSelection(X11Window* window, XSelectionRequestEvent* request)
{
    XSelectionEvent answer;

    memset(&answer, 0, sizeof(answer));

    answer.type = SelectionNotify;
    answer.display = request->display;
    answer.requestor = request->requestor;
    answer.selection = request->selection;
    answer.target = request->target;
    answer.time = request->time;
    answer.property = None;

    Atom property = request->property == None ? request->target : request->property;

    if (window->clipboardText != NULL)
    {
        if (request->target == window->targets)
        {
            Atom offered[3];

            offered[0] = window->targets;
            offered[1] = window->utf8;
            offered[2] = XA_STRING;

            XChangeProperty(window->display, request->requestor, property, XA_ATOM, 32,
                PropModeReplace, (unsigned char*)offered, 3);

            answer.property = property;
        }
        else if (request->target == window->utf8 || request->target == XA_STRING
            || request->target == window->text)
        {
            XChangeProperty(window->display, request->requestor, property, request->target, 8,
                PropModeReplace, (unsigned char*)window->clipboardText,
                (int)strlen(window->clipboardText));

            answer.property = property;
        }
    }

    XSendEvent(window->display, request->requestor, False, 0, (XEvent*)&answer);
    XFlush(window->display);
}

static const char* X11_GetClipboardText(NativeWindow* handle)
{
    X11Window* window = (X11Window*)handle;

    if (window == NULL)
    {
        return NULL;
    }

    if (window->clipboardText != NULL
        && XGetSelectionOwner(window->display, window->clipboard) == window->window)
    {
        return window->clipboardText;
    }

    free(window->incomingText);
    window->incomingText = NULL;

    XConvertSelection(window->display, window->clipboard, window->utf8, window->incoming,
        window->window, CurrentTime);
    XFlush(window->display);

    long long deadline = NT_Now() + CLIPBOARD_TIMEOUT_MS;

    while (NT_Now() < deadline)
    {
        XEvent event;

        if (XCheckTypedWindowEvent(window->display, window->window, SelectionNotify, &event))
        {
            ReadSelection(window, &event.xselection);

            return window->incomingText;
        }

        if (XCheckTypedWindowEvent(window->display, window->window, SelectionRequest, &event))
        {
            AnswerSelection(window, &event.xselectionrequest);
            continue;
        }

        struct pollfd waiting;

        waiting.fd = ConnectionNumber(window->display);
        waiting.events = POLLIN;
        waiting.revents = 0;

        poll(&waiting, 1, 10);
    }

    return NULL;
}

static void X11_SetClipboardText(NativeWindow* handle, const char* text)
{
    X11Window* window = (X11Window*)handle;

    if (window == NULL)
    {
        return;
    }

    free(window->clipboardText);
    window->clipboardText = NULL;

    if (text != NULL)
    {
        size_t length = strlen(text);

        window->clipboardText = malloc(length + 1);

        if (window->clipboardText != NULL)
        {
            memcpy(window->clipboardText, text, length + 1);
        }
    }

    XSetSelectionOwner(window->display, window->clipboard, window->window, CurrentTime);
    XFlush(window->display);
}

static void Drain(X11Window* window)
{
    uint64_t count = 0;

    while (read(window->wake, &count, sizeof(count)) == sizeof(count))
    {
    }
}

static int LookupLatin1(XKeyEvent* key, char* buffer, int capacity)
{
    char raw[8];
    KeySym symbol = 0;
    int length = XLookupString(key, raw, sizeof(raw), &symbol, NULL);
    int written = 0;

    for (int index = 0; index < length && written + 2 < capacity; index++)
    {
        unsigned char value = (unsigned char)raw[index];

        if (value < 0x80)
        {
            buffer[written++] = (char)value;
        }
        else
        {
            buffer[written++] = (char)(0xC0 | (value >> 6));
            buffer[written++] = (char)(0x80 | (value & 0x3F));
        }
    }

    return written;
}

static void SendText(X11Window* window, XKeyEvent* key)
{
    if (window->textCallBack == NULL)
    {
        return;
    }

    char buffer[64];
    Status status = 0;
    int length;

    if (window->inputContext != NULL)
    {
        length = Xutf8LookupString(window->inputContext, key, buffer, sizeof(buffer) - 1, NULL,
            &status);

        if (status != XLookupChars && status != XLookupBoth)
        {
            return;
        }
    }
    else
    {
        length = LookupLatin1(key, buffer, sizeof(buffer) - 1);
    }

    if (length <= 0)
    {
        return;
    }

    buffer[length] = 0;

    window->textCallBack(buffer);
}

static int IsNamed(KeySym symbol)
{
    return (symbol >= 0x30 && symbol <= 0x39)
        || (symbol >= 0x41 && symbol <= 0x5A)
        || (symbol >= 0x61 && symbol <= 0x7A)
        || symbol == 0x20
        || (symbol >= 0xFE00 && symbol <= 0xFFFF);
}

static int KeySymOf(XKeyEvent* key)
{
    KeySym symbol = XLookupKeysym(key, 0);

    if (IsNamed(symbol))
    {
        return (int)symbol;
    }

    KeySym shifted = XLookupKeysym(key, 1);

    if (shifted >= 0x30 && shifted <= 0x39)
    {
        return (int)shifted;
    }

    return symbol > 0xFFFF ? 0 : (int)symbol;
}

static void SendKey(X11Window* window, XKeyEvent* key, int kind, int repeat)
{
    if (window->keyCallBack == NULL)
    {
        return;
    }

    window->keyCallBack(kind, KeySymOf(key), Modifiers(key->state), repeat);
}

static void SendPointer(X11Window* window, int kind, int x, int y, int button)
{
    if (window->pointerCallBack != NULL)
    {
        window->pointerCallBack(kind, x, y, button);
    }
}

static void Ungrab(X11Window* window)
{
    if (!window->grabbed)
    {
        return;
    }

    window->grabbed = 0;

    XUngrabPointer(window->display, CurrentTime);
}

static void Wheel(X11Window* window, XButtonEvent* button)
{
    if (window->wheelCallBack == NULL)
    {
        return;
    }

    int deltaX = 0;
    int deltaY = 0;

    switch (button->button)
    {
        case 4: deltaY = WHEEL_NOTCH; break;
        case 5: deltaY = -WHEEL_NOTCH; break;
        case 6: deltaX = -WHEEL_NOTCH; break;
        case 7: deltaX = WHEEL_NOTCH; break;
        default: return;
    }

    window->wheelCallBack(button->x, button->y, deltaX, deltaY, Modifiers(button->state));
}

static void Dispatch(X11Window* window, XEvent* event)
{
    switch (event->type)
    {
        case Expose:
            if (event->xexpose.count == 0)
            {
                window->dirty = 1;
            }
            break;

        case ConfigureNotify:
            if (event->xconfigure.width != window->width
                || event->xconfigure.height != window->height)
            {
                window->width = event->xconfigure.width;
                window->height = event->xconfigure.height;
                window->dirty = 1;
            }

            SyncOrigin(window);
            break;

        case MapNotify:
            window->mapped = 1;
            window->dirty = 1;
            break;

        case UnmapNotify:
            window->mapped = 0;
            break;

        case ClientMessage:
            if ((Atom)event->xclient.data.l[0] == window->wmDelete)
            {
                window->closed = 1;
            }
            break;

        case PropertyNotify:
            if (event->xproperty.window == window->root
                && event->xproperty.atom == window->resources)
            {
                window->dpi = ReadDpi(window);
                window->dirty = 1;
            }
            break;

        case MotionNotify:
            SendPointer(window, IXEN_POINTER_MOVE, event->xmotion.x, event->xmotion.y,
                IXEN_BUTTON_NONE);
            break;

        case EnterNotify:
            SendPointer(window, IXEN_POINTER_MOVE, event->xcrossing.x, event->xcrossing.y,
                IXEN_BUTTON_NONE);
            break;

        case LeaveNotify:
            SendPointer(window, IXEN_POINTER_LEAVE, event->xcrossing.x, event->xcrossing.y,
                IXEN_BUTTON_NONE);
            break;

        case ButtonPress:
            if (event->xbutton.button >= 4 && event->xbutton.button <= 7)
            {
                Wheel(window, &event->xbutton);
                break;
            }

            if (!window->grabbed)
            {
                window->grabbed = XGrabPointer(window->display, window->window, False,
                    ButtonPressMask | ButtonReleaseMask | PointerMotionMask, GrabModeAsync,
                    GrabModeAsync, None, None, event->xbutton.time) == GrabSuccess;
            }

            SendPointer(window, IXEN_POINTER_DOWN, event->xbutton.x, event->xbutton.y,
                ButtonOf(event->xbutton.button));
            break;

        case ButtonRelease:
            if (event->xbutton.button >= 4 && event->xbutton.button <= 7)
            {
                break;
            }

            SendPointer(window, IXEN_POINTER_UP, event->xbutton.x, event->xbutton.y,
                ButtonOf(event->xbutton.button));

            Ungrab(window);
            break;

        case FocusIn:
            if (window->inputContext != NULL)
            {
                XSetICFocus(window->inputContext);
            }
            break;

        case FocusOut:
            if (window->inputContext != NULL)
            {
                XUnsetICFocus(window->inputContext);
            }

            if (window->grabbed)
            {
                Ungrab(window);
                SendPointer(window, IXEN_POINTER_CAPTURELOST, 0, 0, IXEN_BUTTON_NONE);
            }
            break;

        case KeyPress:
        {
            int code = event->xkey.keycode & 0xFF;
            int repeat = window->down[code];

            window->down[code] = 1;

            SendKey(window, &event->xkey, IXEN_KEY_DOWN, repeat);
            SendText(window, &event->xkey);
            break;
        }

        case KeyRelease:
        {
            int code = event->xkey.keycode & 0xFF;

            window->down[code] = 0;

            SendKey(window, &event->xkey, IXEN_KEY_UP, 0);
            break;
        }

        case SelectionRequest:
            AnswerSelection(window, &event->xselectionrequest);
            break;

        case SelectionClear:
            free(window->clipboardText);
            window->clipboardText = NULL;
            break;
    }
}

static int X11_Run(NativeWindow* handle)
{
    X11Window* window = (X11Window*)handle;

    if (window == NULL)
    {
        return 1;
    }

    XMapWindow(window->display, window->window);
    XFlush(window->display);

    struct pollfd watched[2 + ATSPI_MAX_FDS];

    watched[0].fd = ConnectionNumber(window->display);
    watched[0].events = POLLIN;
    watched[1].fd = window->wake;
    watched[1].events = POLLIN;

    while (!window->closed)
    {
        while (!window->closed && XPending(window->display) > 0)
        {
            XEvent event;

            XNextEvent(window->display, &event);

            if (XFilterEvent(&event, None))
            {
                continue;
            }

            Dispatch(window, &event);
        }

        if (window->closed)
        {
            break;
        }

        NT_Fire(&window->closed);
        Atspi_Pump(window->accessibility);

        if (window->dirty)
        {
            window->dirty = 0;

            Paint(window);
        }

        if (window->closed)
        {
            break;
        }

        XFlush(window->display);

        if (XPending(window->display) > 0)
        {
            continue;
        }

        int fds[ATSPI_MAX_FDS];
        int count = 2 + Atspi_Fds(window->accessibility, fds, ATSPI_MAX_FDS);

        for (int index = 2; index < count; index++)
        {
            watched[index].fd = fds[index - 2];
            watched[index].events = POLLIN;
        }

        for (int index = 0; index < count; index++)
        {
            watched[index].revents = 0;
        }

        poll(watched, (nfds_t)count, NT_Timeout(window->dirty));

        Drain(window);
        Atspi_Pump(window->accessibility);
    }

    return 0;
}

static void X11_Destroy(NativeWindow* handle)
{
    X11Window* window = (X11Window*)handle;

    if (window == NULL)
    {
        return;
    }

    Atspi_Destroy(window->accessibility);

    window->accessibility = NULL;

    if (window->image != NULL)
    {
        window->image->data = NULL;
        XDestroyImage(window->image);
        window->image = NULL;
    }

    for (int index = 0; index < CURSOR_COUNT; index++)
    {
        if (window->cursors[index] != 0)
        {
            XFreeCursor(window->display, window->cursors[index]);
            window->cursors[index] = 0;
        }
    }

    if (window->owned != 0)
    {
        XFreeCursor(window->display, window->owned);
        window->owned = 0;
    }

    if (window->inputContext != NULL)
    {
        XDestroyIC(window->inputContext);
        window->inputContext = NULL;
    }

    if (window->inputMethod != NULL)
    {
        XCloseIM(window->inputMethod);
        window->inputMethod = NULL;
    }

    if (window->gc != NULL)
    {
        XFreeGC(window->display, window->gc);
        window->gc = NULL;
    }

    if (window->window != 0)
    {
        XDestroyWindow(window->display, window->window);
        window->window = 0;
    }

    if (window->colormap != 0)
    {
        XFreeColormap(window->display, window->colormap);
        window->colormap = 0;
    }

    if (window->wake >= 0)
    {
        close(window->wake);
        window->wake = -1;
    }

    free(window->clipboardText);
    free(window->incomingText);

    XCloseDisplay(window->display);

    free(window);
}

static void X11_RegisterPaintCallBack(NativeWindow* handle, void callBack(int, int))
{
    X11Window* window = (X11Window*)handle;

    if (window != NULL)
    {
        window->paintCallBack = callBack;
    }
}

static void X11_RegisterPointerCallBack(NativeWindow* handle, void callBack(int, int, int, int))
{
    X11Window* window = (X11Window*)handle;

    if (window != NULL)
    {
        window->pointerCallBack = callBack;
    }
}

static void X11_RegisterKeyCallBack(NativeWindow* handle, void callBack(int, int, int, int))
{
    X11Window* window = (X11Window*)handle;

    if (window != NULL)
    {
        window->keyCallBack = callBack;
    }
}

static void X11_RegisterTextCallBack(NativeWindow* handle, void callBack(const char*))
{
    X11Window* window = (X11Window*)handle;

    if (window != NULL)
    {
        window->textCallBack = callBack;
    }
}

static void X11_RegisterWheelCallBack(NativeWindow* handle, void callBack(int, int, int, int, int))
{
    X11Window* window = (X11Window*)handle;

    if (window != NULL)
    {
        window->wheelCallBack = callBack;
    }
}

static void X11_RegisterAccessibilityCallBack(NativeWindow* handle, int callBack(int, int, const char*))
{
    X11Window* window = (X11Window*)handle;

    if (window != NULL)
    {
        Atspi_RegisterCallBack(window->accessibility, callBack);
    }
}

static int X11_AccessibilityIsActive(NativeWindow* handle)
{
    X11Window* window = (X11Window*)handle;

    return window == NULL ? 0 : Atspi_IsActive(window->accessibility);
}

static void X11_AccessibilityUpdateNode(NativeWindow* handle, int identifier, int parent, int role,
    long long states, int actions, int x, int y, int width, int height,
    const char* name, const char* description, const char* value, const char* shortcut)
{
    X11Window* window = (X11Window*)handle;

    if (window != NULL)
    {
        Atspi_UpdateNode(window->accessibility, identifier, parent, role, states, actions,
            x, y, width, height, name, description, value, shortcut);
    }
}

static void X11_AccessibilityCommit(NativeWindow* handle, int root, const int* order, int count)
{
    X11Window* window = (X11Window*)handle;

    if (window != NULL)
    {
        SyncOrigin(window);
        Atspi_Commit(window->accessibility, root, order, count);
    }
}

static void X11_AccessibilityNotify(NativeWindow* handle, int identifier, int kind, const char* text)
{
    X11Window* window = (X11Window*)handle;

    if (window != NULL)
    {
        Atspi_Notify(window->accessibility, identifier, kind, text);
    }
}

const NativeBackend X11_Backend =
{
    "X11",
    X11_Run,
    X11_Destroy,
    X11_SetTitle,
    X11_SetPixelsBuffer,
    X11_Invalidate,
    X11_Close,
    X11_GetDpi,
    X11_IsPresentable,
    X11_SetCursor,
    X11_SetCursorImage,
    X11_GetClipboardText,
    X11_SetClipboardText,
    X11_RegisterPaintCallBack,
    X11_RegisterPointerCallBack,
    X11_RegisterKeyCallBack,
    X11_RegisterTextCallBack,
    X11_RegisterWheelCallBack,
    X11_RegisterAccessibilityCallBack,
    X11_AccessibilityIsActive,
    X11_AccessibilityUpdateNode,
    X11_AccessibilityCommit,
    X11_AccessibilityNotify
};
