#pragma once

#include "CoreMinimal.h"
#include "InputCoreTypes.h"
#include "Widgets/DeclarativeSyntaxSupport.h"
#include "Input/Reply.h"
#include "Widgets/SCompoundWidget.h"
#include "Widgets/SWindow.h"
#include "JanusExporterUI.h"
//#include "JanusFbxExporter.h"

class SJanusExporterWindow : public SCompoundWidget
{
public:
	SLATE_BEGIN_ARGS(SJanusExporterWindow)
		: _FullPath()
		, _MaxWindowHeight(0.0f)
		, _MaxWindowWidth(0.0f)
	{}

		SLATE_ARGUMENT(FText, FullPath)
		SLATE_ARGUMENT(float, MaxWindowHeight)
		SLATE_ARGUMENT(float, MaxWindowWidth)
	SLATE_END_ARGS()

	void Construct(const FArguments& InArgs);
	virtual bool SupportsKeyboardFocus() const override { return true; }

	UJanusExporterUI* GetData();

	SJanusExporterWindow()
	{
		ToolData = NewObject<UJanusExporterUI>();
		//UJanusFbxExporter* Exporter = UJanusFbxExporter::GetInstance();
	}

	FReply OnBrowseDir();
	FReply DoExport();
	FReply ShowInExplorer();

private:
	UJanusExporterUI* ToolData;
	SWindow* ParentWindow;
	float MaxWindowHeight;
	float MaxWindowWidth;
};

