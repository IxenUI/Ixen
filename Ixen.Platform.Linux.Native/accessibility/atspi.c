#include "atspi.h"

#include <dbus/dbus.h>

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#define A11Y_BUS_NAME "org.a11y.Bus"
#define A11Y_BUS_PATH "/org/a11y/bus"
#define A11Y_BUS_IFACE "org.a11y.Bus"
#define A11Y_STATUS_IFACE "org.a11y.Status"

#define REGISTRY_NAME "org.a11y.atspi.Registry"
#define ROOT_PATH "/org/a11y/atspi/accessible/root"
#define NULL_PATH "/org/a11y/atspi/null"
#define CACHE_PATH "/org/a11y/atspi/cache"
#define CACHE_ITEM "((so)(so)(so)a(so)assusau)"
#define PATH_PREFIX "/org/a11y/atspi/accessible/"

#define IFACE_ACCESSIBLE "org.a11y.atspi.Accessible"
#define IFACE_COMPONENT "org.a11y.atspi.Component"
#define IFACE_ACTION "org.a11y.atspi.Action"
#define IFACE_APPLICATION "org.a11y.atspi.Application"
#define IFACE_TEXT "org.a11y.atspi.Text"
#define IFACE_EDITABLE "org.a11y.atspi.EditableText"
#define IFACE_SOCKET "org.a11y.atspi.Socket"
#define IFACE_CACHE "org.a11y.atspi.Cache"
#define IFACE_EVENT_OBJECT "org.a11y.atspi.Event.Object"
#define IFACE_PROPERTIES "org.freedesktop.DBus.Properties"
#define IFACE_INTROSPECTABLE "org.freedesktop.DBus.Introspectable"

#define ROLE_APPLICATION 75
#define ROLE_INVALID 0

#define COORD_SCREEN 0

#define CALL_TIMEOUT_MS 800

#define APP_NODE (-2)
#define NO_NODE (-1)

typedef struct AtspiNode
{
    int used;
    int generation;
    int parent;
    int role;
    unsigned long long states;
    int actions;
    int x;
    int y;
    int width;
    int height;
    char* name;
    char* description;
    char* value;
    char* shortcut;
    int* children;
    int childCount;
    int childCapacity;
    int indexInParent;
} AtspiNode;

struct AtspiBridge
{
    DBusConnection* session;
    DBusConnection* bus;

    char* parentName;
    char* parentPath;
    char* title;

    int enabled;
    int registered;
    int applicationId;
    int rootId;
    int generation;
    int originX;
    int originY;

    AtspiNode* nodes;
    int capacity;

    int (*actionCallBack)(int, int, const char*);
};

static char* Copy(const char* text)
{
    if (text == NULL)
    {
        return NULL;
    }

    size_t length = strlen(text);
    char* copy = (char*)malloc(length + 1);

    if (copy == NULL)
    {
        return NULL;
    }

    memcpy(copy, text, length + 1);

    return copy;
}

static const char* Text(const char* value)
{
    return value == NULL ? "" : value;
}

static const char* RoleName(int role)
{
    switch (role)
    {
        case 7: return "check box";
        case 10: return "column header";
        case 11: return "combo box";
        case 16: return "dialog";
        case 20: return "filler";
        case 23: return "frame";
        case 27: return "image";
        case 29: return "label";
        case 31: return "list";
        case 32: return "list item";
        case 33: return "menu";
        case 35: return "menu item";
        case 37: return "page tab";
        case 38: return "page tab list";
        case 39: return "panel";
        case 40: return "password text";
        case 42: return "progress bar";
        case 43: return "push button";
        case 44: return "radio button";
        case 48: return "scroll bar";
        case 51: return "slider";
        case 55: return "table";
        case 56: return "table cell";
        case 61: return "text";
        case 62: return "toggle button";
        case 65: return "tree";
        case 67: return "unknown";
        case 75: return "application";
        case 79: return "entry";
        case 83: return "heading";
        case 88: return "link";
        case 90: return "table row";
        case 91: return "tree item";
        default: return "unknown";
    }
}

static AtspiNode* NodeAt(AtspiBridge* bridge, int identifier)
{
    if (identifier < 0 || identifier >= bridge->capacity)
    {
        return NULL;
    }

    AtspiNode* node = &bridge->nodes[identifier];

    return node->used ? node : NULL;
}

static void ReleaseNode(AtspiNode* node)
{
    free(node->name);
    free(node->description);
    free(node->value);
    free(node->shortcut);
    free(node->children);

    memset(node, 0, sizeof(AtspiNode));
}

static int EnsureCapacity(AtspiBridge* bridge, int identifier)
{
    if (identifier < bridge->capacity)
    {
        return 1;
    }

    int wanted = bridge->capacity == 0 ? 64 : bridge->capacity;

    while (wanted <= identifier)
    {
        wanted *= 2;
    }

    AtspiNode* grown = (AtspiNode*)realloc(bridge->nodes, (size_t)wanted * sizeof(AtspiNode));

    if (grown == NULL)
    {
        return 0;
    }

    memset(grown + bridge->capacity, 0, (size_t)(wanted - bridge->capacity) * sizeof(AtspiNode));

    bridge->nodes = grown;
    bridge->capacity = wanted;

    return 1;
}

static void PathOf(int identifier, char* buffer, size_t size)
{
    snprintf(buffer, size, PATH_PREFIX "%d", identifier);
}

static int NodeOfPath(AtspiBridge* bridge, const char* path)
{
    if (path == NULL)
    {
        return NO_NODE;
    }

    if (strcmp(path, ROOT_PATH) == 0)
    {
        return APP_NODE;
    }

    size_t prefix = strlen(PATH_PREFIX);

    if (strncmp(path, PATH_PREFIX, prefix) != 0)
    {
        return NO_NODE;
    }

    const char* tail = path + prefix;

    if (*tail < '0' || *tail > '9')
    {
        return NO_NODE;
    }

    int identifier = atoi(tail);

    return NodeAt(bridge, identifier) == NULL ? NO_NODE : identifier;
}

static void AppendRef(DBusMessageIter* iter, const char* name, const char* path)
{
    DBusMessageIter structure;

    dbus_message_iter_open_container(iter, DBUS_TYPE_STRUCT, NULL, &structure);
    dbus_message_iter_append_basic(&structure, DBUS_TYPE_STRING, &name);
    dbus_message_iter_append_basic(&structure, DBUS_TYPE_OBJECT_PATH, &path);
    dbus_message_iter_close_container(iter, &structure);
}

static void AppendSelfRef(AtspiBridge* bridge, DBusMessageIter* iter, int identifier)
{
    const char* name = dbus_bus_get_unique_name(bridge->bus);
    char path[64];

    if (identifier == APP_NODE)
    {
        AppendRef(iter, name, ROOT_PATH);

        return;
    }

    PathOf(identifier, path, sizeof(path));
    AppendRef(iter, name, path);
}

static void AppendNullRef(DBusMessageIter* iter)
{
    AppendRef(iter, "", NULL_PATH);
}

static void AppendVariantString(DBusMessageIter* iter, const char* value)
{
    DBusMessageIter variant;

    dbus_message_iter_open_container(iter, DBUS_TYPE_VARIANT, "s", &variant);
    dbus_message_iter_append_basic(&variant, DBUS_TYPE_STRING, &value);
    dbus_message_iter_close_container(iter, &variant);
}

static void AppendVariantInt(DBusMessageIter* iter, int value)
{
    DBusMessageIter variant;
    dbus_int32_t number = value;

    dbus_message_iter_open_container(iter, DBUS_TYPE_VARIANT, "i", &variant);
    dbus_message_iter_append_basic(&variant, DBUS_TYPE_INT32, &number);
    dbus_message_iter_close_container(iter, &variant);
}

