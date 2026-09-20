#define _GNU_SOURCE

#include "native_window.h"
#include "native_backend.h"

#include "../accessibility/atspi.h"

#include <wayland-client.h>
#include <wayland-cursor.h>
#include <xkbcommon/xkbcommon.h>
#include <xkbcommon/xkbcommon-compose.h>

#include "xdg-shell-client-protocol.h"

#include <errno.h>
#include <fcntl.h>
#include <linux/input-event-codes.h>
#include <locale.h>
#include <poll.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <sys/eventfd.h>
#include <sys/mman.h>
#include <unistd.h>

#define BUFFER_COUNT 2
#define WHEEL_NOTCH 120
#define DEFAULT_DPI 96
#define CURSOR_SIZE 24
#define AXIS_PER_NOTCH 10.0
#define VALUE120_PER_NOTCH 120.0
#define CLIPBOARD_TIMEOUT_MS 200
#define CLIPBOARD_MAX 1048576
#define MIME_TEXT "text/plain;charset=utf-8"
#define COMPOSITOR_VERSION 4
#define SEAT_VERSION 5
#define OUTPUT_VERSION 2
#define POINTER_AXIS_VERSION 5

typedef struct WLBuffer
{
    struct wl_buffer* buffer;
    unsigned char* data;
    size_t offset;
    int width;
    int height;
    int busy;
} WLBuffer;

typedef struct WLWindow WLWindow;

struct WLWindow
{
    NativeWindow base;

    struct wl_display* display;
    struct wl_registry* registry;
    struct wl_compositor* compositor;
    uint32_t compositorVersion;
    struct wl_shm* shm;
    struct wl_seat* seat;
    struct xdg_wm_base* shell;
    struct wl_data_device_manager* dataManager;
    struct wl_data_device* dataDevice;
    struct wl_output* output;

    struct wl_surface* surface;
    struct xdg_surface* shellSurface;
    struct xdg_toplevel* toplevel;

    struct wl_pointer* pointer;
    struct wl_keyboard* keyboard;

    struct xkb_context* xkb;
    struct xkb_keymap* keymap;
    struct xkb_state* state;
    struct xkb_compose_table* composeTable;
    struct xkb_compose_state* compose;

    struct wl_cursor_theme* cursorTheme;
    struct wl_surface* cursorSurface;
    WLBuffer cursorImage;
    int cursorHotspotX;
    int cursorHotspotY;
    int cursorKind;
    int cursorOwned;

    struct wl_shm_pool* pool;
    unsigned char* poolData;
    size_t poolSize;
    WLBuffer buffers[BUFFER_COUNT];

    void* pixels;
    int pixelsWidth;
    int pixelsHeight;
    int pixelsRowBytes;

    int width;
    int height;
    int bufferWidth;
    int bufferHeight;
    int scale;

    int configured;
    int closed;
    int dirty;
    int activated;
    int wake;

    int pointerX;
    int pointerY;
    unsigned int enterSerial;
    unsigned int inputSerial;
    int modifiers;

    double axisX;
    double axisY;
    int notchX;
    int notchY;
    int axisSeen;

    uint32_t repeatKey;
    long long repeatDue;
    int repeatRate;
    int repeatDelay;

    char* clipboardText;
    char* incomingText;
    struct wl_data_offer* offer;
    struct wl_data_offer* pending;
    int pendingIsText;
    int offerIsText;
    struct wl_data_source* source;

    void (*paintCallBack)(int, int);
    void (*pointerCallBack)(int, int, int, int);
    void (*keyCallBack)(int, int, int, int);
    void (*textCallBack)(const char*);
    void (*wheelCallBack)(int, int, int, int, int);

    AtspiBridge* accessibility;
};

static void ReleasePool(WLWindow* window)
{
    for (int index = 0; index < BUFFER_COUNT; index++)
    {
        if (window->buffers[index].buffer != NULL)
        {
            wl_buffer_destroy(window->buffers[index].buffer);
        }

        window->buffers[index].buffer = NULL;
        window->buffers[index].data = NULL;
        window->buffers[index].busy = 0;
    }

    if (window->pool != NULL)
    {
        wl_shm_pool_destroy(window->pool);
        window->pool = NULL;
    }

    if (window->poolData != NULL)
    {
        munmap(window->poolData, window->poolSize);
        window->poolData = NULL;
        window->poolSize = 0;
    }
}

static int SharedFile(size_t size)
{
    int file = memfd_create("ixen", MFD_CLOEXEC);

    if (file < 0)
    {
        return -1;
    }

    if (ftruncate(file, (off_t)size) < 0)
    {
        close(file);
        return -1;
    }

    return file;
}

static void OnBufferRelease(void* data, struct wl_buffer* buffer)
{
    (void)buffer;

    WLBuffer* slot = data;

    slot->busy = 0;
}

static const struct wl_buffer_listener BUFFER_LISTENER =
{
    .release = OnBufferRelease
};

static int EnsurePool(WLWindow* window, int width, int height)
{
    size_t stride = (size_t)width * 4;
    size_t frame = stride * (size_t)height;
    size_t size = frame * BUFFER_COUNT;

    if (window->poolData != NULL && window->bufferWidth == width && window->bufferHeight == height)
    {
        return 1;
    }

    ReleasePool(window);

    int file = SharedFile(size);

    if (file < 0)
    {
        return 0;
    }

    window->poolData = mmap(NULL, size, PROT_READ | PROT_WRITE, MAP_SHARED, file, 0);

    if (window->poolData == MAP_FAILED)
    {
        window->poolData = NULL;
        close(file);
        return 0;
    }

    window->poolSize = size;
    window->pool = wl_shm_create_pool(window->shm, file, (int32_t)size);

    close(file);

    if (window->pool == NULL)
    {
        ReleasePool(window);
        return 0;
    }

    for (int index = 0; index < BUFFER_COUNT; index++)
    {
        size_t offset = frame * (size_t)index;

        window->buffers[index].buffer = wl_shm_pool_create_buffer(window->pool, (int32_t)offset,
            width, height, (int32_t)stride, WL_SHM_FORMAT_XRGB8888);

        if (window->buffers[index].buffer == NULL)
        {
            ReleasePool(window);
            return 0;
        }

        window->buffers[index].data = window->poolData + offset;
        window->buffers[index].offset = offset;
        window->buffers[index].busy = 0;

        wl_buffer_add_listener(window->buffers[index].buffer, &BUFFER_LISTENER,
            &window->buffers[index]);
    }

    window->bufferWidth = width;
    window->bufferHeight = height;

    return 1;
}

