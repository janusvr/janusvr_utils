#include "FJanusExporterModulePrivatePCH.h"
#include "JanusExporterWindow.h"
#include "ScopedTransaction.h"
#include "EngineUtils.h"
#include "Runtime/Engine/Classes/Components/StaticMeshComponent.h"
#include "Runtime/Engine/Classes/Engine/World.h"
#include "Runtime/Engine/Public/LightMap.h"
#include "Runtime/Engine/Public/ShadowMap.h"
#include "Editor/UnrealEd/Public/ObjectTools.h"
#include "Editor/UnrealEd/Public/EditorDirectories.h"
#include "Editor/PropertyEditor/Public/PropertyEditorModule.h"
#include "Editor/MainFrame/Public/Interfaces/IMainFrameModule.h"
#include "Developer/DesktopPlatform/Public/DesktopPlatformModule.h"
#include "Runtime/Core/Public/Misc/CoreMisc.h"
#include "Editor/UnrealEd/Classes/Exporters/StaticMeshExporterFBX.h"
#include "Editor/UnrealEd/Classes/Exporters/TextureExporterTGA.h"
#include "Editor/UnrealEd/Public/BusyCursor.h"
#include "Runtime/Engine/Public/ImageUtils.h"
#include "Runtime/Engine/Public/TextureResource.h"
#include "Runtime/Engine/Public/CanvasTypes.h"
#include "Runtime/Engine/Classes/Engine/TextureRenderTarget2D.h"
#include "Runtime/Engine/Public/TextureResource.h"
#include "Runtime/Engine/Classes/Materials/MaterialInstanceDynamic.h"
#include "Runtime/Engine/Public/HitProxies.h"
#include "Runtime/Engine/Public/MaterialShared.h"
#include "Developer/MaterialUtilities/Public/MaterialUtilities.h"

#include "Editor/UnrealEd/Private/StaticLightingSystem/StaticLightingPrivate.h"
#include "LightMap.h"
#include "Engine/MapBuildDataRegistry.h"
#include "Components/ModelComponent.h"
#include "Engine/StaticMesh.h"

THIRD_PARTY_INCLUDES_START
#include "ThirdParty/openexr/Deploy/include/ImfIO.h"
#include "ThirdParty/openexr/Deploy/include/ImathBox.h"
#include "ThirdParty/openexr/Deploy/include/ImfChannelList.h"
#include "ThirdParty/openexr/Deploy/include/ImfInputFile.h"
#include "ThirdParty/openexr/Deploy/include/ImfOutputFile.h"
#include "ThirdParty/openexr/Deploy/include/ImfArray.h"
#include "ThirdParty/openexr/Deploy/include/ImfHeader.h"
#include "ThirdParty/openexr/Deploy/include/ImfStdIO.h"
#include "ThirdParty/openexr/Deploy/include/ImfChannelList.h"
#include "ThirdParty/openexr/Deploy/include/ImfRgbaFile.h"
#include <fbxsdk.h>
THIRD_PARTY_INCLUDES_END


#define LOCTEXT_NAMESPACE "JanusExporter"
#define NO_CUSTOM_SOURCE 1 // remove this if you have the fixed source code that exports materials

// Fbx Exporter does not expose the fbxsdk parameters
#define private public
#include "Editor/UnrealEd/Private/FbxExporter.h"
#undef private

struct LightmappedObjects
{
	TArray<AActor*> Actors;
	FLightMap2D* LightMap;
};

UJanusExporterUI* SJanusExporterWindow::GetData()
{
	return ToolData;
}

JanusTextureFormat GetTextureFormatFromLightmap(JanusLightmapExportType LightmapType)
{
	switch (LightmapType)
	{
	case JanusLightmapExportType::PackedOpenEXR:
		return JanusTextureFormat::OpenEXR;
	default:
		return JanusTextureFormat::PNG;
	}
}

void SJanusExporterWindow::Construct(const FArguments& InArgs)
{
	TSharedPtr<SBox> InspectorBox;
	this->ChildSlot
		[
			SNew(SBox)
			//.MaxDesiredHeight(InArgs._MaxWindowWidth)
		.MaxDesiredWidth(450.0f)
		[
			SNew(SVerticalBox)
			+ SVerticalBox::Slot()
		.AutoHeight()
		.Padding(2)
		[
			SAssignNew(InspectorBox, SBox)
			.MaxDesiredHeight(650.0f)
		.WidthOverride(400.0f)
		]
	+ SVerticalBox::Slot()
		.AutoHeight()
		.HAlign(HAlign_Right)
		.Padding(2)
		[
			SNew(SUniformGridPanel)
			.SlotPadding(2)
		+ SUniformGridPanel::Slot(0, 0)
		[
			SNew(STextBlock)
			.Text(FText::FromString("Exporth Folder Path"))
		]
	+ SUniformGridPanel::Slot(1, 0)
		[
			SNew(SEditableText)
			.Text(FText::FromString(ToolData->ExportPath))
		]
	+ SUniformGridPanel::Slot(2, 0)
		[
			//SAssignNew(ImportButton, SButton)
			SNew(SButton)
			.HAlign(HAlign_Center)
		.Text(FText::FromString("Browse"))
		.OnClicked(this, &SJanusExporterWindow::OnBrowseDir)
		]
		]
	+ SVerticalBox::Slot()
		.AutoHeight()
		.HAlign(HAlign_Right)
		.Padding(2)
		[
			SNew(SUniformGridPanel)
			.SlotPadding(2)
		+ SUniformGridPanel::Slot(1, 0)
		[
			SNew(SButton)
			.HAlign(HAlign_Center)
		.Text(FText::FromString("Export"))
		//.ToolTipText(LOCTEXT("FbxOptionWindow_ImportAll_ToolTip", "Import all files with these same settings"))
		//.IsEnabled(this, &SFbxOptionWindow::CanImport)
		.OnClicked(this, &SJanusExporterWindow::DoExport)
		]
	//+ SUniformGridPanel::Slot(2, 0)
	//[
	//	//SAssignNew(ImportButton, SButton)
	//	SNew(SButton)
	//	.HAlign(HAlign_Center)
	//	.Text(LOCTEXT("FbxOptionWindow_Import", "Show In Explorer"))
	//	//.IsEnabled(this, &SFbxOptionWindow::CanImport)
	//	.OnClicked(this, &SJanusExporterWindow::ShowInExplorer)
	//]
		]
		]
		];

	FPropertyEditorModule& PropertyEditorModule = FModuleManager::GetModuleChecked<FPropertyEditorModule>("PropertyEditor");
	FDetailsViewArgs DetailsViewArgs;
	DetailsViewArgs.bAllowSearch = false;
	DetailsViewArgs.NameAreaSettings = FDetailsViewArgs::HideNameArea;
	TSharedPtr<IDetailsView> DetailsView = PropertyEditorModule.CreateDetailView(DetailsViewArgs);

	InspectorBox->SetContent(DetailsView->AsShared());
	DetailsView->SetObject(ToolData);
}