static void AppendVariantRef(AtspiBridge* bridge, DBusMessageIter* iter, int identifier)
{
    DBusMessageIter variant;

    dbus_message_iter_open_container(iter, DBUS_TYPE_VARIANT, "(so)", &variant);

    if (identifier == NO_NODE)
    {
        AppendNullRef(&variant);
    }
    else
    {
        AppendSelfRef(bridge, &variant, identifier);
    }

    dbus_message_iter_close_container(iter, &variant);
}

static void AppendDictString(DBusMessageIter* dict, const char* key, const char* value)
{
    DBusMessageIter entry;

    dbus_message_iter_open_container(dict, DBUS_TYPE_DICT_ENTRY, NULL, &entry);
    dbus_message_iter_append_basic(&entry, DBUS_TYPE_STRING, &key);
    AppendVariantString(&entry, value);
    dbus_message_iter_close_container(dict, &entry);
}

static void AppendDictInt(DBusMessageIter* dict, const char* key, int value)
{
    DBusMessageIter entry;

    dbus_message_iter_open_container(dict, DBUS_TYPE_DICT_ENTRY, NULL, &entry);
    dbus_message_iter_append_basic(&entry, DBUS_TYPE_STRING, &key);
    AppendVariantInt(&entry, value);
    dbus_message_iter_close_container(dict, &entry);
}

static void AppendDictRef(AtspiBridge* bridge, DBusMessageIter* dict, const char* key, int identifier)
{
    DBusMessageIter entry;

    dbus_message_iter_open_container(dict, DBUS_TYPE_DICT_ENTRY, NULL, &entry);
    dbus_message_iter_append_basic(&entry, DBUS_TYPE_STRING, &key);
    AppendVariantRef(bridge, &entry, identifier);
    dbus_message_iter_close_container(dict, &entry);
}

static void Send(AtspiBridge* bridge, DBusMessage* message)
{
    if (message == NULL)
    {
        return;
    }

    dbus_connection_send(bridge->bus, message, NULL);
    dbus_message_unref(message);
}

static DBusHandlerResult Answer(AtspiBridge* bridge, DBusMessage* message, DBusMessage* reply)
{
    if (reply == NULL)
    {
        return DBUS_HANDLER_RESULT_NEED_MEMORY;
    }

    (void)message;

    Send(bridge, reply);

    return DBUS_HANDLER_RESULT_HANDLED;
}

static DBusHandlerResult Unknown(AtspiBridge* bridge, DBusMessage* message)
{
    DBusMessage* reply = dbus_message_new_error(message, DBUS_ERROR_UNKNOWN_METHOD, "not supported");

    return Answer(bridge, message, reply);
}

static int ParentOf(AtspiBridge* bridge, int identifier)
{
    if (identifier == APP_NODE)
    {
        return NO_NODE;
    }

    AtspiNode* node = NodeAt(bridge, identifier);

    if (node == NULL || identifier == bridge->rootId)
    {
        return APP_NODE;
    }

    return node->parent;
}

static int ChildCountOf(AtspiBridge* bridge, int identifier)
{
    if (identifier == APP_NODE)
    {
        return NodeAt(bridge, bridge->rootId) == NULL ? 0 : 1;
    }

    AtspiNode* node = NodeAt(bridge, identifier);

    return node == NULL ? 0 : node->childCount;
}

static int ChildOf(AtspiBridge* bridge, int identifier, int index)
{
    if (identifier == APP_NODE)
    {
        return index == 0 ? bridge->rootId : NO_NODE;
    }

    AtspiNode* node = NodeAt(bridge, identifier);

    if (node == NULL || index < 0 || index >= node->childCount)
    {
        return NO_NODE;
    }

    return node->children[index];
}

static int RoleOf(AtspiBridge* bridge, int identifier)
{
    if (identifier == APP_NODE)
    {
        return ROLE_APPLICATION;
    }

    AtspiNode* node = NodeAt(bridge, identifier);

    return node == NULL ? ROLE_INVALID : node->role;
}

static const char* NameOf(AtspiBridge* bridge, int identifier)
{
    if (identifier == APP_NODE)
    {
        return Text(bridge->title);
    }

    AtspiNode* node = NodeAt(bridge, identifier);

    if (node == NULL)
    {
        return "";
    }

    if (identifier == bridge->rootId && (node->name == NULL || node->name[0] == '\0'))
    {
        return Text(bridge->title);
    }

    return Text(node->name);
}

static const char* DescriptionOf(AtspiBridge* bridge, int identifier)
{
    if (identifier == APP_NODE)
    {
        return "";
    }

    AtspiNode* node = NodeAt(bridge, identifier);

    return node == NULL ? "" : Text(node->description);
}

static const char* ValueOf(AtspiBridge* bridge, int identifier)
{
    AtspiNode* node = identifier == APP_NODE ? NULL : NodeAt(bridge, identifier);

    return node == NULL ? "" : Text(node->value);
}

static int HasText(AtspiBridge* bridge, int identifier)
{
    AtspiNode* node = identifier == APP_NODE ? NULL : NodeAt(bridge, identifier);

    return node != NULL && (node->value != NULL || (node->actions & IXEN_AXA_SET_VALUE) != 0);
}

static int HasActions(AtspiBridge* bridge, int identifier)
{
    AtspiNode* node = identifier == APP_NODE ? NULL : NodeAt(bridge, identifier);

    return node != NULL && (node->actions & IXEN_AXA_INVOKE) != 0;
}

static int HasEditable(AtspiBridge* bridge, int identifier)
{
    AtspiNode* node = identifier == APP_NODE ? NULL : NodeAt(bridge, identifier);

    return node != NULL && (node->actions & IXEN_AXA_SET_VALUE) != 0;
}

static void ExtentsOf(AtspiBridge* bridge, int identifier, int coordType, int* box)
{
    AtspiNode* node = identifier == APP_NODE ? NodeAt(bridge, bridge->rootId) : NodeAt(bridge, identifier);

    if (node == NULL)
    {
        box[0] = 0;
        box[1] = 0;
        box[2] = 0;
        box[3] = 0;

        return;
    }

    box[0] = node->x;
    box[1] = node->y;
    box[2] = node->width;
    box[3] = node->height;

    if (coordType == COORD_SCREEN)
    {
        box[0] += bridge->originX;
        box[1] += bridge->originY;
    }
}

static int DeepestAt(AtspiBridge* bridge, int identifier, int x, int y)
{
    AtspiNode* node = NodeAt(bridge, identifier);

    if (node == NULL)
    {
        return NO_NODE;
    }

    if (x < node->x || y < node->y || x >= node->x + node->width || y >= node->y + node->height)
    {
        return NO_NODE;
    }

    for (int index = node->childCount - 1; index >= 0; index--)
    {
        int hit = DeepestAt(bridge, node->children[index], x, y);

        if (hit != NO_NODE)
        {
            return hit;
        }
    }

    return identifier;
}