static void Damage(WLWindow* window, struct wl_surface* surface, int width, int height)
{
    if (window->compositorVersion >= 4)
    {
        wl_surface_damage_buffer(surface, 0, 0, width, height);
    }
    else
    {
        wl_surface_damage(surface, 0, 0, width, height);
    }
}

static void Scale(WLWindow* window, struct wl_surface* surface)
{
    if (window->compositorVersion >= 3)
    {
        wl_surface_set_buffer_scale(surface, window->scale);
    }
}

static WLBuffer* FreeBuffer(WLWindow* window)
{
    for (int index = 0; index < BUFFER_COUNT; index++)
    {
        if (window->buffers[index].buffer != NULL && !window->buffers[index].busy)
        {
            return &window->buffers[index];
        }
    }

    return NULL;
}

static int Paint(WLWindow* window)
{
    if (window->paintCallBack == NULL || !window->configured)
    {
        return 1;
    }

    int width = window->width * window->scale;
    int height = window->height * window->scale;

    if (width <= 0 || height <= 0)
    {
        return 1;
    }

    if (!EnsurePool(window, width, height))
    {
        return 1;
    }

    WLBuffer* slot = FreeBuffer(window);

    if (slot == NULL)
    {
        return 0;
    }

    window->paintCallBack(width, height);

    if (window->pixels == NULL)
    {
        return 1;
    }

    int rows = window->pixelsHeight < height ? window->pixelsHeight : height;
    int columns = window->pixelsWidth < width ? window->pixelsWidth : width;
    size_t stride = (size_t)width * 4;
    size_t span = (size_t)columns * 4;

    for (int row = 0; row < rows; row++)
    {
        memcpy(slot->data + stride * (size_t)row,
            (unsigned char*)window->pixels + (size_t)window->pixelsRowBytes * (size_t)row, span);
    }

    slot->busy = 1;

    Scale(window, window->surface);
    wl_surface_attach(window->surface, slot->buffer, 0, 0);
    Damage(window, window->surface, width, height);
    wl_surface_commit(window->surface);

    return 1;
}

static void ApplyCursor(WLWindow* window)
{
    if (window->pointer == NULL || window->enterSerial == 0)
    {
        return;
    }

    if (window->cursorSurface == NULL)
    {
        window->cursorSurface = wl_compositor_create_surface(window->compositor);

        if (window->cursorSurface == NULL)
        {
            return;
        }
    }

    int kind = window->cursorKind;

    if (kind < 0 || kind >= CURSOR_COUNT)
    {
        kind = IXEN_CURSOR_DEFAULT;
    }

    if (CURSOR_NAMES[kind] == NULL)
    {
        wl_pointer_set_cursor(window->pointer, window->enterSerial, NULL, 0, 0);
        return;
    }

    if (window->cursorOwned && window->cursorImage.buffer != NULL)
    {
        wl_surface_attach(window->cursorSurface, window->cursorImage.buffer, 0, 0);
        Damage(window, window->cursorSurface, window->cursorImage.width,
            window->cursorImage.height);
        wl_surface_commit(window->cursorSurface);
        wl_pointer_set_cursor(window->pointer, window->enterSerial, window->cursorSurface,
            window->cursorHotspotX, window->cursorHotspotY);

        return;
    }

    if (window->cursorTheme == NULL)
    {
        window->cursorTheme = wl_cursor_theme_load(NULL, CURSOR_SIZE * window->scale, window->shm);
    }

    if (window->cursorTheme == NULL)
    {
        return;
    }

    struct wl_cursor* cursor = wl_cursor_theme_get_cursor(window->cursorTheme, CURSOR_NAMES[kind]);

    if (cursor == NULL || cursor->image_count < 1)
    {
        cursor = wl_cursor_theme_get_cursor(window->cursorTheme, CURSOR_NAMES[0]);
    }

    if (cursor == NULL || cursor->image_count < 1)
    {
        return;
    }

    struct wl_cursor_image* image = cursor->images[0];
    struct wl_buffer* buffer = wl_cursor_image_get_buffer(image);

    if (buffer == NULL)
    {
        return;
    }

    Scale(window, window->cursorSurface);
    wl_surface_attach(window->cursorSurface, buffer, 0, 0);
    Damage(window, window->cursorSurface, (int)image->width, (int)image->height);
    wl_surface_commit(window->cursorSurface);

    wl_pointer_set_cursor(window->pointer, window->enterSerial, window->cursorSurface,
        (int32_t)image->hotspot_x / window->scale, (int32_t)image->hotspot_y / window->scale);
}

static void ReleaseCursorImage(WLWindow* window)
{
    if (window->cursorImage.buffer != NULL)
    {
        wl_buffer_destroy(window->cursorImage.buffer);
        window->cursorImage.buffer = NULL;
    }

    if (window->cursorImage.data != NULL)
    {
        munmap(window->cursorImage.data, window->cursorImage.offset);
        window->cursorImage.data = NULL;
        window->cursorImage.offset = 0;
    }

    window->cursorOwned = 0;
}

static void SendPointer(WLWindow* window, int kind, int x, int y, int button)
{
    if (window->pointerCallBack != NULL)
    {
        window->pointerCallBack(kind, x, y, button);
    }
}

static int ButtonOf(uint32_t button)
{
    switch (button)
    {
        case BTN_LEFT: return IXEN_BUTTON_LEFT;
        case BTN_MIDDLE: return IXEN_BUTTON_MIDDLE;
        case BTN_RIGHT: return IXEN_BUTTON_RIGHT;
        default: return IXEN_BUTTON_NONE;
    }
}

static int IsNamed(xkb_keysym_t symbol)
{
    return (symbol >= 0x30 && symbol <= 0x39)
        || (symbol >= 0x41 && symbol <= 0x5A)
        || (symbol >= 0x61 && symbol <= 0x7A)
        || symbol == 0x20
        || (symbol >= 0xFE00 && symbol <= 0xFFFF);
}

static xkb_keysym_t SymbolAt(WLWindow* window, xkb_keycode_t code, int level)
{
    const xkb_keysym_t* symbols = NULL;
    xkb_layout_index_t layout = xkb_state_key_get_layout(window->state, code);

    int count = xkb_keymap_key_get_syms_by_level(window->keymap, code, layout,
        (xkb_level_index_t)level, &symbols);

    return count > 0 ? symbols[0] : XKB_KEY_NoSymbol;
}

