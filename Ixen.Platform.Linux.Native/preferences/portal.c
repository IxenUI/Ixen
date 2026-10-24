#include "portal.h"

#include <dbus/dbus.h>

#include <string.h>

#define PORTAL_NAME "org.freedesktop.portal.Desktop"
#define PORTAL_PATH "/org/freedesktop/portal/desktop"
#define PORTAL_SETTINGS "org.freedesktop.portal.Settings"
#define PORTAL_TIMEOUT 300

#define GNOME_INTERFACE "org.gnome.desktop.interface"
#define GNOME_A11Y "org.gnome.desktop.a11y.interface"
#define APPEARANCE "org.freedesktop.appearance"

#define UNKNOWN -1

struct Portal
{
    DBusConnection* bus;
    int animations;
    int a11yContrast;
    int appearanceContrast;
    int textScale;
};

static Portal _portal = { NULL, UNKNOWN, UNKNOWN, UNKNOWN, UNKNOWN };
static int _tried = 0;

static int Store(int* slot, int value)
{
    if (*slot == value)
    {
        return 0;
    }

    *slot = value;

    return 1;
}

static int Unwrap(DBusMessageIter* iter, DBusMessageIter* into)
{
    *into = *iter;

    while (dbus_message_iter_get_arg_type(into) == DBUS_TYPE_VARIANT)
    {
        DBusMessageIter inner;

        dbus_message_iter_recurse(into, &inner);
        *into = inner;
    }

    return dbus_message_iter_get_arg_type(into);
}

static int Apply(Portal* portal, const char* space, const char* key, DBusMessageIter* value)
{
    DBusMessageIter basic;
    int type = Unwrap(value, &basic);

    if (strcmp(space, GNOME_INTERFACE) == 0 && strcmp(key, "enable-animations") == 0
        && type == DBUS_TYPE_BOOLEAN)
    {
        dbus_bool_t on = FALSE;

        dbus_message_iter_get_basic(&basic, &on);

        return Store(&portal->animations, on ? 1 : 0);
    }

    if (strcmp(space, GNOME_A11Y) == 0 && strcmp(key, "high-contrast") == 0
        && type == DBUS_TYPE_BOOLEAN)
    {
        dbus_bool_t on = FALSE;

        dbus_message_iter_get_basic(&basic, &on);

        return Store(&portal->a11yContrast, on ? 1 : 0);
    }

    if (strcmp(space, APPEARANCE) == 0 && strcmp(key, "contrast") == 0
        && type == DBUS_TYPE_UINT32)
    {
        dbus_uint32_t contrast = 0;

        dbus_message_iter_get_basic(&basic, &contrast);

        return Store(&portal->appearanceContrast, contrast == 1 ? 1 : 0);
    }

    if (strcmp(space, GNOME_INTERFACE) == 0 && strcmp(key, "text-scaling-factor") == 0
        && type == DBUS_TYPE_DOUBLE)
    {
        double factor = 1;

        dbus_message_iter_get_basic(&basic, &factor);

        return Store(&portal->textScale, (int)(factor * 100 + 0.5));
    }

    return 0;
}

static void Read(Portal* portal, const char* space, const char* key)
{
    const char* methods[] = { "ReadOne", "Read" };

    for (int index = 0; index < 2; index++)
    {
        DBusMessage* call = dbus_message_new_method_call(PORTAL_NAME, PORTAL_PATH, PORTAL_SETTINGS,
            methods[index]);

        if (call == NULL)
        {
            return;
        }

        dbus_message_append_args(call, DBUS_TYPE_STRING, &space, DBUS_TYPE_STRING, &key,
            DBUS_TYPE_INVALID);

        DBusError error;

        dbus_error_init(&error);

        DBusMessage* reply = dbus_connection_send_with_reply_and_block(portal->bus, call,
            PORTAL_TIMEOUT, &error);

        dbus_message_unref(call);

        if (reply == NULL)
        {
            int unknown = dbus_error_has_name(&error, DBUS_ERROR_UNKNOWN_METHOD);

            dbus_error_free(&error);

            if (unknown)
            {
                continue;
            }

            return;
        }

        DBusMessageIter iter;

        if (dbus_message_iter_init(reply, &iter))
        {
            Apply(portal, space, key, &iter);
        }

        dbus_message_unref(reply);

        return;
    }
}

Portal* Portal_Get(void)
{
    if (_tried)
    {
        return _portal.bus == NULL ? NULL : &_portal;
    }

    _tried = 1;

    DBusError error;

    dbus_error_init(&error);

    _portal.bus = dbus_bus_get_private(DBUS_BUS_SESSION, &error);

    if (_portal.bus == NULL)
    {
        dbus_error_free(&error);

        return NULL;
    }

    dbus_connection_set_exit_on_disconnect(_portal.bus, FALSE);

    dbus_bus_add_match(_portal.bus, "type='signal',interface='" PORTAL_SETTINGS "',member='SettingChanged'",
        &error);
    dbus_error_free(&error);

    Read(&_portal, GNOME_INTERFACE, "enable-animations");
    Read(&_portal, GNOME_A11Y, "high-contrast");
    Read(&_portal, APPEARANCE, "contrast");
    Read(&_portal, GNOME_INTERFACE, "text-scaling-factor");

    return &_portal;
}

int Portal_Fds(Portal* portal, int* fds, int max)
{
    int fd = -1;

    if (portal == NULL || portal->bus == NULL || max < 1 || !dbus_connection_get_unix_fd(portal->bus, &fd))
    {
        return 0;
    }

    fds[0] = fd;

    return 1;
}

int Portal_Pump(Portal* portal)
{
    int changed = 0;

    if (portal == NULL || portal->bus == NULL || !dbus_connection_read_write(portal->bus, 0))
    {
        return 0;
    }

    DBusMessage* message;

    while ((message = dbus_connection_pop_message(portal->bus)) != NULL)
    {
        if (dbus_message_is_signal(message, PORTAL_SETTINGS, "SettingChanged"))
        {
            DBusMessageIter iter;
            const char* space = NULL;
            const char* key = NULL;

            if (dbus_message_iter_init(message, &iter)
                && dbus_message_iter_get_arg_type(&iter) == DBUS_TYPE_STRING)
            {
                dbus_message_iter_get_basic(&iter, &space);

                if (dbus_message_iter_next(&iter) && dbus_message_iter_get_arg_type(&iter) == DBUS_TYPE_STRING)
                {
                    dbus_message_iter_get_basic(&iter, &key);

                    if (dbus_message_iter_next(&iter))
                    {
                        changed |= Apply(portal, space, key, &iter);
                    }
                }
            }
        }

        dbus_message_unref(message);
    }

    return changed;
}

int Portal_ReducedMotion(Portal* portal)
{
    if (portal == NULL || portal->animations == UNKNOWN)
    {
        return UNKNOWN;
    }

    return portal->animations ? 0 : 1;
}

int Portal_HighContrast(Portal* portal)
{
    if (portal == NULL || (portal->a11yContrast == UNKNOWN && portal->appearanceContrast == UNKNOWN))
    {
        return UNKNOWN;
    }

    return portal->a11yContrast == 1 || portal->appearanceContrast == 1 ? 1 : 0;
}

int Portal_TextScale(Portal* portal)
{
    return portal == NULL ? UNKNOWN : portal->textScale;
}