FReply SJanusExporterWindow::OnBrowseDir()
{
	IDesktopPlatform* DesktopPlatform = FDesktopPlatformModule::Get();

	if (DesktopPlatform)
	{
		TSharedPtr<SWindow> ParentWindow = FSlateApplication::Get().FindWidgetWindow(AsShared());
		void* ParentWindowHandle = (ParentWindow.IsValid() && ParentWindow->GetNativeWindow().IsValid()) ? ParentWindow->GetNativeWindow()->GetOSWindowHandle() : nullptr;

		FString FolderPath;
		const bool bFolderSelected = DesktopPlatform->OpenDirectoryDialog(ParentWindowHandle, "Choose a directory", ToolData->ExportPath, FolderPath);

		if (bFolderSelected)
		{
			if (!FolderPath.EndsWith(L"\\"))
			{
				FolderPath += L"\\";
			}
			ToolData->ExportPath = FolderPath;
		}
	}
	return FReply::Handled();
}

void AssembleListOfExporters(TArray<UExporter*>& OutExporters)
{
	UPackage* TransientPackage = GetTransientPackage();

	OutExporters.Empty();
	for (TObjectIterator<UClass> It; It; ++It)
	{
		if (It->IsChildOf(UExporter::StaticClass()) && !It->HasAnyClassFlags(CLASS_Abstract))
		{
			UExporter* Exporter = NewObject<UExporter>(TransientPackage, *It);
			OutExporters.Add(Exporter);
		}
	}
}

void GetMaterialExportFlags(UMaterialInterface* InMaterial, UJanusExporterUI* InExportData, bool* OutCanExportBaseColor,
	bool* OutCanExportNormal)
{
	*OutCanExportBaseColor = FMaterialUtilities::SupportsExport(EBlendMode::BLEND_Opaque, EMaterialProperty::MP_BaseColor);
	*OutCanExportNormal = InExportData->ExportNormals &&FMaterialUtilities::SupportsExport(EBlendMode::BLEND_Opaque, EMaterialProperty::MP_Normal);
}

bool ExportFBX(UStaticMesh* Mesh, FString RootFolder, TArray<UMaterialInterface*>* Materials, UJanusExporterUI* ToolData)
{
	FString SelectedExportPath = FPaths::Combine(RootFolder, Mesh->GetName());

	UnFbx::FFbxExporter* Exporter = UnFbx::FFbxExporter::GetInstance();
	//UJanusFbxExporter* Exporter = UJanusFbxExporter::GetInstance();
	Exporter->CreateDocument();
	Exporter->ExportStaticMesh(Mesh);
	//Exporter->ExportStaticMesh(Actor, MeshComponent, &INodeNameAdapter());

	FbxScene* Scene = Exporter->Scene;
	for (int i = 0; i < Scene->GetNodeCount(); i++)
	{
		FbxNode* Node = Scene->GetNode(i);

		FbxDouble3 Rotation = Node->LclRotation.Get();
		Rotation[2] -= 90;
		Node->LclRotation.Set(Rotation);
	}

	//for (int i = 0; i < Scene->GetMaterialCount(); i++)
	//{
	//	FbxSurfaceMaterial* Material = Scene->GetMaterial(i);
	//	Scene->RemoveMaterial(Material);
	//}

	//TArray<UMaterialInterface*> Mats = *Materials;
	//for (int i = 0; i < Materials->Num(); i++)
	//{
	//	UMaterialInterface* Mat = Mats[i];

	//	bool bExportedBaseColor;
	//	bool bExportedNormal;
	//	GetMaterialExportFlags(Mat, ToolData, &bExportedBaseColor, &bExportedNormal);

	//	FbxSurfacePhong* FbxMaterial = FbxSurfacePhong::Create(Scene, TCHAR_TO_ANSI(*Mat->GetName()));
	//	FbxMaterial->Diffuse.Set(FbxDouble3(1, 1, 1));
	//	FbxMaterial->DiffuseFactor.Set(1.);
	//	Scene->AddMaterial(FbxMaterial);

	//	if (bExportedBaseColor)
	//	{
	//		// Set texture properties.
	//		FbxFileTexture* lTexture = FbxFileTexture::Create(Scene, "Diffuse Texture");
	//		char* textureFileName = TCHAR_TO_ANSI(*(Mat->GetName() + "_BaseColor.png"));

	//		lTexture->SetFileName(textureFileName); // Resource file is in current directory.
	//		lTexture->SetTextureUse(FbxTexture::ETextureUse::eStandard);
	//		lTexture->SetMappingType(FbxTexture::eUV);
	//		lTexture->SetMaterialUse(FbxFileTexture::EMaterialUse::eModelMaterial);
	//		lTexture->SetSwapUV(false);
	//		lTexture->SetTranslation(0.0, 0.0);
	//		lTexture->SetScale(1.0, 1.0);
	//		lTexture->SetRotation(0.0, 0.0);
	//		FbxMaterial->Diffuse.ConnectSrcObject(lTexture);
	//	}

	//	if (bExportedNormal)
	//	{
	//		// Set texture properties.
	//		FbxFileTexture* lTexture = FbxFileTexture::Create(Scene, "Normal Texture");
	//		char* textureFileName = TCHAR_TO_ANSI(*(Mat->GetName() + "_Normal.png"));

	//		lTexture->SetFileName(textureFileName);
	//		lTexture->SetTextureUse(FbxTexture::ETextureUse::eBumpNormalMap);
	//		lTexture->SetMappingType(FbxTexture::eUV);
	//		lTexture->SetMaterialUse(FbxFileTexture::EMaterialUse::eModelMaterial);
	//		lTexture->SetSwapUV(false);
	//		lTexture->SetTranslation(0.0, 0.0);
	//		lTexture->SetScale(1.0, 1.0);
	//		lTexture->SetRotation(0.0, 0.0);
	//		FbxMaterial->NormalMap.ConnectSrcObject(lTexture);
	//	}
	//}

	FbxAxisSystem::EFrontVector FrontVector = FbxAxisSystem::eParityEven;
	const FbxAxisSystem UnrealZUp(FbxAxisSystem::eYAxis, FbxAxisSystem::eParityOdd, FbxAxisSystem::eRightHanded);
	UnrealZUp.ConvertScene(Scene);

	Exporter->WriteToFile(*SelectedExportPath);
	return true;
}