static int KeySymOf(WLWindow* window, xkb_keycode_t code)
{
    if (window->state == NULL || window->keymap == NULL)
    {
        return 0;
    }

    xkb_keysym_t symbol = SymbolAt(window, code, 0);

    if (IsNamed(symbol))
    {
        return (int)symbol;
    }

    xkb_keysym_t shifted = SymbolAt(window, code, 1);

    if (shifted >= 0x30 && shifted <= 0x39)
    {
        return (int)shifted;
    }

    return symbol > 0xFFFF ? 0 : (int)symbol;
}

static void SendKey(WLWindow* window, xkb_keycode_t code, int kind, int repeat)
{
    if (window->keyCallBack != NULL)
    {
        window->keyCallBack(kind, KeySymOf(window, code), window->modifiers, repeat);
    }
}

static void SendText(WLWindow* window, xkb_keycode_t code)
{
    if (window->textCallBack == NULL || window->state == NULL)
    {
        return;
    }

    char buffer[64];
    int length = 0;

    if (window->compose != NULL)
    {
        xkb_keysym_t symbol = xkb_state_key_get_one_sym(window->state, code);

        if (xkb_compose_state_feed(window->compose, symbol) == XKB_COMPOSE_FEED_ACCEPTED)
        {
            enum xkb_compose_status status = xkb_compose_state_get_status(window->compose);

            if (status == XKB_COMPOSE_COMPOSING || status == XKB_COMPOSE_CANCELLED)
            {
                return;
            }

            if (status == XKB_COMPOSE_COMPOSED)
            {
                length = xkb_compose_state_get_utf8(window->compose, buffer, sizeof(buffer));
            }
        }
    }

    if (length == 0)
    {
        length = xkb_state_key_get_utf8(window->state, code, buffer, sizeof(buffer));
    }

    if (length <= 0 || (size_t)length >= sizeof(buffer))
    {
        return;
    }

    buffer[length] = 0;

    window->textCallBack(buffer);
}

static void OnPointerEnter(void* data, struct wl_pointer* pointer, uint32_t serial,
    struct wl_surface* surface, wl_fixed_t x, wl_fixed_t y)
{
    (void)pointer;
    (void)surface;

    WLWindow* window = data;

    window->enterSerial = serial;
    window->inputSerial = serial;
    window->pointerX = wl_fixed_to_int(x);
    window->pointerY = wl_fixed_to_int(y);

    ApplyCursor(window);

    SendPointer(window, IXEN_POINTER_MOVE, window->pointerX, window->pointerY, IXEN_BUTTON_NONE);
}

static void OnPointerLeave(void* data, struct wl_pointer* pointer, uint32_t serial,
    struct wl_surface* surface)
{
    (void)pointer;
    (void)surface;

    WLWindow* window = data;

    window->inputSerial = serial;

    SendPointer(window, IXEN_POINTER_LEAVE, window->pointerX, window->pointerY, IXEN_BUTTON_NONE);
}

static void OnPointerMotion(void* data, struct wl_pointer* pointer, uint32_t time,
    wl_fixed_t x, wl_fixed_t y)
{
    (void)pointer;
    (void)time;

    WLWindow* window = data;

    window->pointerX = wl_fixed_to_int(x);
    window->pointerY = wl_fixed_to_int(y);

    SendPointer(window, IXEN_POINTER_MOVE, window->pointerX, window->pointerY, IXEN_BUTTON_NONE);
}

static void OnPointerButton(void* data, struct wl_pointer* pointer, uint32_t serial,
    uint32_t time, uint32_t button, uint32_t state)
{
    (void)pointer;
    (void)time;

    WLWindow* window = data;

    window->inputSerial = serial;

    int kind = state == WL_POINTER_BUTTON_STATE_PRESSED ? IXEN_POINTER_DOWN : IXEN_POINTER_UP;

    SendPointer(window, kind, window->pointerX, window->pointerY, ButtonOf(button));
}

static void OnPointerAxis(void* data, struct wl_pointer* pointer, uint32_t time,
    uint32_t axis, wl_fixed_t value)
{
    (void)pointer;
    (void)time;

    WLWindow* window = data;
    double amount = wl_fixed_to_double(value);

    if (axis == WL_POINTER_AXIS_VERTICAL_SCROLL)
    {
        window->axisY += amount;
    }
    else
    {
        window->axisX += amount;
    }
}

static void OnPointerAxisDiscrete(void* data, struct wl_pointer* pointer, uint32_t axis,
    int32_t discrete)
{
    (void)pointer;

    WLWindow* window = data;

    window->axisSeen = 1;

    if (axis == WL_POINTER_AXIS_VERTICAL_SCROLL)
    {
        window->notchY += discrete;
    }
    else
    {
        window->notchX += discrete;
    }
}

static void OnPointerFrame(void* data, struct wl_pointer* pointer)
{
    (void)pointer;

    WLWindow* window = data;

    int deltaX = 0;
    int deltaY = 0;

    if (window->axisSeen)
    {
        deltaX = window->notchX * WHEEL_NOTCH;
        deltaY = -window->notchY * WHEEL_NOTCH;
    }
    else
    {
        int steps = (int)(window->axisY / AXIS_PER_NOTCH);
        int sideways = (int)(window->axisX / AXIS_PER_NOTCH);

        deltaY = -steps * WHEEL_NOTCH;
        deltaX = sideways * WHEEL_NOTCH;

        window->axisY -= steps * AXIS_PER_NOTCH;
        window->axisX -= sideways * AXIS_PER_NOTCH;
    }

    window->axisSeen = 0;
    window->notchX = 0;
    window->notchY = 0;

    if ((deltaX != 0 || deltaY != 0) && window->wheelCallBack != NULL)
    {
        window->wheelCallBack(window->pointerX, window->pointerY, deltaX, deltaY,
            window->modifiers);
    }
}

static void OnPointerAxisSource(void* data, struct wl_pointer* pointer, uint32_t source)
{
    (void)data;
    (void)pointer;
    (void)source;
}

static void OnPointerAxisStop(void* data, struct wl_pointer* pointer, uint32_t time,
    uint32_t axis)
{
    (void)data;
    (void)pointer;
    (void)time;
    (void)axis;
}

static void OnPointerAxisValue120(void* data, struct wl_pointer* pointer, uint32_t axis,
    int32_t value120)
{
    (void)pointer;

    WLWindow* window = data;

    window->axisSeen = 1;

    if (axis == WL_POINTER_AXIS_VERTICAL_SCROLL)
    {
        window->notchY += (int)(value120 / VALUE120_PER_NOTCH);
    }
    else
    {
        window->notchX += (int)(value120 / VALUE120_PER_NOTCH);
    }
}

static void OnPointerAxisDirection(void* data, struct wl_pointer* pointer, uint32_t axis,
    uint32_t direction)
{
    (void)data;
    (void)pointer;
    (void)axis;
    (void)direction;
}

