#pragma once

#include "CoreMinimal.h"
#include "UObject/ObjectMacros.h"
#include "UObject/Object.h"
#include "Factories/ImportSettings.h"
#include "JanusExporterUI.generated.h"

UCLASS(config = EditorPerProjectUserSettings, AutoExpandCategories = (FTransform), HideCategories = Object, MinimalAPI)
class UJanusExporterUI : public UObject
{
	GENERATED_UCLASS_BODY()

public:
	UPROPERTY()
	bool bIsObjImport;

	/** UObject Interface */
	virtual bool CanEditChange(const UProperty* InProperty) const override;

};

