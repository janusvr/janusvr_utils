#include "FJanusExporterModulePrivatePCH.h"
#include "WinUtil.h"
#include <Shlobj.h>

FString UWinUtil::GetDefaultExportPath(FConfigCacheIni* GConfig)
{
	wchar_t* DocumentsFolder = 0;
	SHGetKnownFolderPath(FOLDERID_Documents, 0, NULL, &DocumentsFolder);

	// get scene name
	int DocumentsFolderLength = wcslen(DocumentsFolder);
	FString ExportPath = FString(DocumentsFolderLength, DocumentsFolder);

	FString ProjectName;
	GConfig->GetString(
		TEXT("/Script/EngineSettings.GeneralProjectSettings"),
		TEXT("ProjectName"),
		ProjectName,
		GGameIni
	);
	if (ProjectName.IsEmpty())
	{
		ProjectName = L"Unreal Project";
	}

	ExportPath = ExportPath + L"\\JanusVR\\workspaces\\" + ProjectName + L"\\";

	CoTaskMemFree(static_cast<void*>(DocumentsFolder));

	return ExportPath;
}