static const struct wl_pointer_listener POINTER_LISTENER =
{
    .enter = OnPointerEnter,
    .leave = OnPointerLeave,
    .motion = OnPointerMotion,
    .button = OnPointerButton,
    .axis = OnPointerAxis,
    .frame = OnPointerFrame,
    .axis_source = OnPointerAxisSource,
    .axis_stop = OnPointerAxisStop,
    .axis_discrete = OnPointerAxisDiscrete,
    .axis_value120 = OnPointerAxisValue120,
    .axis_relative_direction = OnPointerAxisDirection
};

static void OnKeymap(void* data, struct wl_keyboard* keyboard, uint32_t format, int fd,
    uint32_t size)
{
    (void)keyboard;

    WLWindow* window = data;

    if (format != WL_KEYBOARD_KEYMAP_FORMAT_XKB_V1)
    {
        close(fd);
        return;
    }

    char* text = mmap(NULL, size, PROT_READ, MAP_PRIVATE, fd, 0);

    close(fd);

    if (text == MAP_FAILED)
    {
        return;
    }

    struct xkb_keymap* keymap = xkb_keymap_new_from_string(window->xkb, text,
        XKB_KEYMAP_FORMAT_TEXT_V1, XKB_KEYMAP_COMPILE_NO_FLAGS);

    munmap(text, size);

    if (keymap == NULL)
    {
        return;
    }

    struct xkb_state* state = xkb_state_new(keymap);

    if (state == NULL)
    {
        xkb_keymap_unref(keymap);
        return;
    }

    if (window->state != NULL)
    {
        xkb_state_unref(window->state);
    }

    if (window->keymap != NULL)
    {
        xkb_keymap_unref(window->keymap);
    }

    window->keymap = keymap;
    window->state = state;
}

static void OnKeyboardEnter(void* data, struct wl_keyboard* keyboard, uint32_t serial,
    struct wl_surface* surface, struct wl_array* keys)
{
    (void)keyboard;
    (void)surface;
    (void)keys;

    WLWindow* window = data;

    window->inputSerial = serial;
}

static void OnKeyboardLeave(void* data, struct wl_keyboard* keyboard, uint32_t serial,
    struct wl_surface* surface)
{
    (void)keyboard;
    (void)surface;

    WLWindow* window = data;

    window->inputSerial = serial;
    window->repeatKey = 0;
}

static void OnKey(void* data, struct wl_keyboard* keyboard, uint32_t serial, uint32_t time,
    uint32_t key, uint32_t state)
{
    (void)keyboard;
    (void)time;

    WLWindow* window = data;
    xkb_keycode_t code = key + 8;

    window->inputSerial = serial;

    if (state == WL_KEYBOARD_KEY_STATE_RELEASED)
    {
        if (window->repeatKey == code)
        {
            window->repeatKey = 0;
        }

        SendKey(window, code, IXEN_KEY_UP, 0);

        return;
    }

    SendKey(window, code, IXEN_KEY_DOWN, 0);
    SendText(window, code);

    if (window->keymap != NULL && xkb_keymap_key_repeats(window->keymap, code)
        && window->repeatRate > 0)
    {
        window->repeatKey = code;
        window->repeatDue = NT_Now() + window->repeatDelay;
    }
}

static void OnModifiers(void* data, struct wl_keyboard* keyboard, uint32_t serial,
    uint32_t depressed, uint32_t latched, uint32_t locked, uint32_t group)
{
    (void)keyboard;

    WLWindow* window = data;

    window->inputSerial = serial;

    if (window->state == NULL)
    {
        return;
    }

    xkb_state_update_mask(window->state, depressed, latched, locked, 0, 0, group);

    int result = 0;

    if (xkb_state_mod_name_is_active(window->state, XKB_MOD_NAME_SHIFT, XKB_STATE_MODS_EFFECTIVE))
    {
        result |= IXEN_MOD_SHIFT;
    }

    if (xkb_state_mod_name_is_active(window->state, XKB_MOD_NAME_CTRL, XKB_STATE_MODS_EFFECTIVE))
    {
        result |= IXEN_MOD_CONTROL;
    }

    if (xkb_state_mod_name_is_active(window->state, XKB_MOD_NAME_ALT, XKB_STATE_MODS_EFFECTIVE))
    {
        result |= IXEN_MOD_ALT;
    }

    if (xkb_state_mod_name_is_active(window->state, XKB_MOD_NAME_LOGO, XKB_STATE_MODS_EFFECTIVE))
    {
        result |= IXEN_MOD_META;
    }

    window->modifiers = result;
}

static void OnRepeatInfo(void* data, struct wl_keyboard* keyboard, int32_t rate, int32_t delay)
{
    (void)keyboard;

    WLWindow* window = data;

    window->repeatRate = rate;
    window->repeatDelay = delay;
}

static const struct wl_keyboard_listener KEYBOARD_LISTENER =
{
    .keymap = OnKeymap,
    .enter = OnKeyboardEnter,
    .leave = OnKeyboardLeave,
    .key = OnKey,
    .modifiers = OnModifiers,
    .repeat_info = OnRepeatInfo
};

static void OnSeatCapabilities(void* data, struct wl_seat* seat, uint32_t capabilities)
{
    WLWindow* window = data;

    if ((capabilities & WL_SEAT_CAPABILITY_POINTER) != 0 && window->pointer == NULL)
    {
        window->pointer = wl_seat_get_pointer(seat);
        wl_pointer_add_listener(window->pointer, &POINTER_LISTENER, window);
    }

    if ((capabilities & WL_SEAT_CAPABILITY_KEYBOARD) != 0 && window->keyboard == NULL)
    {
        window->keyboard = wl_seat_get_keyboard(seat);
        wl_keyboard_add_listener(window->keyboard, &KEYBOARD_LISTENER, window);
    }
}

static void OnSeatName(void* data, struct wl_seat* seat, const char* name)
{
    (void)data;
    (void)seat;
    (void)name;
}

static const struct wl_seat_listener SEAT_LISTENER =
{
    .capabilities = OnSeatCapabilities,
    .name = OnSeatName
};

static void OnOutputGeometry(void* data, struct wl_output* output, int32_t x, int32_t y,
    int32_t physicalWidth, int32_t physicalHeight, int32_t subpixel, const char* make,
    const char* model, int32_t transform)
{
    (void)data;
    (void)output;
    (void)x;
    (void)y;
    (void)physicalWidth;
    (void)physicalHeight;
    (void)subpixel;
    (void)make;
    (void)model;
    (void)transform;
}

