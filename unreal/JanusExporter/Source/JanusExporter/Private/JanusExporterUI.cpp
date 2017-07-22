#include "FJanusExporterModulePrivatePCH.h"
#include "JanusExporterUI.h"
#include "WinUtil.h"

UJanusExporterUI::UJanusExporterUI(const FObjectInitializer& ObjectInitializer)
	: Super(ObjectInitializer)
{
	UWinUtil util;
	ExportPath = util.GetDefaultExportPath(GConfig);
}

bool UJanusExporterUI::CanEditChange(const UProperty* InProperty) const
{
	bool bIsMutable = Super::CanEditChange(InProperty);
	return bIsMutable;
}