static DBusHandlerResult AccessibleProperty(AtspiBridge* bridge, DBusMessage* message,
    int identifier, const char* property)
{
    DBusMessage* reply = dbus_message_new_method_return(message);

    if (reply == NULL)
    {
        return DBUS_HANDLER_RESULT_NEED_MEMORY;
    }

    DBusMessageIter iter;

    dbus_message_iter_init_append(reply, &iter);

    if (strcmp(property, "Name") == 0)
    {
        AppendVariantString(&iter, NameOf(bridge, identifier));
    }
    else if (strcmp(property, "Description") == 0)
    {
        AppendVariantString(&iter, DescriptionOf(bridge, identifier));
    }
    else if (strcmp(property, "HelpText") == 0)
    {
        AppendVariantString(&iter, DescriptionOf(bridge, identifier));
    }
    else if (strcmp(property, "Locale") == 0)
    {
        AppendVariantString(&iter, "C");
    }
    else if (strcmp(property, "AccessibleId") == 0)
    {
        AppendVariantString(&iter, "");
    }
    else if (strcmp(property, "ChildCount") == 0)
    {
        AppendVariantInt(&iter, ChildCountOf(bridge, identifier));
    }
    else if (strcmp(property, "Parent") == 0)
    {
        DBusMessageIter variant;

        dbus_message_iter_open_container(&iter, DBUS_TYPE_VARIANT, "(so)", &variant);

        if (identifier == APP_NODE)
        {
            if (bridge->parentName == NULL)
            {
                AppendNullRef(&variant);
            }
            else
            {
                AppendRef(&variant, bridge->parentName, bridge->parentPath);
            }
        }
        else
        {
            AppendSelfRef(bridge, &variant, ParentOf(bridge, identifier));
        }

        dbus_message_iter_close_container(&iter, &variant);
    }
    else
    {
        dbus_message_unref(reply);

        return Unknown(bridge, message);
    }

    return Answer(bridge, message, reply);
}

static DBusHandlerResult ApplicationProperty(AtspiBridge* bridge, DBusMessage* message,
    const char* property)
{
    DBusMessage* reply = dbus_message_new_method_return(message);

    if (reply == NULL)
    {
        return DBUS_HANDLER_RESULT_NEED_MEMORY;
    }

    DBusMessageIter iter;

    dbus_message_iter_init_append(reply, &iter);

    if (strcmp(property, "ToolkitName") == 0)
    {
        AppendVariantString(&iter, "Ixen");
    }
    else if (strcmp(property, "Version") == 0)
    {
        AppendVariantString(&iter, "1.0");
    }
    else if (strcmp(property, "AtspiVersion") == 0)
    {
        AppendVariantString(&iter, "2.1");
    }
    else if (strcmp(property, "Id") == 0)
    {
        AppendVariantInt(&iter, bridge->applicationId);
    }
    else
    {
        dbus_message_unref(reply);

        return Unknown(bridge, message);
    }

    return Answer(bridge, message, reply);
}

static DBusHandlerResult TextProperty(AtspiBridge* bridge, DBusMessage* message,
    int identifier, const char* property)
{
    DBusMessage* reply = dbus_message_new_method_return(message);

    if (reply == NULL)
    {
        return DBUS_HANDLER_RESULT_NEED_MEMORY;
    }

    DBusMessageIter iter;

    dbus_message_iter_init_append(reply, &iter);

    if (strcmp(property, "CharacterCount") == 0)
    {
        AppendVariantInt(&iter, (int)strlen(ValueOf(bridge, identifier)));
    }
    else if (strcmp(property, "CaretOffset") == 0)
    {
        AppendVariantInt(&iter, (int)strlen(ValueOf(bridge, identifier)));
    }
    else
    {
        dbus_message_unref(reply);

        return Unknown(bridge, message);
    }

    return Answer(bridge, message, reply);
}

static DBusHandlerResult ActionProperty(AtspiBridge* bridge, DBusMessage* message, int identifier,
    const char* property)
{
    if (strcmp(property, "NActions") != 0)
    {
        return Unknown(bridge, message);
    }

    DBusMessage* reply = dbus_message_new_method_return(message);

    if (reply == NULL)
    {
        return DBUS_HANDLER_RESULT_NEED_MEMORY;
    }

    DBusMessageIter iter;

    dbus_message_iter_init_append(reply, &iter);
    AppendVariantInt(&iter, HasActions(bridge, identifier) ? 1 : 0);

    return Answer(bridge, message, reply);
}

static DBusHandlerResult GetAll(AtspiBridge* bridge, DBusMessage* message, int identifier,
    const char* interface)
{
    DBusMessage* reply = dbus_message_new_method_return(message);

    if (reply == NULL)
    {
        return DBUS_HANDLER_RESULT_NEED_MEMORY;
    }

    DBusMessageIter iter;
    DBusMessageIter dict;

    dbus_message_iter_init_append(reply, &iter);
    dbus_message_iter_open_container(&iter, DBUS_TYPE_ARRAY, "{sv}", &dict);

    if (strcmp(interface, IFACE_ACCESSIBLE) == 0)
    {
        AppendDictString(&dict, "Name", NameOf(bridge, identifier));
        AppendDictString(&dict, "Description", DescriptionOf(bridge, identifier));
        AppendDictString(&dict, "Locale", "C");
        AppendDictString(&dict, "AccessibleId", "");
        AppendDictInt(&dict, "ChildCount", ChildCountOf(bridge, identifier));

        if (identifier == APP_NODE && bridge->parentName != NULL)
        {
            DBusMessageIter entry;
            DBusMessageIter variant;
            const char* key = "Parent";

            dbus_message_iter_open_container(&dict, DBUS_TYPE_DICT_ENTRY, NULL, &entry);
            dbus_message_iter_append_basic(&entry, DBUS_TYPE_STRING, &key);
            dbus_message_iter_open_container(&entry, DBUS_TYPE_VARIANT, "(so)", &variant);
            AppendRef(&variant, bridge->parentName, bridge->parentPath);
            dbus_message_iter_close_container(&entry, &variant);
            dbus_message_iter_close_container(&dict, &entry);
        }
        else
        {
            AppendDictRef(bridge, &dict, "Parent", ParentOf(bridge, identifier));
        }
    }
    else if (strcmp(interface, IFACE_APPLICATION) == 0)
    {
        AppendDictString(&dict, "ToolkitName", "Ixen");
        AppendDictString(&dict, "Version", "1.0");
        AppendDictString(&dict, "AtspiVersion", "2.1");
        AppendDictInt(&dict, "Id", bridge->applicationId);
    }
    else if (strcmp(interface, IFACE_TEXT) == 0)
    {
        int length = (int)strlen(ValueOf(bridge, identifier));

        AppendDictInt(&dict, "CharacterCount", length);
        AppendDictInt(&dict, "CaretOffset", length);
    }
    else if (strcmp(interface, IFACE_ACTION) == 0)
    {
        AppendDictInt(&dict, "NActions", HasActions(bridge, identifier) ? 1 : 0);
    }

    dbus_message_iter_close_container(&iter, &dict);

    return Answer(bridge, message, reply);
}