static void OnOutputMode(void* data, struct wl_output* output, uint32_t flags, int32_t width,
    int32_t height, int32_t refresh)
{
    (void)data;
    (void)output;
    (void)flags;
    (void)width;
    (void)height;
    (void)refresh;
}

static void OnOutputDone(void* data, struct wl_output* output)
{
    (void)data;
    (void)output;
}

static void OnOutputScale(void* data, struct wl_output* output, int32_t factor)
{
    (void)output;

    WLWindow* window = data;

    if (factor > 0 && factor != window->scale)
    {
        window->scale = factor;
        window->dirty = 1;

        if (window->cursorTheme != NULL)
        {
            wl_cursor_theme_destroy(window->cursorTheme);
            window->cursorTheme = NULL;
        }
    }
}

static const struct wl_output_listener OUTPUT_LISTENER =
{
    .geometry = OnOutputGeometry,
    .mode = OnOutputMode,
    .done = OnOutputDone,
    .scale = OnOutputScale
};

static void OnShellPing(void* data, struct xdg_wm_base* shell, uint32_t serial)
{
    (void)data;

    xdg_wm_base_pong(shell, serial);
}

static const struct xdg_wm_base_listener SHELL_LISTENER = { .ping = OnShellPing };

static void OnSurfaceConfigure(void* data, struct xdg_surface* surface, uint32_t serial)
{
    WLWindow* window = data;

    xdg_surface_ack_configure(surface, serial);

    window->configured = 1;
    window->dirty = 1;
}

static const struct xdg_surface_listener SURFACE_LISTENER = { .configure = OnSurfaceConfigure };

static void OnToplevelConfigure(void* data, struct xdg_toplevel* toplevel, int32_t width,
    int32_t height, struct wl_array* states)
{
    (void)toplevel;

    WLWindow* window = data;

    window->activated = 0;

    uint32_t* state;

    wl_array_for_each(state, states)
    {
        if (*state == XDG_TOPLEVEL_STATE_ACTIVATED)
        {
            window->activated = 1;
        }
    }

    if (width > 0 && height > 0 && (width != window->width || height != window->height))
    {
        window->width = width;
        window->height = height;
        window->dirty = 1;
    }
}

static void OnToplevelClose(void* data, struct xdg_toplevel* toplevel)
{
    (void)toplevel;

    WLWindow* window = data;

    window->closed = 1;
}

static const struct xdg_toplevel_listener TOPLEVEL_LISTENER =
{
    .configure = OnToplevelConfigure,
    .close = OnToplevelClose
};

static void OnOfferMime(void* data, struct wl_data_offer* offer, const char* mime)
{
    (void)offer;

    WLWindow* window = data;

    if (strcmp(mime, MIME_TEXT) == 0 || strcmp(mime, "text/plain") == 0
        || strcmp(mime, "UTF8_STRING") == 0)
    {
        window->pendingIsText = 1;
    }
}

static void OnOfferSourceActions(void* data, struct wl_data_offer* offer, uint32_t actions)
{
    (void)data;
    (void)offer;
    (void)actions;
}

static void OnOfferAction(void* data, struct wl_data_offer* offer, uint32_t action)
{
    (void)data;
    (void)offer;
    (void)action;
}

static const struct wl_data_offer_listener OFFER_LISTENER =
{
    .offer = OnOfferMime,
    .source_actions = OnOfferSourceActions,
    .action = OnOfferAction
};

static void OnDataOffer(void* data, struct wl_data_device* device, struct wl_data_offer* offer)
{
    (void)device;

    WLWindow* window = data;

    window->pending = offer;
    window->pendingIsText = 0;

    wl_data_offer_add_listener(offer, &OFFER_LISTENER, window);
}

static void OnSelection(void* data, struct wl_data_device* device, struct wl_data_offer* offer)
{
    (void)device;

    WLWindow* window = data;

    if (window->offer != NULL && window->offer != offer)
    {
        wl_data_offer_destroy(window->offer);
    }

    window->offer = offer;
    window->offerIsText = offer != NULL && offer == window->pending ? window->pendingIsText : 0;
}

static void OnDataEnter(void* data, struct wl_data_device* device, uint32_t serial,
    struct wl_surface* surface, wl_fixed_t x, wl_fixed_t y, struct wl_data_offer* offer)
{
    (void)data;
    (void)device;
    (void)serial;
    (void)surface;
    (void)x;
    (void)y;
    (void)offer;
}

static void OnDataLeave(void* data, struct wl_data_device* device)
{
    (void)data;
    (void)device;
}

static void OnDataMotion(void* data, struct wl_data_device* device, uint32_t time,
    wl_fixed_t x, wl_fixed_t y)
{
    (void)data;
    (void)device;
    (void)time;
    (void)x;
    (void)y;
}

static void OnDataDrop(void* data, struct wl_data_device* device)
{
    (void)data;
    (void)device;
}

static const struct wl_data_device_listener DEVICE_LISTENER =
{
    .data_offer = OnDataOffer,
    .enter = OnDataEnter,
    .leave = OnDataLeave,
    .motion = OnDataMotion,
    .drop = OnDataDrop,
    .selection = OnSelection
};

static void OnSourceTarget(void* data, struct wl_data_source* source, const char* mime)
{
    (void)data;
    (void)source;
    (void)mime;
}

static void OnSourceSend(void* data, struct wl_data_source* source, const char* mime, int fd)
{
    (void)source;
    (void)mime;

    WLWindow* window = data;

    if (window->clipboardText != NULL)
    {
        size_t remaining = strlen(window->clipboardText);
        const char* at = window->clipboardText;

        while (remaining > 0)
        {
            ssize_t written = write(fd, at, remaining);

            if (written <= 0)
            {
                break;
            }

            at += written;
            remaining -= (size_t)written;
        }
    }

    close(fd);
}

static void OnSourceCancelled(void* data, struct wl_data_source* source)
{
    WLWindow* window = data;

    if (window->source == source)
    {
        window->source = NULL;
    }

    wl_data_source_destroy(source);
}

static const struct wl_data_source_listener SOURCE_LISTENER =
{
    .target = OnSourceTarget,
    .send = OnSourceSend,
    .cancelled = OnSourceCancelled
};

static uint32_t Pick(uint32_t offered, uint32_t wanted)
{
    return offered < wanted ? offered : wanted;
}

