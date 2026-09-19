#ifndef _ATSPI_H_
#define _ATSPI_H_

#define IXEN_AXN_NAME 0
#define IXEN_AXN_VALUE 1
#define IXEN_AXN_FOCUS 2
#define IXEN_AXN_STRUCTURE 3
#define IXEN_AXN_ANNOUNCE 4
#define IXEN_AXN_ANNOUNCE_URGENT 5

#define IXEN_AXA_INVOKE 1
#define IXEN_AXA_FOCUS 2
#define IXEN_AXA_SET_VALUE 4
#define IXEN_AXA_SCROLL 8

#define ATSPI_MAX_FDS 2

typedef struct AtspiBridge AtspiBridge;

AtspiBridge* Atspi_Create(void);
void Atspi_Destroy(AtspiBridge* bridge);

int Atspi_IsActive(AtspiBridge* bridge);
int Atspi_Fds(AtspiBridge* bridge, int* fds, int max);
void Atspi_Pump(AtspiBridge* bridge);

void Atspi_SetTitle(AtspiBridge* bridge, const char* title);
void Atspi_SetOrigin(AtspiBridge* bridge, int x, int y);
void Atspi_RegisterCallBack(AtspiBridge* bridge, int callBack(int, int, const char*));

void Atspi_UpdateNode(AtspiBridge* bridge, int identifier, int parent, int role,
    long long states, int actions, int x, int y, int width, int height,
    const char* name, const char* description, const char* value, const char* shortcut);

void Atspi_Commit(AtspiBridge* bridge, int root, const int* order, int count);
void Atspi_Notify(AtspiBridge* bridge, int identifier, int kind, const char* text);

#endif