static DBusHandlerResult Properties(AtspiBridge* bridge, DBusMessage* message, int identifier)
{
    const char* member = dbus_message_get_member(message);
    DBusError error;
    const char* interface = NULL;
    const char* property = NULL;

    dbus_error_init(&error);

    if (strcmp(member, "GetAll") == 0)
    {
        if (!dbus_message_get_args(message, &error, DBUS_TYPE_STRING, &interface, DBUS_TYPE_INVALID))
        {
            dbus_error_free(&error);

            return Unknown(bridge, message);
        }

        return GetAll(bridge, message, identifier, interface);
    }

    if (strcmp(member, "Set") == 0)
    {
        DBusMessageIter iter;

        if (dbus_message_iter_init(message, &iter))
        {
            const char* name = NULL;

            dbus_message_iter_get_basic(&iter, &interface);
            dbus_message_iter_next(&iter);
            dbus_message_iter_get_basic(&iter, &name);
            dbus_message_iter_next(&iter);

            if (interface != NULL && name != NULL
                && strcmp(interface, IFACE_APPLICATION) == 0 && strcmp(name, "Id") == 0)
            {
                DBusMessageIter variant;
                dbus_int32_t value = 0;

                dbus_message_iter_recurse(&iter, &variant);
                dbus_message_iter_get_basic(&variant, &value);

                bridge->applicationId = (int)value;
            }
        }

        return Answer(bridge, message, dbus_message_new_method_return(message));
    }

    if (strcmp(member, "Get") != 0)
    {
        return Unknown(bridge, message);
    }

    if (!dbus_message_get_args(message, &error, DBUS_TYPE_STRING, &interface,
        DBUS_TYPE_STRING, &property, DBUS_TYPE_INVALID))
    {
        dbus_error_free(&error);

        return Unknown(bridge, message);
    }

    if (strcmp(interface, IFACE_ACCESSIBLE) == 0)
    {
        return AccessibleProperty(bridge, message, identifier, property);
    }

    if (strcmp(interface, IFACE_APPLICATION) == 0)
    {
        return ApplicationProperty(bridge, message, property);
    }

    if (strcmp(interface, IFACE_TEXT) == 0)
    {
        return TextProperty(bridge, message, identifier, property);
    }

    if (strcmp(interface, IFACE_ACTION) == 0)
    {
        return ActionProperty(bridge, message, identifier, property);
    }

    return Unknown(bridge, message);
}

static DBusHandlerResult Accessible(AtspiBridge* bridge, DBusMessage* message, int identifier)
{
    const char* member = dbus_message_get_member(message);
    DBusMessage* reply = dbus_message_new_method_return(message);

    if (reply == NULL)
    {
        return DBUS_HANDLER_RESULT_NEED_MEMORY;
    }

    DBusMessageIter iter;

    dbus_message_iter_init_append(reply, &iter);

    if (strcmp(member, "GetChildren") == 0)
    {
        DBusMessageIter array;
        int count = ChildCountOf(bridge, identifier);

        dbus_message_iter_open_container(&iter, DBUS_TYPE_ARRAY, "(so)", &array);

        for (int index = 0; index < count; index++)
        {
            AppendSelfRef(bridge, &array, ChildOf(bridge, identifier, index));
        }

        dbus_message_iter_close_container(&iter, &array);
    }
    else if (strcmp(member, "GetChildAtIndex") == 0)
    {
        DBusError error;
        dbus_int32_t index = 0;

        dbus_error_init(&error);
        dbus_message_get_args(message, &error, DBUS_TYPE_INT32, &index, DBUS_TYPE_INVALID);
        dbus_error_free(&error);

        int child = ChildOf(bridge, identifier, (int)index);

        if (child == NO_NODE)
        {
            AppendNullRef(&iter);
        }
        else
        {
            AppendSelfRef(bridge, &iter, child);
        }
    }
    else if (strcmp(member, "GetIndexInParent") == 0)
    {
        dbus_int32_t index = 0;

        if (identifier != APP_NODE && identifier != bridge->rootId)
        {
            AtspiNode* node = NodeAt(bridge, identifier);

            index = node == NULL ? -1 : node->indexInParent;
        }
        else if (identifier == APP_NODE)
        {
            index = -1;
        }

        dbus_message_iter_append_basic(&iter, DBUS_TYPE_INT32, &index);
    }
    else if (strcmp(member, "GetRelationSet") == 0)
    {
        DBusMessageIter array;

        dbus_message_iter_open_container(&iter, DBUS_TYPE_ARRAY, "(ua(so))", &array);
        dbus_message_iter_close_container(&iter, &array);
    }
    else if (strcmp(member, "GetRole") == 0)
    {
        dbus_uint32_t role = (dbus_uint32_t)RoleOf(bridge, identifier);

        dbus_message_iter_append_basic(&iter, DBUS_TYPE_UINT32, &role);
    }
    else if (strcmp(member, "GetRoleName") == 0 || strcmp(member, "GetLocalizedRoleName") == 0)
    {
        const char* name = RoleName(RoleOf(bridge, identifier));

        dbus_message_iter_append_basic(&iter, DBUS_TYPE_STRING, &name);
    }
    else if (strcmp(member, "GetState") == 0)
    {
        DBusMessageIter array;
        AtspiNode* node = identifier == APP_NODE ? NULL : NodeAt(bridge, identifier);
        unsigned long long states = node == NULL ? 0 : node->states;
        dbus_uint32_t low = (dbus_uint32_t)(states & 0xFFFFFFFFull);
        dbus_uint32_t high = (dbus_uint32_t)((states >> 32) & 0xFFFFFFFFull);

        dbus_message_iter_open_container(&iter, DBUS_TYPE_ARRAY, "u", &array);
        dbus_message_iter_append_basic(&array, DBUS_TYPE_UINT32, &low);
        dbus_message_iter_append_basic(&array, DBUS_TYPE_UINT32, &high);
        dbus_message_iter_close_container(&iter, &array);
    }
    else if (strcmp(member, "GetAttributes") == 0)
    {
        DBusMessageIter array;

        dbus_message_iter_open_container(&iter, DBUS_TYPE_ARRAY, "{ss}", &array);
        dbus_message_iter_close_container(&iter, &array);
    }
    else if (strcmp(member, "GetApplication") == 0)
    {
        AppendSelfRef(bridge, &iter, APP_NODE);
    }
    else if (strcmp(member, "GetInterfaces") == 0)
    {
        DBusMessageIter array;
        const char* name;

        dbus_message_iter_open_container(&iter, DBUS_TYPE_ARRAY, "s", &array);

        name = IFACE_ACCESSIBLE;
        dbus_message_iter_append_basic(&array, DBUS_TYPE_STRING, &name);

        if (identifier == APP_NODE)
        {
            name = IFACE_APPLICATION;
            dbus_message_iter_append_basic(&array, DBUS_TYPE_STRING, &name);
        }
        else
        {
            name = IFACE_COMPONENT;
            dbus_message_iter_append_basic(&array, DBUS_TYPE_STRING, &name);

            if (HasActions(bridge, identifier))
            {
                name = IFACE_ACTION;
                dbus_message_iter_append_basic(&array, DBUS_TYPE_STRING, &name);
            }

            if (HasText(bridge, identifier))
            {
                name = IFACE_TEXT;
                dbus_message_iter_append_basic(&array, DBUS_TYPE_STRING, &name);
            }

            if (HasEditable(bridge, identifier))
            {
                name = IFACE_EDITABLE;
                dbus_message_iter_append_basic(&array, DBUS_TYPE_STRING, &name);
            }
        }

        dbus_message_iter_close_container(&iter, &array);
    }
    else
    {
        dbus_message_unref(reply);

        return Unknown(bridge, message);
    }

    return Answer(bridge, message, reply);
}

static int Perform(AtspiBridge* bridge, int identifier, int action, const char* value)
{
    if (bridge->actionCallBack == NULL || identifier == APP_NODE)
    {
        return 0;
    }

    return bridge->actionCallBack(identifier, action, value);
}