static void OnGlobal(void* data, struct wl_registry* registry, uint32_t name,
    const char* interface, uint32_t version)
{
    WLWindow* window = data;

    if (strcmp(interface, wl_compositor_interface.name) == 0)
    {
        window->compositorVersion = Pick(version, COMPOSITOR_VERSION);
        window->compositor = wl_registry_bind(registry, name, &wl_compositor_interface,
            window->compositorVersion);
    }
    else if (strcmp(interface, wl_shm_interface.name) == 0)
    {
        window->shm = wl_registry_bind(registry, name, &wl_shm_interface, 1);
    }
    else if (strcmp(interface, xdg_wm_base_interface.name) == 0)
    {
        window->shell = wl_registry_bind(registry, name, &xdg_wm_base_interface, 1);
        xdg_wm_base_add_listener(window->shell, &SHELL_LISTENER, window);
    }
    else if (strcmp(interface, wl_seat_interface.name) == 0 && window->seat == NULL)
    {
        window->seat = wl_registry_bind(registry, name, &wl_seat_interface,
            Pick(version, SEAT_VERSION));
        wl_seat_add_listener(window->seat, &SEAT_LISTENER, window);
    }
    else if (strcmp(interface, wl_output_interface.name) == 0 && window->output == NULL)
    {
        window->output = wl_registry_bind(registry, name, &wl_output_interface,
            Pick(version, OUTPUT_VERSION));
        wl_output_add_listener(window->output, &OUTPUT_LISTENER, window);
    }
    else if (strcmp(interface, wl_data_device_manager_interface.name) == 0)
    {
        window->dataManager = wl_registry_bind(registry, name, &wl_data_device_manager_interface,
            Pick(version, 3));
    }
}

static void OnGlobalGone(void* data, struct wl_registry* registry, uint32_t name)
{
    (void)data;
    (void)registry;
    (void)name;
}

static const struct wl_registry_listener REGISTRY_LISTENER =
{
    .global = OnGlobal,
    .global_remove = OnGlobalGone
};

static void ReleaseWayland(WLWindow* window)
{
    ReleaseCursorImage(window);
    ReleasePool(window);

    if (window->cursorTheme != NULL)
    {
        wl_cursor_theme_destroy(window->cursorTheme);
    }

    if (window->cursorSurface != NULL)
    {
        wl_surface_destroy(window->cursorSurface);
    }

    if (window->source != NULL)
    {
        wl_data_source_destroy(window->source);
    }

    if (window->offer != NULL)
    {
        wl_data_offer_destroy(window->offer);
    }

    if (window->dataDevice != NULL)
    {
        wl_data_device_destroy(window->dataDevice);
    }

    if (window->compose != NULL)
    {
        xkb_compose_state_unref(window->compose);
    }

    if (window->composeTable != NULL)
    {
        xkb_compose_table_unref(window->composeTable);
    }

    if (window->state != NULL)
    {
        xkb_state_unref(window->state);
    }

    if (window->keymap != NULL)
    {
        xkb_keymap_unref(window->keymap);
    }

    if (window->xkb != NULL)
    {
        xkb_context_unref(window->xkb);
    }

    if (window->pointer != NULL)
    {
        wl_pointer_destroy(window->pointer);
    }

    if (window->keyboard != NULL)
    {
        wl_keyboard_destroy(window->keyboard);
    }

    if (window->toplevel != NULL)
    {
        xdg_toplevel_destroy(window->toplevel);
    }

    if (window->shellSurface != NULL)
    {
        xdg_surface_destroy(window->shellSurface);
    }

    if (window->surface != NULL)
    {
        wl_surface_destroy(window->surface);
    }

    if (window->display != NULL)
    {
        wl_display_disconnect(window->display);
    }

    if (window->wake >= 0)
    {
        close(window->wake);
    }

    free(window->clipboardText);
    free(window->incomingText);
}

NativeWindow* WL_Create(const char* title, int width, int height)
{
    struct wl_display* display = wl_display_connect(NULL);

    if (display == NULL)
    {
        return NULL;
    }

    WLWindow* window = calloc(1, sizeof(WLWindow));

    if (window == NULL)
    {
        wl_display_disconnect(display);
        return NULL;
    }

    window->base.backend = &WL_Backend;
    window->display = display;
    window->width = width > 0 ? width : 1;
    window->height = height > 0 ? height : 1;
    window->scale = 1;
    window->cursorKind = 0;
    window->repeatRate = 0;
    window->repeatDelay = 0;
    window->wake = eventfd(0, EFD_CLOEXEC | EFD_NONBLOCK);

    window->registry = wl_display_get_registry(display);
    wl_registry_add_listener(window->registry, &REGISTRY_LISTENER, window);

    wl_display_roundtrip(display);
    wl_display_roundtrip(display);

    if (window->compositor == NULL || window->shm == NULL || window->shell == NULL)
    {
        fprintf(stderr, "Ixen: this Wayland compositor offers no wl_compositor, wl_shm or "
            "xdg_wm_base, so the X11 backend is used instead.\n");
        ReleaseWayland(window);
        free(window);

        return NULL;
    }

    setlocale(LC_CTYPE, "");

    window->xkb = xkb_context_new(XKB_CONTEXT_NO_FLAGS);

    if (window->xkb != NULL)
    {
        const char* locale = setlocale(LC_CTYPE, NULL);

        window->composeTable = xkb_compose_table_new_from_locale(window->xkb,
            locale == NULL ? "C" : locale, XKB_COMPOSE_COMPILE_NO_FLAGS);

        if (window->composeTable != NULL)
        {
            window->compose = xkb_compose_state_new(window->composeTable,
                XKB_COMPOSE_STATE_NO_FLAGS);
        }
    }

    if (window->dataManager != NULL && window->seat != NULL)
    {
        window->dataDevice = wl_data_device_manager_get_data_device(window->dataManager,
            window->seat);

        if (window->dataDevice != NULL)
        {
            wl_data_device_add_listener(window->dataDevice, &DEVICE_LISTENER, window);
        }
    }

    window->surface = wl_compositor_create_surface(window->compositor);

    if (window->surface == NULL)
    {
        ReleaseWayland(window);
        free(window);

        return NULL;
    }

    window->shellSurface = xdg_wm_base_get_xdg_surface(window->shell, window->surface);
    xdg_surface_add_listener(window->shellSurface, &SURFACE_LISTENER, window);

    window->toplevel = xdg_surface_get_toplevel(window->shellSurface);
    xdg_toplevel_add_listener(window->toplevel, &TOPLEVEL_LISTENER, window);

    xdg_toplevel_set_title(window->toplevel, title == NULL ? "" : title);
    xdg_toplevel_set_app_id(window->toplevel, "ixen");

    wl_surface_commit(window->surface);
    wl_display_roundtrip(display);

    window->accessibility = Atspi_Create();

    Atspi_SetTitle(window->accessibility, title);
    Atspi_SetOrigin(window->accessibility, 0, 0);

    window->dirty = 1;

    return &window->base;
}

