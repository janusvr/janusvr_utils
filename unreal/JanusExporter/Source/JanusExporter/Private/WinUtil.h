#pragma once

#include "CoreMinimal.h"

struct UWinUtil
{
public:
	UWinUtil()
	{
	}

	FString GetDefaultExportPath(FConfigCacheIni* GConfig);
};
