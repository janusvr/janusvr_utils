#include "FJanusExporterModulePrivatePCH.h"
#include "JanusExporterUI.h"

UJanusExporterUI::UJanusExporterUI(const FObjectInitializer& ObjectInitializer)
	: Super(ObjectInitializer)
{
}

bool UJanusExporterUI::CanEditChange(const UProperty* InProperty) const
{
	bool bIsMutable = Super::CanEditChange(InProperty);
	return bIsMutable;
}