static void Drain(WLWindow* window)
{
    uint64_t count = 0;

    while (read(window->wake, &count, sizeof(count)) == sizeof(count))
    {
    }
}

static int RepeatWait(WLWindow* window)
{
    if (window->repeatKey == 0 || window->repeatRate <= 0)
    {
        return -1;
    }

    long long wait = window->repeatDue - NT_Now();

    return wait <= 0 ? 0 : (int)wait;
}

static void FireRepeat(WLWindow* window)
{
    if (window->repeatKey == 0 || window->repeatRate <= 0 || NT_Now() < window->repeatDue)
    {
        return;
    }

    SendKey(window, window->repeatKey, IXEN_KEY_DOWN, 1);
    SendText(window, window->repeatKey);

    window->repeatDue = NT_Now() + 1000 / window->repeatRate;
}

static int WL_Run(NativeWindow* handle)
{
    WLWindow* window = (WLWindow*)handle;

    if (window == NULL)
    {
        return 0;
    }

    struct pollfd watched[2 + ATSPI_MAX_FDS];
    int painted = 1;

    while (!window->closed)
    {
        NT_Fire(&window->closed);
        FireRepeat(window);
        Atspi_Pump(window->accessibility);

        if (window->dirty)
        {
            window->dirty = 0;
            painted = Paint(window);

            if (!painted)
            {
                window->dirty = 1;
            }
        }

        if (window->closed)
        {
            break;
        }

        int reading = 1;

        while (wl_display_prepare_read(window->display) != 0)
        {
            if (wl_display_dispatch_pending(window->display) < 0)
            {
                window->closed = 1;
                reading = 0;
                break;
            }
        }

        if (!reading)
        {
            break;
        }

        if (wl_display_flush(window->display) < 0 && errno != EAGAIN)
        {
            wl_display_cancel_read(window->display);
            window->closed = 1;
            break;
        }

        int count = 0;

        watched[count].fd = wl_display_get_fd(window->display);
        watched[count].events = POLLIN;
        watched[count].revents = 0;
        count++;

        watched[count].fd = window->wake;
        watched[count].events = POLLIN;
        watched[count].revents = 0;
        count++;

        int fds[ATSPI_MAX_FDS];
        int extra = Atspi_Fds(window->accessibility, fds, ATSPI_MAX_FDS);

        for (int index = 0; index < extra; index++)
        {
            watched[count].fd = fds[index];
            watched[count].events = POLLIN;
            watched[count].revents = 0;
            count++;
        }

        int timeout = NT_Timeout(window->dirty && painted);
        int repeat = RepeatWait(window);

        if (repeat >= 0 && (timeout < 0 || repeat < timeout))
        {
            timeout = repeat;
        }

        if (poll(watched, (nfds_t)count, timeout) > 0 && (watched[0].revents & POLLIN) != 0)
        {
            wl_display_read_events(window->display);
        }
        else
        {
            wl_display_cancel_read(window->display);
        }

        Drain(window);

        if (wl_display_dispatch_pending(window->display) < 0)
        {
            window->closed = 1;
        }

        Atspi_Pump(window->accessibility);
    }

    return 0;
}

static void WL_Destroy(NativeWindow* handle)
{
    WLWindow* window = (WLWindow*)handle;

    if (window == NULL)
    {
        return;
    }

    Atspi_Destroy(window->accessibility);
    ReleaseWayland(window);

    free(window);
}

static void WL_SetTitle(NativeWindow* handle, const char* title)
{
    WLWindow* window = (WLWindow*)handle;

    if (window == NULL || window->toplevel == NULL)
    {
        return;
    }

    xdg_toplevel_set_title(window->toplevel, title == NULL ? "" : title);

    Atspi_SetTitle(window->accessibility, title);
}

static void WL_SetPixelsBuffer(NativeWindow* handle, void* buffer, int width, int height,
    int rowBytes)
{
    WLWindow* window = (WLWindow*)handle;

    if (window == NULL || buffer == NULL || width <= 0 || height <= 0)
    {
        return;
    }

    window->pixels = buffer;
    window->pixelsWidth = width;
    window->pixelsHeight = height;
    window->pixelsRowBytes = rowBytes;
}