void ExportPNG(FString& Path, TArray<FColor> ColorData, int Width, int Height)
{
	FString fPath = Path + ".png";
	TArray<uint8> PNGData;
	FImageUtils::CompressImageArray(Width, Height, ColorData, PNGData);
	FFileHelper::SaveArrayToFile(PNGData, *fPath);
}

void ExportEXR(FString& Path, Imf::Rgba* ColorData, int Width, int Height)
{
	FString fPath = Path + ".exr";
	Imf::RgbaOutputFile file(TCHAR_TO_ANSI(*fPath), Width, Height, Imf::WRITE_RGB,
		1, Imath::V2f(0, 0), 1, Imf::LineOrder::INCREASING_Y, Imf::Compression::ZIP_COMPRESSION, Imf::globalThreadCount()); // 1
	file.setFrameBuffer(ColorData, 1, Width); // 2
	file.writePixels(Height);
}

void ExportPNG(UTexture* Texture, FString RootFolder, bool bFillAlpha = true)
{
	FString TexPath = FPaths::Combine(RootFolder, Texture->GetName()) + ".png";

	UTexture2D* Texture2D = (UTexture2D*)Texture;
	TEnumAsByte<TextureCompressionSettings> Compression = Texture2D->CompressionSettings;
	TEnumAsByte<TextureMipGenSettings> MipSettings = Texture2D->MipGenSettings;
	uint32 SRGB = Texture2D->SRGB;
	Texture2D->CompressionSettings = TextureCompressionSettings::TC_VectorDisplacementmap;
	Texture2D->MipGenSettings = TextureMipGenSettings::TMGS_NoMipmaps;
	Texture2D->SRGB = 0;
	Texture2D->UpdateResource();

	FTexture2DMipMap* MipMap = &Texture2D->PlatformData->Mips[0];

	void* DataPointer = MipMap->BulkData.Lock(LOCK_READ_ONLY);
	FColor* Data = static_cast<FColor*>(DataPointer);

	TArray<FColor> ColorData;

	int32 Width = MipMap->SizeX;
	int32 Height = MipMap->SizeY;
	int32 Elements = Width * Height;

	if (bFillAlpha)
	{
		for (int i = 0; i < Elements; i++) // this is terrible
		{
			FColor Color = Data[i];
			Color.A = 255;
			ColorData.Add(Color);
		}
	}
	else
	{
		for (int i = 0; i < Elements; i++) // this is terrible
		{
			FColor Color = Data[i];
			ColorData.Add(Color);
		}
	}

	MipMap->BulkData.Unlock();
	Texture2D->CompressionSettings = Compression;
	Texture2D->MipGenSettings = MipSettings;
	Texture2D->SRGB = SRGB;
	Texture2D->UpdateResource();

	ExportPNG(TexPath, ColorData, Width, Height);
}
void ExportTGA(UTexture* Texture, FString RootFolder)
{
	FString TexPath = FPaths::Combine(RootFolder + Texture->GetName()) + ".tga";

	auto TransientPackage = GetTransientPackage();
	UTextureExporterTGA* Exporter = NewObject<UTextureExporterTGA>(TransientPackage, UTextureExporterTGA::StaticClass());

	const FScopedBusyCursor BusyCursor;

	UExporter::FExportToFileParams Params;
	Params.Object = Texture;
	Params.Exporter = Exporter;
	Params.Filename = *TexPath;
	Params.InSelectedOnly = false;
	Params.NoReplaceIdentical = false;
	Params.Prompt = false;
	Params.bUseFileArchive = Texture->IsA(UPackage::StaticClass());
	Params.WriteEmptyFiles = false;
	UExporter::ExportToFileEx(Params);
}
void ExportBMP(FString& Path, TArray<FColor> ColorData, int Width, int Height)
{
	FFileHelper::CreateBitmap(*Path, Width, Height, ColorData.GetData());
}

