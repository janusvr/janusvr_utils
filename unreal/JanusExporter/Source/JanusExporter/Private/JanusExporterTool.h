#pragma once

#include "BaseEditorTool.h"
#include "JanusExporterTool.generated.h"

UCLASS(Blueprintable)
class UJanusExporterTool : public UBaseEditorTool
{
	GENERATED_BODY()

public:
	UJanusExporterTool();

	UPROPERTY(EditAnywhere, Category = "Settings")
		FString ExportPath;

	UPROPERTY(EditAnywhere, Category = "Settings")
		float UniformScale = 0.01f;

	UFUNCTION(Exec, Category = "Settings")
		void SearchForExport();

	UFUNCTION(Exec)
		void Export();
};