static DBusHandlerResult Component(AtspiBridge* bridge, DBusMessage* message, int identifier)
{
    const char* member = dbus_message_get_member(message);
    DBusMessage* reply = dbus_message_new_method_return(message);

    if (reply == NULL)
    {
        return DBUS_HANDLER_RESULT_NEED_MEMORY;
    }

    DBusMessageIter iter;
    DBusError error;

    dbus_message_iter_init_append(reply, &iter);
    dbus_error_init(&error);

    if (strcmp(member, "GetExtents") == 0)
    {
        DBusMessageIter structure;
        dbus_uint32_t coordType = COORD_SCREEN;
        int box[4];

        dbus_message_get_args(message, &error, DBUS_TYPE_UINT32, &coordType, DBUS_TYPE_INVALID);
        ExtentsOf(bridge, identifier, (int)coordType, box);

        dbus_message_iter_open_container(&iter, DBUS_TYPE_STRUCT, NULL, &structure);

        for (int index = 0; index < 4; index++)
        {
            dbus_int32_t value = box[index];

            dbus_message_iter_append_basic(&structure, DBUS_TYPE_INT32, &value);
        }

        dbus_message_iter_close_container(&iter, &structure);
    }
    else if (strcmp(member, "GetPosition") == 0)
    {
        dbus_uint32_t coordType = COORD_SCREEN;
        int box[4];

        dbus_message_get_args(message, &error, DBUS_TYPE_UINT32, &coordType, DBUS_TYPE_INVALID);
        ExtentsOf(bridge, identifier, (int)coordType, box);

        dbus_int32_t x = box[0];
        dbus_int32_t y = box[1];

        dbus_message_iter_append_basic(&iter, DBUS_TYPE_INT32, &x);
        dbus_message_iter_append_basic(&iter, DBUS_TYPE_INT32, &y);
    }
    else if (strcmp(member, "GetSize") == 0)
    {
        int box[4];

        ExtentsOf(bridge, identifier, 1, box);

        dbus_int32_t width = box[2];
        dbus_int32_t height = box[3];

        dbus_message_iter_append_basic(&iter, DBUS_TYPE_INT32, &width);
        dbus_message_iter_append_basic(&iter, DBUS_TYPE_INT32, &height);
    }
    else if (strcmp(member, "Contains") == 0)
    {
        dbus_int32_t x = 0;
        dbus_int32_t y = 0;
        dbus_uint32_t coordType = COORD_SCREEN;
        int box[4];

        dbus_message_get_args(message, &error, DBUS_TYPE_INT32, &x, DBUS_TYPE_INT32, &y,
            DBUS_TYPE_UINT32, &coordType, DBUS_TYPE_INVALID);
        ExtentsOf(bridge, identifier, (int)coordType, box);

        dbus_bool_t inside = x >= box[0] && y >= box[1]
            && x < box[0] + box[2] && y < box[1] + box[3];

        dbus_message_iter_append_basic(&iter, DBUS_TYPE_BOOLEAN, &inside);
    }
    else if (strcmp(member, "GetAccessibleAtPoint") == 0)
    {
        dbus_int32_t x = 0;
        dbus_int32_t y = 0;
        dbus_uint32_t coordType = COORD_SCREEN;

        dbus_message_get_args(message, &error, DBUS_TYPE_INT32, &x, DBUS_TYPE_INT32, &y,
            DBUS_TYPE_UINT32, &coordType, DBUS_TYPE_INVALID);

        int localX = (int)x;
        int localY = (int)y;

        if ((int)coordType == COORD_SCREEN)
        {
            localX -= bridge->originX;
            localY -= bridge->originY;
        }

        int hit = DeepestAt(bridge, bridge->rootId, localX, localY);

        if (hit == NO_NODE)
        {
            AppendNullRef(&iter);
        }
        else
        {
            AppendSelfRef(bridge, &iter, hit);
        }
    }
    else if (strcmp(member, "GrabFocus") == 0)
    {
        dbus_bool_t done = Perform(bridge, identifier, IXEN_AXA_FOCUS, NULL) != 0;

        dbus_message_iter_append_basic(&iter, DBUS_TYPE_BOOLEAN, &done);
    }
    else if (strcmp(member, "ScrollTo") == 0 || strcmp(member, "ScrollToPoint") == 0)
    {
        dbus_bool_t done = Perform(bridge, identifier, IXEN_AXA_SCROLL, NULL) != 0;

        dbus_message_iter_append_basic(&iter, DBUS_TYPE_BOOLEAN, &done);
    }
    else if (strcmp(member, "GetLayer") == 0)
    {
        dbus_uint32_t layer = 3;

        dbus_message_iter_append_basic(&iter, DBUS_TYPE_UINT32, &layer);
    }
    else if (strcmp(member, "GetMDIZOrder") == 0)
    {
        dbus_int16_t order = 0;

        dbus_message_iter_append_basic(&iter, DBUS_TYPE_INT16, &order);
    }
    else if (strcmp(member, "GetAlpha") == 0)
    {
        double alpha = 1.0;

        dbus_message_iter_append_basic(&iter, DBUS_TYPE_DOUBLE, &alpha);
    }
    else
    {
        dbus_error_free(&error);
        dbus_message_unref(reply);

        return Unknown(bridge, message);
    }

    dbus_error_free(&error);

    return Answer(bridge, message, reply);
}

static DBusHandlerResult Action(AtspiBridge* bridge, DBusMessage* message, int identifier)
{
    const char* member = dbus_message_get_member(message);
    AtspiNode* node = identifier == APP_NODE ? NULL : NodeAt(bridge, identifier);

    if (node == NULL)
    {
        return Unknown(bridge, message);
    }

    DBusMessage* reply = dbus_message_new_method_return(message);

    if (reply == NULL)
    {
        return DBUS_HANDLER_RESULT_NEED_MEMORY;
    }

    DBusMessageIter iter;

    dbus_message_iter_init_append(reply, &iter);

    if (strcmp(member, "DoAction") == 0)
    {
        dbus_bool_t done = Perform(bridge, identifier, IXEN_AXA_INVOKE, NULL) != 0;

        dbus_message_iter_append_basic(&iter, DBUS_TYPE_BOOLEAN, &done);
    }
    else if (strcmp(member, "GetName") == 0 || strcmp(member, "GetLocalizedName") == 0)
    {
        const char* name = "click";

        dbus_message_iter_append_basic(&iter, DBUS_TYPE_STRING, &name);
    }
    else if (strcmp(member, "GetDescription") == 0)
    {
        const char* description = Text(node->description);

        dbus_message_iter_append_basic(&iter, DBUS_TYPE_STRING, &description);
    }
    else if (strcmp(member, "GetKeyBinding") == 0)
    {
        const char* shortcut = Text(node->shortcut);

        dbus_message_iter_append_basic(&iter, DBUS_TYPE_STRING, &shortcut);
    }
    else if (strcmp(member, "GetActions") == 0)
    {
        DBusMessageIter array;

        dbus_message_iter_open_container(&iter, DBUS_TYPE_ARRAY, "(sss)", &array);

        if (HasActions(bridge, identifier))
        {
            DBusMessageIter structure;
            const char* name = "click";
            const char* description = Text(node->description);
            const char* shortcut = Text(node->shortcut);

            dbus_message_iter_open_container(&array, DBUS_TYPE_STRUCT, NULL, &structure);
            dbus_message_iter_append_basic(&structure, DBUS_TYPE_STRING, &name);
            dbus_message_iter_append_basic(&structure, DBUS_TYPE_STRING, &description);
            dbus_message_iter_append_basic(&structure, DBUS_TYPE_STRING, &shortcut);
            dbus_message_iter_close_container(&array, &structure);
        }

        dbus_message_iter_close_container(&iter, &array);
    }
    else
    {
        dbus_message_unref(reply);

        return Unknown(bridge, message);
    }

    return Answer(bridge, message, reply);
}