FColor Exposure(Imf::Rgba color, float relative_fstop)
{
	float val = FMath::Pow(2.0, relative_fstop);

	float r = fminf(color.r * val * 255.0f, 255);
	float g = fminf(color.g * val * 255.0f, 255);
	float b = fminf(color.b * val * 255.0f, 255);

	return FColor(r, g, b, 255);
}

void DecodeAndSaveHQLightmap(LightmappedObjects* Exported, FString RootFolder, JanusTextureFormat Format, float RelFStops)
{
	FLightMap2D* LightMap = Exported->LightMap;
	UTexture2D* Texture2D = LightMap->GetTexture(0);
	FString TexName = Texture2D->GetName();
	FString TexPath = RootFolder + TexName;

	TEnumAsByte<TextureCompressionSettings> Compression = Texture2D->CompressionSettings;
	TEnumAsByte<TextureMipGenSettings> MipSettings = Texture2D->MipGenSettings;
	uint32 SRGB = Texture2D->SRGB;
	Texture2D->CompressionSettings = TextureCompressionSettings::TC_HDR;//TextureCompressionSettings::TC_VectorDisplacementmap;
	Texture2D->MipGenSettings = TextureMipGenSettings::TMGS_NoMipmaps;
	Texture2D->SRGB = 0;
	Texture2D->UpdateResource();

	FTexture2DMipMap* MipMap = &Texture2D->PlatformData->Mips[0];

	void* DataPointer = MipMap->BulkData.Lock(LOCK_READ_ONLY);
	Imf::Rgba* Data = static_cast<Imf::Rgba*>(DataPointer);

	int32 Width = MipMap->SizeX;
	int32 Height = MipMap->SizeY;
	int32 Elements = Width * Height;
	int32 HalfHeight = Height / 2;

	Imf::Rgba* ColorData = new Imf::Rgba[Elements];
	const float LogBlackPoint = 0.01858136;

	TArray<AActor*> Actors = Exported->Actors;
	for (int i = 0; i < Actors.Num(); i++)
	{
		AActor* Actor = Actors[i];

		TArray<UStaticMeshComponent*> StaticMeshes;
		Actor->GetComponents<UStaticMeshComponent>(StaticMeshes);
		for (int j = 0; j < StaticMeshes.Num(); j++)
		{
			UStaticMeshComponent* Component = StaticMeshes[j];
			if (!Component->GetStaticMesh() ||
				!Component->HasLightmapTextureCoordinates())
			{
				continue;
			}

			UStaticMesh* Mesh = Component->GetStaticMesh();
			int32 LMapIndex = Mesh->LightMapCoordinateIndex;

			const FMeshMapBuildData* MeshMapBuildData = Component->GetMeshMapBuildData(Component->LODData[0]);

			if (!MeshMapBuildData)
			{
				continue;
			}

			FLightMap* LMap = MeshMapBuildData->LightMap;
			if (!LMap)
			{
				continue;
			}

			FLightMap2D* LMap2D = LMap->GetLightMap2D();
			if (LMap2D->GetTexture(0)->GetName() != TexName)
			{
				continue;
			}

			FLightMapInteraction Interaction = LMap2D->GetInteraction(ERHIFeatureLevel::SM5);
			const FVector4 Scale = Interaction.GetScaleArray()[0];
			const FVector4 Add = Interaction.GetAddArray()[0];

			const FVector2D Sca = Interaction.GetCoordinateScale();
			const FVector2D Bias = Interaction.GetCoordinateBias();

			int X = (int)fmin(Bias.X * Width, Width);
			int Y = (int)fmin(Bias.Y * Height, Height);
			int W = (int)fmin(X + (Sca.X * Width), Width);
			int H = (int)(fmin(Y + (Sca.Y * Height), Height) / 2.0);
			Y = (int)(Y / 2.0);

			for (int x = X; x < W; x++)
			{
				for (int y = Y; y < H; y++)
				{
					int Index0 = (x + (y * Width));
					int Index1 = (x + ((y + HalfHeight) * Width));
					Imf::Rgba Color0 = Data[Index0];
					Imf::Rgba Color1 = Data[Index1];
					FVector4 Lightmap0 = FVector4(Color0.r, Color0.g, Color0.b, Color0.a);
					FVector4 Lightmap1 = FVector4(Color1.r, Color1.g, Color1.b, Color1.a);

					double LogL = Lightmap0.W;
					// Add residual
					LogL += Lightmap1.W * (1.0 / 255.0) - (0.5 / 255.0);
					// Range scale LogL
					LogL = LogL * Scale.W + Add.W;
					// Range scale UVW
					FVector UVW = Lightmap0 * Lightmap0 * Scale + Add;
					// LogL -> L
					double L = exp2(LogL) - LogBlackPoint;
					double Directionality = 0.6;
					double Luma = L * Directionality;
					FVector Color = Luma * UVW;

					// Exposure
					float RelativeFStop = 1;
					float Exp = FMath::Pow(2.0f, RelativeFStop);
					Color = Color * Exp;

					Imf::Rgba color = Imf::Rgba(UVW.X * Luma, UVW.Y * Luma, UVW.Z * Luma, 1);

					ColorData[x + ((y * 2) * Width)] = color;
					ColorData[x + (((y * 2) + 1) * Width)] = color;
				}
			}
		}
	}

	MipMap->BulkData.Unlock();
	Texture2D->CompressionSettings = Compression;
	Texture2D->MipGenSettings = MipSettings;
	Texture2D->SRGB = SRGB;
	Texture2D->UpdateResource();

	switch (Format)
	{
	case OpenEXR:
		ExportEXR(TexPath, ColorData, Width, Height);
		break;
	case PNG:
		// convert from HDR to LDR
		TArray<FColor> PNGData;
		PNGData.SetNum(Elements);

		for (int i = 0; i < Elements; i++)
		{
			Imf::Rgba color = ColorData[i];
			PNGData[i] = Exposure(color, RelFStops);
		}
		ExportPNG(TexPath, PNGData, Width, Height);
		break;
	}

	delete[] ColorData;
}

