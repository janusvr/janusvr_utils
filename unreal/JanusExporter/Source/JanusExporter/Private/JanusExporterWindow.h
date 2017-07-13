#pragma once

#include "CoreMinimal.h"
#include "InputCoreTypes.h"
#include "Widgets/DeclarativeSyntaxSupport.h"
#include "Input/Reply.h"
#include "Widgets/SCompoundWidget.h"
#include "Widgets/SWindow.h"
#include "JanusExporterUI.h"

class SJanusExporterWindow : public SCompoundWidget
{
public:
	SLATE_BEGIN_ARGS(SJanusExporterWindow)
		: _ImportUI(NULL)
		, _FullPath()
		, _MaxWindowHeight(0.0f)
		, _MaxWindowWidth(0.0f)
	{}

		SLATE_ARGUMENT(UJanusExporterUI*, ImportUI)
		SLATE_ARGUMENT(FText, FullPath)
		SLATE_ARGUMENT(float, MaxWindowHeight)
		SLATE_ARGUMENT(float, MaxWindowWidth)
	SLATE_END_ARGS()

	void Construct(const FArguments& InArgs);
	virtual bool SupportsKeyboardFocus() const override { return true; }
	
	SJanusExporterWindow()
	{}

private:
	UJanusExporterUI* ImportUI;
	TWeakPtr< SWindow > WidgetWindow;
	FText FullPath;
	float MaxWindowHeight;
	float MaxWindowWidth;
	//TSharedPtr< SButton > ImportButton;
};