static DBusHandlerResult TextInterface(AtspiBridge* bridge, DBusMessage* message, int identifier)
{
    const char* member = dbus_message_get_member(message);
    const char* value = ValueOf(bridge, identifier);
    int length = (int)strlen(value);
    DBusMessage* reply = dbus_message_new_method_return(message);

    if (reply == NULL)
    {
        return DBUS_HANDLER_RESULT_NEED_MEMORY;
    }

    DBusMessageIter iter;
    DBusError error;

    dbus_message_iter_init_append(reply, &iter);
    dbus_error_init(&error);

    if (strcmp(member, "GetText") == 0)
    {
        dbus_int32_t start = 0;
        dbus_int32_t end = length;

        dbus_message_get_args(message, &error, DBUS_TYPE_INT32, &start, DBUS_TYPE_INT32, &end,
            DBUS_TYPE_INVALID);

        if (start < 0)
        {
            start = 0;
        }

        if (end < 0 || end > length)
        {
            end = length;
        }

        if (end < start)
        {
            end = start;
        }

        char* slice = (char*)malloc((size_t)(end - start) + 1);

        if (slice == NULL)
        {
            dbus_error_free(&error);
            dbus_message_unref(reply);

            return DBUS_HANDLER_RESULT_NEED_MEMORY;
        }

        memcpy(slice, value + start, (size_t)(end - start));
        slice[end - start] = '\0';

        const char* text = slice;

        dbus_message_iter_append_basic(&iter, DBUS_TYPE_STRING, &text);
        free(slice);
    }
    else if (strcmp(member, "GetTextAtOffset") == 0 || strcmp(member, "GetStringAtOffset") == 0)
    {
        dbus_int32_t start = 0;
        dbus_int32_t end = length;

        dbus_message_iter_append_basic(&iter, DBUS_TYPE_STRING, &value);
        dbus_message_iter_append_basic(&iter, DBUS_TYPE_INT32, &start);
        dbus_message_iter_append_basic(&iter, DBUS_TYPE_INT32, &end);
    }
    else if (strcmp(member, "GetCharacterAtOffset") == 0)
    {
        dbus_int32_t offset = 0;

        dbus_message_get_args(message, &error, DBUS_TYPE_INT32, &offset, DBUS_TYPE_INVALID);

        dbus_int32_t character = offset >= 0 && offset < length ? (unsigned char)value[offset] : 0;

        dbus_message_iter_append_basic(&iter, DBUS_TYPE_INT32, &character);
    }
    else if (strcmp(member, "GetNSelections") == 0)
    {
        dbus_int32_t count = 0;

        dbus_message_iter_append_basic(&iter, DBUS_TYPE_INT32, &count);
    }
    else
    {
        dbus_error_free(&error);
        dbus_message_unref(reply);

        return Unknown(bridge, message);
    }

    dbus_error_free(&error);

    return Answer(bridge, message, reply);
}

static DBusHandlerResult Editable(AtspiBridge* bridge, DBusMessage* message, int identifier)
{
    if (strcmp(dbus_message_get_member(message), "SetTextContents") != 0)
    {
        return Unknown(bridge, message);
    }

    DBusError error;
    const char* value = NULL;

    dbus_error_init(&error);

    if (!dbus_message_get_args(message, &error, DBUS_TYPE_STRING, &value, DBUS_TYPE_INVALID))
    {
        dbus_error_free(&error);

        return Unknown(bridge, message);
    }

    dbus_error_free(&error);

    dbus_bool_t done = Perform(bridge, identifier, IXEN_AXA_SET_VALUE, value) != 0;
    DBusMessage* reply = dbus_message_new_method_return(message);

    if (reply == NULL)
    {
        return DBUS_HANDLER_RESULT_NEED_MEMORY;
    }

    DBusMessageIter iter;

    dbus_message_iter_init_append(reply, &iter);
    dbus_message_iter_append_basic(&iter, DBUS_TYPE_BOOLEAN, &done);

    return Answer(bridge, message, reply);
}

static DBusHandlerResult Introspect(AtspiBridge* bridge, DBusMessage* message, int identifier)
{
    const char* body =
        "<node>"
        "<interface name=\"org.a11y.atspi.Accessible\"/>"
        "<interface name=\"org.a11y.atspi.Component\"/>"
        "<interface name=\"org.a11y.atspi.Action\"/>"
        "<interface name=\"org.a11y.atspi.Text\"/>"
        "<interface name=\"org.a11y.atspi.EditableText\"/>"
        "<interface name=\"org.a11y.atspi.Application\"/>"
        "<interface name=\"org.freedesktop.DBus.Properties\"/>"
        "</node>";

    (void)identifier;

    DBusMessage* reply = dbus_message_new_method_return(message);

    if (reply == NULL)
    {
        return DBUS_HANDLER_RESULT_NEED_MEMORY;
    }

    DBusMessageIter iter;

    dbus_message_iter_init_append(reply, &iter);
    dbus_message_iter_append_basic(&iter, DBUS_TYPE_STRING, &body);

    return Answer(bridge, message, reply);
}

static DBusHandlerResult Cache(DBusConnection* connection, DBusMessage* message, void* data)
{
    AtspiBridge* bridge = (AtspiBridge*)data;

    (void)connection;

    if (dbus_message_get_type(message) != DBUS_MESSAGE_TYPE_METHOD_CALL)
    {
        return DBUS_HANDLER_RESULT_NOT_YET_HANDLED;
    }

    const char* interface = dbus_message_get_interface(message);

    if (interface == NULL || strcmp(interface, IFACE_CACHE) != 0
        || strcmp(dbus_message_get_member(message), "GetItems") != 0)
    {
        return Unknown(bridge, message);
    }

    DBusMessage* reply = dbus_message_new_method_return(message);

    if (reply == NULL)
    {
        return DBUS_HANDLER_RESULT_NEED_MEMORY;
    }

    DBusMessageIter iter;
    DBusMessageIter array;

    dbus_message_iter_init_append(reply, &iter);
    dbus_message_iter_open_container(&iter, DBUS_TYPE_ARRAY, CACHE_ITEM, &array);
    dbus_message_iter_close_container(&iter, &array);

    return Answer(bridge, message, reply);
}

static DBusHandlerResult Handle(DBusConnection* connection, DBusMessage* message, void* data)
{
    AtspiBridge* bridge = (AtspiBridge*)data;

    (void)connection;

    if (dbus_message_get_type(message) != DBUS_MESSAGE_TYPE_METHOD_CALL)
    {
        return DBUS_HANDLER_RESULT_NOT_YET_HANDLED;
    }

    int identifier = NodeOfPath(bridge, dbus_message_get_path(message));

    if (identifier == NO_NODE)
    {
        return Unknown(bridge, message);
    }

    const char* interface = dbus_message_get_interface(message);

    if (interface == NULL)
    {
        return Unknown(bridge, message);
    }

    if (strcmp(interface, IFACE_PROPERTIES) == 0)
    {
        return Properties(bridge, message, identifier);
    }

    if (strcmp(interface, IFACE_ACCESSIBLE) == 0)
    {
        return Accessible(bridge, message, identifier);
    }

    if (strcmp(interface, IFACE_COMPONENT) == 0)
    {
        return Component(bridge, message, identifier);
    }

    if (strcmp(interface, IFACE_ACTION) == 0)
    {
        return Action(bridge, message, identifier);
    }

    if (strcmp(interface, IFACE_TEXT) == 0)
    {
        return TextInterface(bridge, message, identifier);
    }

    if (strcmp(interface, IFACE_EDITABLE) == 0)
    {
        return Editable(bridge, message, identifier);
    }

    if (strcmp(interface, IFACE_INTROSPECTABLE) == 0)
    {
        return Introspect(bridge, message, identifier);
    }

    return Unknown(bridge, message);
}