static void WL_Invalidate(NativeWindow* handle)
{
    WLWindow* window = (WLWindow*)handle;

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

static void WL_Close(NativeWindow* handle)
{
    WLWindow* window = (WLWindow*)handle;

    if (window == NULL)
    {
        return;
    }

    window->closed = 1;

    WL_Invalidate(handle);
}

static unsigned int WL_GetDpi(NativeWindow* handle)
{
    WLWindow* window = (WLWindow*)handle;

    return window == NULL ? DEFAULT_DPI : (unsigned int)(DEFAULT_DPI * window->scale);
}

static int WL_IsPresentable(NativeWindow* handle)
{
    WLWindow* window = (WLWindow*)handle;

    return window != NULL && window->configured && !window->closed;
}

static void WL_SetCursor(NativeWindow* handle, int kind)
{
    WLWindow* window = (WLWindow*)handle;

    if (window == NULL)
    {
        return;
    }

    ReleaseCursorImage(window);

    window->cursorKind = kind;

    ApplyCursor(window);
}

static void WL_SetCursorImage(NativeWindow* handle, const void* pixels, int width, int height,
    int hotspotX, int hotspotY)
{
    WLWindow* window = (WLWindow*)handle;

    if (window == NULL || pixels == NULL || width <= 0 || height <= 0 || window->shm == NULL)
    {
        return;
    }

    ReleaseCursorImage(window);

    size_t stride = (size_t)width * 4;
    size_t size = stride * (size_t)height;
    int file = SharedFile(size);

    if (file < 0)
    {
        return;
    }

    unsigned char* data = mmap(NULL, size, PROT_READ | PROT_WRITE, MAP_SHARED, file, 0);

    if (data == MAP_FAILED)
    {
        close(file);
        return;
    }

    memcpy(data, pixels, size);

    struct wl_shm_pool* pool = wl_shm_create_pool(window->shm, file, (int32_t)size);

    close(file);

    if (pool == NULL)
    {
        munmap(data, size);
        return;
    }

    window->cursorImage.buffer = wl_shm_pool_create_buffer(pool, 0, width, height,
        (int32_t)stride, WL_SHM_FORMAT_ARGB8888);

    wl_shm_pool_destroy(pool);

    if (window->cursorImage.buffer == NULL)
    {
        munmap(data, size);
        return;
    }

    window->cursorImage.data = data;
    window->cursorImage.offset = size;
    window->cursorImage.width = width;
    window->cursorImage.height = height;
    window->cursorHotspotX = hotspotX;
    window->cursorHotspotY = hotspotY;
    window->cursorOwned = 1;
    window->cursorKind = 0;

    ApplyCursor(window);
}

static const char* WL_GetClipboardText(NativeWindow* handle)
{
    WLWindow* window = (WLWindow*)handle;

    if (window == NULL || window->offer == NULL || !window->offerIsText)
    {
        return NULL;
    }

    int pipes[2];

    if (pipe(pipes) < 0)
    {
        return NULL;
    }

    wl_data_offer_receive(window->offer, MIME_TEXT, pipes[1]);
    wl_display_flush(window->display);

    close(pipes[1]);

    size_t capacity = 4096;
    size_t length = 0;
    char* text = malloc(capacity);

    if (text == NULL)
    {
        close(pipes[0]);
        return NULL;
    }

    long long deadline = NT_Now() + CLIPBOARD_TIMEOUT_MS;

    while (NT_Now() < deadline && length < CLIPBOARD_MAX)
    {
        struct pollfd watched[2];

        watched[0].fd = pipes[0];
        watched[0].events = POLLIN;
        watched[0].revents = 0;
        watched[1].fd = wl_display_get_fd(window->display);
        watched[1].events = POLLIN;
        watched[1].revents = 0;

        int wait = (int)(deadline - NT_Now());

        if (poll(watched, 2, wait < 0 ? 0 : wait) <= 0)
        {
            break;
        }

        if ((watched[1].revents & POLLIN) != 0)
        {
            wl_display_dispatch(window->display);
        }

        if ((watched[0].revents & POLLIN) == 0)
        {
            continue;
        }

        if (length + 1 >= capacity)
        {
            capacity *= 2;

            char* grown = realloc(text, capacity);

            if (grown == NULL)
            {
                break;
            }

            text = grown;
        }

        ssize_t got = read(pipes[0], text + length, capacity - length - 1);

        if (got <= 0)
        {
            break;
        }

        length += (size_t)got;
    }

    close(pipes[0]);

    text[length] = 0;

    free(window->incomingText);

    window->incomingText = text;

    return window->incomingText;
}

static void WL_SetClipboardText(NativeWindow* handle, const char* text)
{
    WLWindow* window = (WLWindow*)handle;

    if (window == NULL || window->dataDevice == NULL || window->dataManager == NULL)
    {
        return;
    }

    free(window->clipboardText);

    window->clipboardText = text == NULL ? NULL : strdup(text);

    if (window->clipboardText == NULL)
    {
        return;
    }

    struct wl_data_source* source = wl_data_device_manager_create_data_source(window->dataManager);

    if (source == NULL)
    {
        return;
    }

    wl_data_source_add_listener(source, &SOURCE_LISTENER, window);
    wl_data_source_offer(source, MIME_TEXT);
    wl_data_source_offer(source, "text/plain");

    window->source = source;

    wl_data_device_set_selection(window->dataDevice, source, window->inputSerial);
    wl_display_flush(window->display);
}

static void WL_RegisterPaintCallBack(NativeWindow* handle, void callBack(int, int))
{
    WLWindow* window = (WLWindow*)handle;

    if (window != NULL)
    {
        window->paintCallBack = callBack;
    }
}

static void WL_RegisterPointerCallBack(NativeWindow* handle, void callBack(int, int, int, int))
{
    WLWindow* window = (WLWindow*)handle;

    if (window != NULL)
    {
        window->pointerCallBack = callBack;
    }
}

static void WL_RegisterKeyCallBack(NativeWindow* handle, void callBack(int, int, int, int))
{
    WLWindow* window = (WLWindow*)handle;

    if (window != NULL)
    {
        window->keyCallBack = callBack;
    }
}

static void WL_RegisterTextCallBack(NativeWindow* handle, void callBack(const char*))
{
    WLWindow* window = (WLWindow*)handle;

    if (window != NULL)
    {
        window->textCallBack = callBack;
    }
}

static void WL_RegisterWheelCallBack(NativeWindow* handle, void callBack(int, int, int, int, int))
{
    WLWindow* window = (WLWindow*)handle;

    if (window != NULL)
    {
        window->wheelCallBack = callBack;
    }
}

static void WL_RegisterAccessibilityCallBack(NativeWindow* handle,
    int callBack(int, int, const char*))
{
    WLWindow* window = (WLWindow*)handle;

    if (window != NULL)
    {
        Atspi_RegisterCallBack(window->accessibility, callBack);
    }
}

static int WL_AccessibilityIsActive(NativeWindow* handle)
{
    WLWindow* window = (WLWindow*)handle;

    return window == NULL ? 0 : Atspi_IsActive(window->accessibility);
}

static void WL_AccessibilityUpdateNode(NativeWindow* handle, int identifier, int parent, int role,
    long long states, int actions, int x, int y, int width, int height,
    const char* name, const char* description, const char* value, const char* shortcut)
{
    WLWindow* window = (WLWindow*)handle;

    if (window != NULL)
    {
        Atspi_UpdateNode(window->accessibility, identifier, parent, role, states, actions,
            x, y, width, height, name, description, value, shortcut);
    }
}

static void WL_AccessibilityCommit(NativeWindow* handle, int root, const int* order, int count)
{
    WLWindow* window = (WLWindow*)handle;

    if (window != NULL)
    {
        Atspi_Commit(window->accessibility, root, order, count);
    }
}

static void WL_AccessibilityNotify(NativeWindow* handle, int identifier, int kind,
    const char* text)
{
    WLWindow* window = (WLWindow*)handle;

    if (window != NULL)
    {
        Atspi_Notify(window->accessibility, identifier, kind, text);
    }
}

const NativeBackend WL_Backend =
{
    "Wayland",
    WL_Run,
    WL_Destroy,
    WL_SetTitle,
    WL_SetPixelsBuffer,
    WL_Invalidate,
    WL_Close,
    WL_GetDpi,
    WL_IsPresentable,
    WL_SetCursor,
    WL_SetCursorImage,
    WL_GetClipboardText,
    WL_SetClipboardText,
    WL_RegisterPaintCallBack,
    WL_RegisterPointerCallBack,
    WL_RegisterKeyCallBack,
    WL_RegisterTextCallBack,
    WL_RegisterWheelCallBack,
    WL_RegisterAccessibilityCallBack,
    WL_AccessibilityIsActive,
    WL_AccessibilityUpdateNode,
    WL_AccessibilityCommit,
    WL_AccessibilityNotify
};
