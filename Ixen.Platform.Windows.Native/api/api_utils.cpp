#include "api_utils.h"

LPWSTR PreMarshalString(LPWSTR string)
{
	if (!string)
	{
		return nullptr;
	}

	size_t ulSize = ((wcslen(string) + 1) * sizeof(wchar_t));
	wchar_t* pwszReturn = nullptr;

	pwszReturn = (wchar_t*)::CoTaskMemAlloc(ulSize);

	if (pwszReturn)
	{
		wcscpy_s(pwszReturn, ulSize - 1, string);;
	}

	return pwszReturn;
}