static void ReadEnabled(AtspiBridge* bridge)
{
    if (bridge->session == NULL)
    {
        return;
    }

    DBusMessage* call = dbus_message_new_method_call(A11Y_BUS_NAME, A11Y_BUS_PATH,
        IFACE_PROPERTIES, "Get");

    if (call == NULL)
    {
        return;
    }

    const char* interface = A11Y_STATUS_IFACE;
    const char* property = "IsEnabled";

    dbus_message_append_args(call, DBUS_TYPE_STRING, &interface, DBUS_TYPE_STRING, &property,
        DBUS_TYPE_INVALID);

    DBusError error;

    dbus_error_init(&error);

    DBusMessage* reply = dbus_connection_send_with_reply_and_block(bridge->session, call,
        CALL_TIMEOUT_MS, &error);

    dbus_message_unref(call);

    if (reply == NULL)
    {
        dbus_error_free(&error);

        return;
    }

    DBusMessageIter iter;
    DBusMessageIter variant;
    dbus_bool_t enabled = FALSE;

    if (dbus_message_iter_init(reply, &iter)
        && dbus_message_iter_get_arg_type(&iter) == DBUS_TYPE_VARIANT)
    {
        dbus_message_iter_recurse(&iter, &variant);

        if (dbus_message_iter_get_arg_type(&variant) == DBUS_TYPE_BOOLEAN)
        {
            dbus_message_iter_get_basic(&variant, &enabled);
        }
    }

    dbus_message_unref(reply);
    dbus_error_free(&error);

    bridge->enabled = enabled ? 1 : 0;
}

static DBusHandlerResult Watch(DBusConnection* connection, DBusMessage* message, void* data)
{
    AtspiBridge* bridge = (AtspiBridge*)data;

    (void)connection;

    if (dbus_message_is_signal(message, IFACE_PROPERTIES, "PropertiesChanged"))
    {
        ReadEnabled(bridge);
    }

    return DBUS_HANDLER_RESULT_NOT_YET_HANDLED;
}

static char* BusAddress(AtspiBridge* bridge)
{
    DBusMessage* call = dbus_message_new_method_call(A11Y_BUS_NAME, A11Y_BUS_PATH,
        A11Y_BUS_IFACE, "GetAddress");

    if (call == NULL)
    {
        return NULL;
    }

    DBusError error;

    dbus_error_init(&error);

    DBusMessage* reply = dbus_connection_send_with_reply_and_block(bridge->session, call,
        CALL_TIMEOUT_MS, &error);

    dbus_message_unref(call);

    if (reply == NULL)
    {
        dbus_error_free(&error);

        return NULL;
    }

    const char* address = NULL;
    char* copy = NULL;

    if (dbus_message_get_args(reply, &error, DBUS_TYPE_STRING, &address, DBUS_TYPE_INVALID))
    {
        copy = Copy(address);
    }

    dbus_message_unref(reply);
    dbus_error_free(&error);

    return copy;
}

static void Embed(AtspiBridge* bridge)
{
    DBusMessage* call = dbus_message_new_method_call(REGISTRY_NAME, ROOT_PATH,
        IFACE_SOCKET, "Embed");

    if (call == NULL)
    {
        return;
    }

    DBusMessageIter iter;

    dbus_message_iter_init_append(call, &iter);
    AppendSelfRef(bridge, &iter, APP_NODE);

    DBusError error;

    dbus_error_init(&error);

    DBusMessage* reply = dbus_connection_send_with_reply_and_block(bridge->bus, call,
        CALL_TIMEOUT_MS, &error);

    dbus_message_unref(call);

    if (reply == NULL)
    {
        dbus_error_free(&error);

        return;
    }

    DBusMessageIter answer;
    DBusMessageIter structure;

    if (dbus_message_iter_init(reply, &answer)
        && dbus_message_iter_get_arg_type(&answer) == DBUS_TYPE_STRUCT)
    {
        const char* name = NULL;
        const char* path = NULL;

        dbus_message_iter_recurse(&answer, &structure);
        dbus_message_iter_get_basic(&structure, &name);
        dbus_message_iter_next(&structure);
        dbus_message_iter_get_basic(&structure, &path);

        free(bridge->parentName);
        free(bridge->parentPath);

        bridge->parentName = Copy(name);
        bridge->parentPath = Copy(path);
    }

    dbus_message_unref(reply);
    dbus_error_free(&error);
}

static int EnsureRegistered(AtspiBridge* bridge)
{
    if (bridge->registered)
    {
        return 1;
    }

    if (!bridge->enabled || bridge->session == NULL)
    {
        return 0;
    }

    char* address = BusAddress(bridge);

    if (address == NULL)
    {
        return 0;
    }

    DBusError error;

    dbus_error_init(&error);

    bridge->bus = dbus_connection_open_private(address, &error);

    free(address);

    if (bridge->bus == NULL)
    {
        dbus_error_free(&error);

        return 0;
    }

    dbus_connection_set_exit_on_disconnect(bridge->bus, FALSE);

    if (!dbus_bus_register(bridge->bus, &error))
    {
        dbus_error_free(&error);
        dbus_connection_close(bridge->bus);
        dbus_connection_unref(bridge->bus);
        bridge->bus = NULL;

        return 0;
    }

    dbus_error_free(&error);

    DBusObjectPathVTable vtable;

    memset(&vtable, 0, sizeof(vtable));
    vtable.message_function = Handle;

    if (!dbus_connection_register_fallback(bridge->bus, "/org/a11y/atspi/accessible", &vtable, bridge))
    {
        dbus_connection_close(bridge->bus);
        dbus_connection_unref(bridge->bus);
        bridge->bus = NULL;

        return 0;
    }

    DBusObjectPathVTable cache;

    memset(&cache, 0, sizeof(cache));
    cache.message_function = Cache;

    dbus_connection_register_object_path(bridge->bus, CACHE_PATH, &cache, bridge);

    Embed(bridge);

    bridge->registered = 1;

    return 1;
}

AtspiBridge* Atspi_Create(void)
{
    AtspiBridge* bridge = (AtspiBridge*)calloc(1, sizeof(AtspiBridge));

    if (bridge == NULL)
    {
        return NULL;
    }

    bridge->rootId = NO_NODE;

    DBusError error;

    dbus_error_init(&error);

    bridge->session = dbus_bus_get_private(DBUS_BUS_SESSION, &error);

    if (bridge->session == NULL)
    {
        dbus_error_free(&error);

        return bridge;
    }

    dbus_connection_set_exit_on_disconnect(bridge->session, FALSE);

    dbus_bus_add_match(bridge->session,
        "type='signal',interface='org.freedesktop.DBus.Properties',member='PropertiesChanged'",
        &error);

    dbus_error_free(&error);

    dbus_connection_add_filter(bridge->session, Watch, bridge, NULL);

    ReadEnabled(bridge);

    return bridge;
}

void Atspi_Destroy(AtspiBridge* bridge)
{
    if (bridge == NULL)
    {
        return;
    }

    for (int index = 0; index < bridge->capacity; index++)
    {
        if (bridge->nodes[index].used)
        {
            ReleaseNode(&bridge->nodes[index]);
        }
    }

    free(bridge->nodes);
    free(bridge->parentName);
    free(bridge->parentPath);
    free(bridge->title);

    if (bridge->bus != NULL)
    {
        dbus_connection_close(bridge->bus);
        dbus_connection_unref(bridge->bus);
    }

    if (bridge->session != NULL)
    {
        dbus_connection_remove_filter(bridge->session, Watch, bridge);
        dbus_connection_close(bridge->session);
        dbus_connection_unref(bridge->session);
    }

    free(bridge);
}

