#pragma once

#include "CoreMinimal.h"
#include "UObject/ObjectMacros.h"
#include "UObject/Object.h"
#include "Factories/ImportSettings.h"
#include "JanusImporterIncludes.h"
#include "JanusExporterUI.generated.h"

UCLASS(config = EditorPerProjectUserSettings, AutoExpandCategories = (FTransform), HideCategories = Object, MinimalAPI)
class UJanusExporterUI : public UObject
{
	GENERATED_UCLASS_BODY()

public:
	FString ExportPath;

	UPROPERTY(EditAnywhere, Category = AssetObjects, meta = (ClampMin = "0.0001", ClampMax = "10000000.0"))
		float ExportScale = 0.01f;

	UPROPERTY(EditAnywhere, Category = Materials, meta = (ClampMin = "32", ClampMax = "4096"))
		int MaterialResolution = 1024;
	//UPROPERTY(EditAnywhere, Category = Lightmaps)
		TEnumAsByte<JanusTextureFormat> MaterialFormat = JanusTextureFormat::PNG;
	
	//UPROPERTY(EditAnywhere, Category = Materials)
		bool ExportNormals = false;

	UPROPERTY(EditAnywhere, Category = Lightmaps)
		TEnumAsByte<JanusLightmapExportType> LightmapFormat = JanusLightmapExportType::PackedOpenEXR;
	UPROPERTY(EditAnywhere, Category = Lightmaps, meta = (ClampMin = "-5", ClampMax = "5"))
		float RelativeFStops = 0;


	/** UObject Interface */
	virtual bool CanEditChange(const UProperty* InProperty) const override;

};