float Luminance(FVector LinearColor)
{
	return FVector::DotProduct(LinearColor, FVector(0.3f, 0.59f, 0.11f));
}

void DecodeAndSaveLQLightmap(LightmappedObjects* Exported, FString RootFolder)
{
	FLightMap2D* LightMap = Exported->LightMap;
	UTexture2D* Texture2D = LightMap->GetTexture(0);
	FString TexName = Texture2D->GetName();
	FString TexPath = RootFolder + TexName;

	TEnumAsByte<TextureCompressionSettings> Compression = Texture2D->CompressionSettings;
	TEnumAsByte<TextureMipGenSettings> MipSettings = Texture2D->MipGenSettings;
	uint32 SRGB = Texture2D->SRGB;
	Texture2D->CompressionSettings = TextureCompressionSettings::TC_HDR;//TextureCompressionSettings::TC_VectorDisplacementmap;
	Texture2D->MipGenSettings = TextureMipGenSettings::TMGS_NoMipmaps;
	Texture2D->SRGB = 0;
	Texture2D->UpdateResource();

	FTexture2DMipMap* MipMap = &Texture2D->PlatformData->Mips[0];

	void* DataPointer = MipMap->BulkData.Lock(LOCK_READ_ONLY);
	Imf::Rgba* Data = static_cast<Imf::Rgba*>(DataPointer);

	TArray<FColor> ColorData;

	int32 Width = MipMap->SizeX;
	int32 Height = MipMap->SizeY;
	int32 Elements = Width * Height;
	int32 HalfHeight = Height / 2;

	ColorData.SetNum(Elements);

	TArray<AActor*> Actors = Exported->Actors;
	for (int i = 0; i < Actors.Num(); i++)
	{
		AActor* Actor = Actors[i];

		TArray<UStaticMeshComponent*> StaticMeshes;
		Actor->GetComponents<UStaticMeshComponent>(StaticMeshes);
		for (int j = 0; j < StaticMeshes.Num(); j++)
		{
			UStaticMeshComponent* Component = StaticMeshes[j];
			if (!Component->GetStaticMesh() ||
				!Component->HasLightmapTextureCoordinates())
			{
				continue;
			}

			UStaticMesh* Mesh = Component->GetStaticMesh();
			int32 LMapIndex = Mesh->LightMapCoordinateIndex;

			const FMeshMapBuildData* MeshMapBuildData = Component->GetMeshMapBuildData(Component->LODData[0]);

			if (!MeshMapBuildData)
			{
				continue;
			}

			FLightMap* LMap = MeshMapBuildData->LightMap;
			if (!LMap)
			{
				continue;
			}

			FLightMap2D* LMap2D = LMap->GetLightMap2D();
			if (LMap2D->GetTexture(0)->GetName() != TexName)
			{
				continue;
			}

			FLightMapInteraction Interaction = LMap2D->GetInteraction(ERHIFeatureLevel::SM5);
			const FVector4 Scale = Interaction.GetScaleArray()[0];
			const FVector4 Add = Interaction.GetAddArray()[0];

			const FVector2D Sca = Interaction.GetCoordinateScale();
			const FVector2D Bias = Interaction.GetCoordinateBias();

			int X = fmin(Bias.X * Width, Width);
			int Y = fmin(Bias.Y * Height, Height);
			int W = fmin(X + (Sca.X * Width), Width);
			int H = fmin(Y + (Sca.Y * Height), Height) / 2;

			for (int x = X; x < W; x++)
			{
				for (int y = Y; y < H; y++)
				{
					int Index0 = (x + (y * Width));
					int Index1 = (x + ((y + HalfHeight) * Width));
					Imf::Rgba Color0 = Data[Index0];
					Imf::Rgba Color1 = Data[Index1];
					FVector4 Lightmap0 = FVector4(Color0.r, Color0.g, Color0.b, Color0.a);
					FVector4 Lightmap1 = FVector4(Color1.r, Color1.g, Color1.b, Color1.a);

					FVector LogRGB = FVector(Lightmap0.X * Scale.X, Lightmap0.Y * Scale.Y, Lightmap0.Z * Scale.Z);

					float LogL = Luminance(LogRGB);

					// LogL -> L
					const float LogBlackPoint = 0.00390625;	// exp2(-8);
					float L = exp2(LogL * 16 - 8) - LogBlackPoint;		// 1 exp2, 1 smad, 1 ssub

					float Directionality = 0.6;

					float Luma = L * Directionality;
					FVector Color = LogRGB * (Luma / LogL);				// 1 rcp, 1 smul, 1 vmul

																		// Exposure
					float RelativeFStop = 1;
					float Exp = FMath::Pow(2.0f, RelativeFStop);
					Color = Color * Exp;

					float r = fminf(Color.X * 255.0f, 255);
					float g = fminf(Color.Y * 255.0f, 255);
					float b = fminf(Color.Z * 255.0f, 255);

					FColor FinalColor = FColor(r, g, b, 255);

					ColorData[x + ((y * 2) * Width)] = FinalColor;
					ColorData[x + (((y * 2) + 1) * Width)] = FinalColor;
				}
			}
		}
	}

	MipMap->BulkData.Unlock();
	Texture2D->CompressionSettings = Compression;
	Texture2D->MipGenSettings = MipSettings;
	Texture2D->SRGB = SRGB;
	Texture2D->UpdateResource();

	ExportPNG(TexPath, ColorData, Width, Height);
}