int Atspi_IsActive(AtspiBridge* bridge)
{
    return bridge != NULL && bridge->enabled;
}

int Atspi_Fds(AtspiBridge* bridge, int* fds, int max)
{
    int count = 0;

    if (bridge == NULL)
    {
        return 0;
    }

    int fd = -1;

    if (bridge->session != NULL && count < max
        && dbus_connection_get_unix_fd(bridge->session, &fd) && fd >= 0)
    {
        fds[count++] = fd;
    }

    if (bridge->bus != NULL && count < max
        && dbus_connection_get_unix_fd(bridge->bus, &fd) && fd >= 0)
    {
        fds[count++] = fd;
    }

    return count;
}

void Atspi_Pump(AtspiBridge* bridge)
{
    if (bridge == NULL)
    {
        return;
    }

    if (bridge->session != NULL)
    {
        while (dbus_connection_read_write_dispatch(bridge->session, 0)
            && dbus_connection_get_dispatch_status(bridge->session) == DBUS_DISPATCH_DATA_REMAINS)
        {
        }
    }

    if (bridge->bus != NULL)
    {
        while (dbus_connection_read_write_dispatch(bridge->bus, 0)
            && dbus_connection_get_dispatch_status(bridge->bus) == DBUS_DISPATCH_DATA_REMAINS)
        {
        }
    }
}

void Atspi_SetTitle(AtspiBridge* bridge, const char* title)
{
    if (bridge == NULL)
    {
        return;
    }

    free(bridge->title);

    bridge->title = Copy(title);
}

void Atspi_SetOrigin(AtspiBridge* bridge, int x, int y)
{
    if (bridge == NULL)
    {
        return;
    }

    bridge->originX = x;
    bridge->originY = y;
}

void Atspi_RegisterCallBack(AtspiBridge* bridge, int callBack(int, int, const char*))
{
    if (bridge == NULL)
    {
        return;
    }

    bridge->actionCallBack = callBack;
}

void Atspi_UpdateNode(AtspiBridge* bridge, int identifier, int parent, int role,
    long long states, int actions, int x, int y, int width, int height,
    const char* name, const char* description, const char* value, const char* shortcut)
{
    if (bridge == NULL || identifier < 0 || !EnsureRegistered(bridge))
    {
        return;
    }

    if (!EnsureCapacity(bridge, identifier))
    {
        return;
    }

    AtspiNode* node = &bridge->nodes[identifier];
    int* children = node->children;
    int childCount = node->childCount;
    int childCapacity = node->childCapacity;

    free(node->name);
    free(node->description);
    free(node->value);
    free(node->shortcut);

    memset(node, 0, sizeof(AtspiNode));

    node->used = 1;
    node->parent = parent;
    node->role = role;
    node->states = (unsigned long long)states;
    node->actions = actions;
    node->x = x;
    node->y = y;
    node->width = width;
    node->height = height;
    node->name = Copy(name);
    node->description = Copy(description);
    node->value = Copy(value);
    node->shortcut = Copy(shortcut);
    node->children = children;
    node->childCount = childCount;
    node->childCapacity = childCapacity;
}

static void AddChild(AtspiNode* node, int child)
{
    if (node->childCount == node->childCapacity)
    {
        int wanted = node->childCapacity == 0 ? 4 : node->childCapacity * 2;
        int* grown = (int*)realloc(node->children, (size_t)wanted * sizeof(int));

        if (grown == NULL)
        {
            return;
        }

        node->children = grown;
        node->childCapacity = wanted;
    }

    node->children[node->childCount++] = child;
}

void Atspi_Commit(AtspiBridge* bridge, int root, const int* order, int count)
{
    if (bridge == NULL || !EnsureRegistered(bridge))
    {
        return;
    }

    bridge->generation++;

    for (int index = 0; index < bridge->capacity; index++)
    {
        bridge->nodes[index].childCount = 0;
    }

    for (int index = 0; index < count; index++)
    {
        AtspiNode* node = NodeAt(bridge, order[index]);

        if (node != NULL)
        {
            node->generation = bridge->generation;
        }
    }

    for (int index = 0; index < count; index++)
    {
        int identifier = order[index];
        AtspiNode* node = NodeAt(bridge, identifier);

        if (node == NULL || identifier == root)
        {
            continue;
        }

        AtspiNode* parent = NodeAt(bridge, node->parent);

        if (parent == NULL)
        {
            continue;
        }

        node->indexInParent = parent->childCount;

        AddChild(parent, identifier);
    }

    for (int index = 0; index < bridge->capacity; index++)
    {
        AtspiNode* node = &bridge->nodes[index];

        if (!node->used)
        {
            continue;
        }

        if (node->generation != bridge->generation)
        {
            ReleaseNode(node);
        }
    }

    bridge->rootId = root;

    dbus_connection_flush(bridge->bus);
}

void Atspi_Notify(AtspiBridge* bridge, int identifier, int kind, const char* text)
{
    if (bridge == NULL || !bridge->registered || bridge->bus == NULL)
    {
        return;
    }

    char path[64];

    if (NodeAt(bridge, identifier) == NULL)
    {
        return;
    }

    PathOf(identifier, path, sizeof(path));

    const char* member = NULL;
    const char* detail = "";
    dbus_int32_t first = 0;
    dbus_int32_t second = 0;

    switch (kind)
    {
        case IXEN_AXN_NAME:
            member = "PropertyChange";
            detail = "accessible-name";
            break;

        case IXEN_AXN_VALUE:
            member = "PropertyChange";
            detail = "accessible-value";
            break;

        case IXEN_AXN_FOCUS:
            member = "StateChanged";
            detail = "focused";
            first = 1;
            break;

        case IXEN_AXN_STRUCTURE:
            member = "ChildrenChanged";
            detail = "add";
            break;

        case IXEN_AXN_ANNOUNCE:
            member = "Announcement";
            first = 1;
            break;

        case IXEN_AXN_ANNOUNCE_URGENT:
            member = "Announcement";
            first = 2;
            break;

        default:
            return;
    }

    DBusMessage* signal = dbus_message_new_signal(path, IFACE_EVENT_OBJECT, member);

    if (signal == NULL)
    {
        return;
    }

    DBusMessageIter iter;
    DBusMessageIter properties;

    dbus_message_iter_init_append(signal, &iter);
    dbus_message_iter_append_basic(&iter, DBUS_TYPE_STRING, &detail);
    dbus_message_iter_append_basic(&iter, DBUS_TYPE_INT32, &first);
    dbus_message_iter_append_basic(&iter, DBUS_TYPE_INT32, &second);

    if (kind == IXEN_AXN_STRUCTURE)
    {
        AppendVariantRef(bridge, &iter, identifier);
    }
    else if (kind == IXEN_AXN_ANNOUNCE || kind == IXEN_AXN_ANNOUNCE_URGENT)
    {
        AppendVariantString(&iter, Text(text));
    }
    else if (kind == IXEN_AXN_NAME)
    {
        AppendVariantString(&iter, NameOf(bridge, identifier));
    }
    else if (kind == IXEN_AXN_VALUE)
    {
        AppendVariantString(&iter, ValueOf(bridge, identifier));
    }
    else
    {
        AppendVariantString(&iter, "0");
    }

    dbus_message_iter_open_container(&iter, DBUS_TYPE_ARRAY, "{sv}", &properties);
    dbus_message_iter_close_container(&iter, &properties);

    Send(bridge, signal);

    dbus_connection_flush(bridge->bus);
}
