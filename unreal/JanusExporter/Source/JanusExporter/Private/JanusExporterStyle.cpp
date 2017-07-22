#include "FJanusExporterModulePrivatePCH.h"
#include "JanusExporterStyle.h"

void FJanusExporterStyle::Initialize()
{
	// Only register once
	if (StyleSet.IsValid())
	{
		return;
	}

	StyleSet = MakeShareable(new FSlateStyleSet(GetStyleSetName()));

	FSlateStyleRegistry::RegisterSlateStyle(*StyleSet.Get());
}

void FJanusExporterStyle::Shutdown()
{
	if (StyleSet.IsValid())
	{
		FSlateStyleRegistry::UnRegisterSlateStyle(*StyleSet.Get());
		ensure(StyleSet.IsUnique());
		StyleSet.Reset();
	}
}

TSharedPtr< class FSlateStyleSet > FJanusExporterStyle::StyleSet = nullptr;

TSharedPtr< class ISlateStyle > FJanusExporterStyle::Get()
{
	return StyleSet;
}

FName FJanusExporterStyle::GetStyleSetName()
{
	return TEXT("DemoStyle");
}
