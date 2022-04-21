#ifndef _API_UTILS_H_
#define _API_UTILS_H_

#include <wtypes.h>

#define IXEN_API_ENTRY extern "C" __declspec(dllexport)

LPWSTR PreMarshalString(LPWSTR string);

#endif