void ExportMaterial(int maxResolution, FString& Folder, UMaterialInterface* InMaterial, JanusTextureFormat Format, UJanusExporterUI* InExportData,
	TArray<FString>* TexturesExp, TArray<AssetImage>* AssetImages)
{
	check(InMaterial);

	TEnumAsByte<EBlendMode> BlendMode = InMaterial->GetBlendMode();
	bool bCanExportBaseColor = FMaterialUtilities::SupportsExport(EBlendMode::BLEND_Opaque, EMaterialProperty::MP_BaseColor);
	bool bCanExportMetallic = FMaterialUtilities::SupportsExport(EBlendMode::BLEND_Opaque, EMaterialProperty::MP_Metallic);
	bool bCanExportRoughness = FMaterialUtilities::SupportsExport(EBlendMode::BLEND_Opaque, EMaterialProperty::MP_Roughness);
	bool bCanExportSpecular = FMaterialUtilities::SupportsExport(EBlendMode::BLEND_Opaque, EMaterialProperty::MP_Specular);
	bool bCanExportNormal = InExportData->ExportNormals && FMaterialUtilities::SupportsExport(EBlendMode::BLEND_Opaque, EMaterialProperty::MP_Normal);

	if (bCanExportNormal)
	{
		TArray<FColor> NormalData;
		FIntPoint OutSize = FIntPoint(maxResolution, maxResolution);
		FMaterialUtilities::ExportMaterialProperty(InMaterial, EMaterialProperty::MP_Normal, OutSize, NormalData);
		int ColorElements = OutSize.X * OutSize.Y;

		FString MatName = InMaterial->GetName();
		FString Path = MatName + "_Normal";

		FString fullPath = Folder + "/" + Path;

		FColor* First = NormalData.GetData();
		for (int i = 0; i < ColorElements; i++)
		{
			FColor Color = NormalData[i];
			Color.A = 255;
			First[i] = Color;
		}

		ExportPNG(fullPath, NormalData, OutSize.X, OutSize.Y);

		TexturesExp->Add(Path);
		AssetImage Asset;
		Asset.Format = Format;
		Asset.Path = Path;
		AssetImages->Add(Asset);
	}

	if (bCanExportBaseColor)
	{
		TArray<FColor> ColorData;
		FIntPoint OutSize = FIntPoint(maxResolution, maxResolution);
		FMaterialUtilities::ExportMaterialProperty(InMaterial, EMaterialProperty::MP_BaseColor, OutSize, ColorData);
		int ColorElements = OutSize.X * OutSize.Y;

		bool SetAlpha = false;
		FColor* First = ColorData.GetData();
		switch (BlendMode)
		{
		case EBlendMode::BLEND_Masked:
		{
			TArray<FColor> MaskData;
			FMaterialUtilities::ExportMaterialProperty(InMaterial, EMaterialProperty::MP_OpacityMask, OutSize, MaskData);

			if (MaskData.Num() > 0)
			{
				for (int i = 0; i < ColorElements; i++)
				{
					First[i].A = MaskData[i].R;
				}
			}
			else
			{
				SetAlpha = true;
			}
		}
		break;
		case EBlendMode::BLEND_AlphaComposite:
		{
			TArray<FColor> OpacityData;
			FMaterialUtilities::ExportMaterialProperty(InMaterial, EMaterialProperty::MP_Opacity, OutSize, OpacityData);

			if (OpacityData.Num() > 0)
			{
				for (int i = 0; i < ColorElements; i++)
				{
					First[i].A = OpacityData[i].R;
				}
			}
			else
			{
				SetAlpha = true;
			}
		}
		case EBlendMode::BLEND_Opaque:
		default:
		{
			// for some reason it's all transparent, so change the alpha
			SetAlpha = true;
		}
		break;
		}

		if (SetAlpha)
		{
			for (int i = 0; i < ColorElements; i++)
			{
				First[i].A = 255;
			}
		}

		FString MatName = InMaterial->GetName();
		FString Path = MatName + "_BaseColor";

		FString fullPath = Folder + "/" + Path;

		ExportPNG(fullPath, ColorData, OutSize.X, OutSize.Y);

		TexturesExp->Add(Path);
		AssetImage Asset;
		Asset.Format = Format;
		Asset.Path = Path;
		AssetImages->Add(Asset);
	}
}

FVector ChangeSpace(FVector Vector)
{
	return FVector(Vector.Y, Vector.Z, -Vector.X);
}
FVector ChangeSpaceScalar(FVector Vector)
{
	return FVector(Vector.Y, Vector.Z, Vector.X);
}
FVector GetSignVector(FVector Vector)
{
	return FVector(Vector.X > 0 ? 1 : -1, Vector.Y > 0 ? 1 : -1, Vector.Z > 0 ? 1 : -1);
}
FVector Abs(FVector Vector)
{
	return FVector(fabsf(Vector.X), fabsf(Vector.Y), fabsf(Vector.Z));
}

FReply SJanusExporterWindow::DoExport()
{
	float UniformScale = ToolData->ExportScale;
	FString ExportPath = ToolData->ExportPath;
	int MaterialResolution = ToolData->MaterialResolution;
	bool ExportNormals = ToolData->ExportNormals;
	JanusLightmapExportType LightmapFormat = ToolData->LightmapFormat;
	JanusTextureFormat LightmapTexFormat = GetTextureFormatFromLightmap(LightmapFormat);

	TArray<UObject*> ObjectsToExport;

	FString Root = FString(ExportPath); // copy so we dont mess with the original reference
	FString Index = "<html>\n\t<head>\n\t\t<title>Unreal Export</title>\n\t</head>\n\t<body>\n\t\t<FireBoxRoom>\n\t\t\t<Assets>";

	TArray<AActor*> ActorsExported;
	TArray<UStaticMesh*> StaticMeshesExp;
	TArray<AssetObject> StaticMeshesExported;
	TArray<FString> TexturesExp;
	TArray<AssetImage> AssetImages;
	TArray<UMaterialInterface*> MaterialsExported;
	TMap<FString, LightmappedObjects> LightmapsToExport;

	for (TObjectIterator<AActor> Itr; Itr; ++Itr)
	{
		AActor *Actor = *Itr;

		FString Name = Actor->GetName();
		if (Actor->GetName().Contains("SkySphere"))
		{
			continue;
		}

		ActorsExported.Add(Actor);

		TArray<UStaticMeshComponent*> StaticMeshes;
		Actor->GetComponents<UStaticMeshComponent>(StaticMeshes);
		for (int32 i = 0; i < StaticMeshes.Num(); i++)
		{
			UStaticMeshComponent* Component = StaticMeshes[i];
			UStaticMesh *Mesh = Component->GetStaticMesh();
			if (!Mesh ||
				Component->LODData.Num() == 0)
			{
				continue;
			}

			TArray<UMaterialInterface*> Materials = Component->GetMaterials();
			for (int32 j = 0; j < Materials.Num(); j++)
			{
				UMaterialInterface* Material = Materials[j];
				if (!Material)
				{
					continue;
				}

				if (MaterialsExported.Contains(Material))
				{
					continue;
				}

				MaterialsExported.Add(Material);
				ExportMaterial(MaterialResolution, Root, Material, JanusTextureFormat::PNG, ToolData, &TexturesExp, &AssetImages);
			}

			if (!StaticMeshesExp.Contains(Mesh))
			{
				StaticMeshesExp.Add(Mesh);

				AssetObject obj;
				obj.Mesh = Mesh;
				obj.Component = Component;
				StaticMeshesExported.Add(obj);

				ExportFBX(Mesh, Root, &Materials, ToolData);
			}

			if (LightmapFormat == JanusLightmapExportType::PackedOpenEXR ||
				LightmapFormat == JanusLightmapExportType::PackedLDR)
			{
				int32 LMapIndex = Mesh->LightMapCoordinateIndex;
				const FMeshMapBuildData* MeshMapBuildData = Component->GetMeshMapBuildData(Component->LODData[0]);

				if (!MeshMapBuildData)
				{
					continue;
				}

				FLightMap* LightMap = MeshMapBuildData->LightMap;
				if (LightMap != NULL)
				{
					FLightMap2D* LightMap2D = LightMap->GetLightMap2D();

					// HQ
					UTexture2D* Texture = LightMap2D->GetTexture(0); // 0 = HQ LightMap, 1 = LQ LightMap
					FString TexName = Texture->GetName();
					if (!LightmapsToExport.Contains(TexName))
					{
						LightmappedObjects Obj;
						Obj.LightMap = LightMap2D;
						LightmapsToExport.Add(TexName, Obj);
					}
					LightmapsToExport[TexName].Actors.Add(Actor);

					if (!TexturesExp.Contains(TexName))
					{
						TexturesExp.Add(TexName);

						AssetImage Asset;
						Asset.Format = LightmapTexFormat;
						Asset.Path = TexName;
						AssetImages.Add(Asset);
					}
				}
			}
		}
	}

	for (auto Elem : LightmapsToExport)
	{
		DecodeAndSaveHQLightmap(&Elem.Value, Root, LightmapTexFormat, ToolData->RelativeFStops);
	}

	// Models before textures so we can start showing the scene faster (textures take too long to load)
	for (int32 i = 0; i < StaticMeshesExp.Num(); i++)
	{
		UStaticMesh *Mesh = StaticMeshesExp[i];
		Index.Append("\n\t\t\t\t<AssetObject id=\"" + Mesh->GetName() + "\" src=\"" + Mesh->GetName() + ".fbx\" />");
	}

	for (int32 i = 0; i < AssetImages.Num(); i++)
	{
		AssetImage Asset = AssetImages[i];
		Index.Append("\n\t\t\t\t<AssetImage id=\"" + Asset.Path + "\" src=\"" + Asset.Path + JanusExporter::GetTextureFormatExtension(Asset.Format) + "\" />");
	}

	Index.Append("\n\t\t\t</Assets>\n\t\t\t<Room>");

	for (int32 i = 0; i < ActorsExported.Num(); i++)
	{
		AActor *Actor = ActorsExported[i];

		TArray<UStaticMeshComponent*> StaticMeshes;
		Actor->GetComponents<UStaticMeshComponent>(StaticMeshes);
		for (int32 i = 0; i < StaticMeshes.Num(); i++)
		{
			UStaticMeshComponent* Component = StaticMeshes[i];
			UStaticMesh *Mesh = Component->GetStaticMesh();
			if (!Mesh)
			{
				continue;
			}

			FString ImageID = "";
			FString NormalID = "";
			FString LmapID = "";
			FVector4 LightMapSca = FVector4(0, 0, 0, 0);
			bool HasLightmap = false;

			if (ToolData->LightmapFormat == JanusLightmapExportType::PackedOpenEXR ||
				ToolData->LightmapFormat == JanusLightmapExportType::PackedLDR)
			{
				if (Component->LODData.Num() > 0)
				{
					FStaticMeshComponentLODInfo* LODInfo = &Component->LODData[0];
					const FMeshMapBuildData* MeshMapBuildData = Component->GetMeshMapBuildData(Component->LODData[0]);
					FLightMap* LightMap = MeshMapBuildData->LightMap;

					if (LightMap != NULL)
					{
						FLightMap2D* LightMap2D = LightMap->GetLightMap2D();
						UTexture2D* Texture = LightMap2D->GetTexture(0);
						FString TexName = Texture->GetName();
						LmapID = TexName;

						FLightMapInteraction Interaction = LightMap2D->GetInteraction(ERHIFeatureLevel::SM5);
						FVector2D Scale = Interaction.GetCoordinateScale();
						FVector2D Bias = Interaction.GetCoordinateBias();
						LightMapSca = FVector4(Scale.X, Scale.Y, Bias.X, 1 - Bias.Y - Scale.Y);
						HasLightmap = true;
					}
				}
			}

			TArray<UMaterialInterface*> Materials = Component->GetMaterials();
			for (int32 j = 0; j < Materials.Num(); j++)
			{
				UMaterialInterface* Material = Materials[j];
				if (!Material)
				{
					continue;
				}

				bool bExportedBaseColor;
				bool bExportedNormal;
				GetMaterialExportFlags(Material, ToolData, &bExportedBaseColor, &bExportedNormal);

				if (bExportedBaseColor)
				{
					ImageID = Material->GetName() + "_BaseColor";
				}

				if (bExportedNormal)
				{
					NormalID = Material->GetName() + "_Normal";
				}
			}

			Index.Append("\n\t\t\t\t<Object ");

			//Index.Append("lighting=\"true\" ");
			Index.Append("id = \"" + Mesh->GetName() + "\" ");
			//Index.Append("collision_id=\"" + Mesh->GetName() + "\" ");

			FRotator Rot = Actor->GetActorRotation();
			FVector XDir = Rot.RotateVector(FVector::RightVector);
			FVector YDir = Rot.RotateVector(FVector::UpVector);
			FVector ZDir = Rot.RotateVector(FVector::ForwardVector);

			FVector Pos = Actor->GetActorLocation() * UniformScale;
			FVector Sca = Actor->GetActorScale();

			Pos = ChangeSpace(Pos);
			Sca = ChangeSpaceScalar(Sca) * UniformScale;

			XDir = ChangeSpace(XDir);
			YDir = ChangeSpace(YDir);
			ZDir = ChangeSpace(ZDir);
			FVector Sign = GetSignVector(Sca);

			Index.Append("pos=\"");
			Index.Append(FString::SanitizeFloat(Pos.X) + " " + FString::SanitizeFloat(Pos.Y) + " " + FString::SanitizeFloat(Pos.Z) + "\" ");
			if (Sca.X < 0 || Sca.Y < 0 || Sca.Z < 0)
			{
				Index.Append("cull_face=\"front\" ");
			}

			if (ImageID != "")
			{
				Index.Append("image_id=\"" + ImageID + "\" ");
			}
			/*if (NormalID != "")
			{
				Index.Append("tex3=\"" + NormalID + "\" ");
			}*/

			Index.Append("js_id=\"" + Actor->GetName() + "\" ");

			if (HasLightmap)
			{
				Index.Append("lighting=\"false\" ");
				Index.Append("lmap_id=\"" + LmapID + "\" ");
				Index.Append("lmap_sca=\"");

				//float TexelSize = 1 / 1024.0f;

				Index.Append(FString::SanitizeFloat(LightMapSca.X) + " " + FString::SanitizeFloat(LightMapSca.Y) + " " + FString::SanitizeFloat(LightMapSca.Z) + " " + FString::SanitizeFloat(LightMapSca.W) + "\" ");
			}
			else
			{
				Index.Append("lighting=\"true\" ");
			}

			Index.Append("scale=\"");
			Index.Append(FString::SanitizeFloat(Sca.X) + " " + FString::SanitizeFloat(Sca.Y) + " " + FString::SanitizeFloat(Sca.Z) + "\" ");

			/*Index.Append("rotation=\"");
			FVector RotEuler = Rot.Euler();
			RotEuler = FVector(RotEuler.X, RotEuler.Z, RotEuler.Y);
			Index.Append(FString::SanitizeFloat(RotEuler.X) + " " + FString::SanitizeFloat(RotEuler.Y) + " " + FString::SanitizeFloat(RotEuler.Z) + "\" ");*/

			Index.Append("xdir=\"");
			Index.Append(FString::SanitizeFloat(XDir.X) + " " + FString::SanitizeFloat(XDir.Y) + " " + FString::SanitizeFloat(XDir.Z) + "\" ");

			Index.Append("ydir=\"");
			Index.Append(FString::SanitizeFloat(YDir.X) + " " + FString::SanitizeFloat(YDir.Y) + " " + FString::SanitizeFloat(YDir.Z) + "\" ");

			Index.Append("zdir=\"");
			Index.Append(FString::SanitizeFloat(ZDir.X) + " " + FString::SanitizeFloat(ZDir.Y) + " " + FString::SanitizeFloat(ZDir.Z) + "\" ");


			Index.Append("/>");
		}
	}

	Index.Append("\n\t\t\t</Room>\n\t\t</FireBoxRoom>\n\t</body>\n</html>");

	FString IndexPath = FString(ExportPath).Append("index.html");
	FFileHelper::SaveStringToFile(Index, *IndexPath);

	return FReply::Handled();
}

FReply SJanusExporterWindow::ShowInExplorer()
{
	IPlatformFile& PlatformFile = FPlatformFileManager::Get().GetPlatformFile();
	FString ExportPath = ToolData->ExportPath;

	if (PlatformFile.DirectoryExists(ExportPath.GetCharArray().GetData()))
	{
		FPlatformProcess::CreateProc(ExportPath.GetCharArray().GetData(), nullptr, true, false, false, nullptr, 0, nullptr, nullptr);
	}
	return FReply::Handled();
}


#undef LOCTEXT_NAMESPACE
