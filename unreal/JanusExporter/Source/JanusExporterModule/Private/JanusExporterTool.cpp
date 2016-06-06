#include "FJanusExporterModulePrivatePCH.h"
#include "JanusExporterTool.h"
#include "ScopedTransaction.h"
#include "EngineUtils.h"
#include "Runtime/Engine/Classes/Components/StaticMeshComponent.h"
#include "Runtime/Engine/Classes/Engine/World.h"
#include "Runtime/Engine/Public/LightMap.h"
#include "Runtime/Engine/Public/ShadowMap.h"
#include "Editor/UnrealEd/Public/ObjectTools.h"
#include "Editor/UnrealEd/Public/EditorDirectories.h"
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
#include "Developer/MaterialUtilities/Public/MaterialUtilities.h"

#define private public   // POG = Programacao Orientada a Gambiarra
#include "Editor/UnrealEd/Private/FbxExporter.h"
#undef private

#include <fbxsdk.h>

#define LOCTEXT_NAMESPACE "JanusExporter"

UJanusExporterTool::UJanusExporterTool()
	: Super(FObjectInitializer::Get())
{
	ExportPath = "C:\\janus\\";
}

void AssembleListOfExporters(TArray<UExporter*>& OutExporters)
{
	auto TransientPackage = GetTransientPackage();

	// @todo DB: Assemble this set once.
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

void UJanusExporterTool::SearchForExport()
{
	// If not prompting individual files, prompt the user to select a target directory.
	IDesktopPlatform* DesktopPlatform = FDesktopPlatformModule::Get();
	void* ParentWindowWindowHandle = NULL;

	IMainFrameModule& MainFrameModule = FModuleManager::LoadModuleChecked<IMainFrameModule>(TEXT("MainFrame"));
	const TSharedPtr<SWindow>& MainFrameParentWindow = MainFrameModule.GetParentWindow();
	if (MainFrameParentWindow.IsValid() && MainFrameParentWindow->GetNativeWindow().IsValid())
	{
		ParentWindowWindowHandle = MainFrameParentWindow->GetNativeWindow()->GetOSWindowHandle();
	}

	FString FolderName;
	const FString Title = NSLOCTEXT("UnrealEd", "ChooseADirectory", "Choose A Directory").ToString();
	const bool bFolderSelected = DesktopPlatform->OpenDirectoryDialog(
		ParentWindowWindowHandle,
		Title,
		ExportPath,
		FolderName
		);

	if (bFolderSelected)
	{
		ExportPath = FolderName.Append("//");
	}
}

bool ExportFBX(UStaticMesh* Mesh, FString RootFolder)
{
	FString SelectedExportPath = RootFolder + Mesh->GetName();

	UnFbx::FFbxExporter* Exporter = UnFbx::FFbxExporter::GetInstance();
	Exporter->CreateDocument();
	Exporter->ExportStaticMesh(Mesh);

	FbxScene* Scene = Exporter->Scene;
	for (int i = 0; i < Scene->GetNodeCount(); i++)
	{
		FbxNode* Node = Scene->GetNode(i);

		FbxDouble3 Rotation = Node->LclRotation.Get();
		Rotation[2] -= 90;
		Node->LclRotation.Set(Rotation);
	}

	for (int i = 0; i < Scene->GetMaterialCount(); i++)
	{
		FbxSurfaceMaterial* Material = Scene->GetMaterial(i);
		Scene->RemoveMaterial(Material);
	}

	FbxSurfaceLambert* FbxMaterial = FbxSurfaceLambert::Create(Scene, "Fbx Default Material");
	FbxMaterial->Diffuse.Set(FbxDouble3(0.72, 0.72, 0.72));
	Scene->AddMaterial(FbxMaterial);

	FbxAxisSystem::EFrontVector FrontVector = FbxAxisSystem::eParityEven;
	const FbxAxisSystem UnrealZUp(FbxAxisSystem::eYAxis, FbxAxisSystem::eParityOdd, FbxAxisSystem::eRightHanded);
	UnrealZUp.ConvertScene(Scene);

	Exporter->WriteToFile(*SelectedExportPath);

	return true;
}

void ExportTGA(UTexture* Texture, FString RootFolder)
{
	FString MeshPath = RootFolder + Texture->GetName() + ".tga";

	auto TransientPackage = GetTransientPackage();
	UTextureExporterTGA* Exporter = NewObject<UTextureExporterTGA>(TransientPackage, UTextureExporterTGA::StaticClass());

	const FScopedBusyCursor BusyCursor;

	UExporter::FExportToFileParams Params;
	Params.Object = Texture;
	Params.Exporter = Exporter;
	Params.Filename = *MeshPath;
	Params.InSelectedOnly = false;
	Params.NoReplaceIdentical = false;
	Params.Prompt = false;
	Params.bUseFileArchive = Texture->IsA(UPackage::StaticClass());
	Params.WriteEmptyFiles = false;
	UExporter::ExportToFileEx(Params);
}

void ExportPNG(UTexture* Texture, FString RootFolder, bool bFillAlpha = true)
{
	FString TexPath = RootFolder + Texture->GetName() + ".png";

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

	TArray<uint8> PNGData;

	FImageUtils::CompressImageArray(Width, Height, ColorData, PNGData);

	FFileHelper::SaveArrayToFile(PNGData, *TexPath);
}

void ExportPNG(FString& Path, TArray<FColor> ColorData, int Width, int Height)
{
	TArray<uint8> PNGData;
	FImageUtils::CompressImageArray(Width, Height, ColorData, PNGData);
	FFileHelper::SaveArrayToFile(PNGData, *Path);
}

void ExportBMP(FString& Path, TArray<FColor> ColorData, int Width, int Height)
{
	FFileHelper::CreateBitmap(*Path, Width, Height, ColorData.GetData());
}

void ExportMaterial(FString& Folder, UMaterialInterface* Material, TArray<FString>* ExportedTextures)
{
//#if CUSTOM_UNREAL  // needs custom Unreal source code to export materials, as the API is broken (until I manage to push my modifications to the main repo that is)
	check(Material);

	TEnumAsByte<EBlendMode> BlendMode = Material->GetBlendMode();
	bool bIsValidMaterial = FMaterialUtilities::SupportsExport((EBlendMode)(BlendMode), EMaterialProperty::MP_BaseColor);

	//if (bIsValidMaterial)
	{
		TArray<FColor> ColorData;
		FIntPoint Size;
		FMaterialUtilities::ExportMaterialProperty(Material, EMaterialProperty::MP_BaseColor, ColorData, Size);

		FString MatName = Material->GetName();
		FString Path = MatName + "_BaseColor";
		ExportedTextures->Add(Path);

		Path = Folder + "/" + Path + ".png";

		// for some reason it's all transparent, so change the alpha
		int ColorElements = Size.X * Size.Y;
		FColor* First = ColorData.GetData();
		for (int i = 0; i < ColorElements; i++)
		{
			First[i].A = 255;
		}

		ExportPNG(Path, ColorData, Size.X, Size.Y);
	}
//#endif
}

FVector ChangeSpace(FVector Vector)
{
	return FVector(Vector.Y, Vector.Z, -Vector.X);
}

FVector ChangeSpaceScalar(FVector Vector)
{
	return FVector(Vector.Y, Vector.Z, Vector.X);
}

float Abs(float Value)
{
	if (Value < 0)
	{
		return Value * -1;
	}
	return Value;
}

FVector GetSignVector(FVector Vector)
{
	return FVector(Vector.X > 0 ? 1 : -1, Vector.Y > 0 ? 1 : -1, Vector.Z > 0 ? 1 : -1);
}

FVector Abs(FVector Vector)
{
	return FVector(Abs(Vector.X), Abs(Vector.Y), Abs(Vector.Z));
}

void UJanusExporterTool::Export()
{
	TArray<UObject*> ObjectsToExport;

	FString Root = FString(ExportPath); // copy so we dont mess with the original reference
	FString Index = "<html>\n\t<head>\n\t\t<title>Unreal Export</title>\n\t</head>\n\t<body>\n\t\t<FireBoxRoom>\n\t\t\t<Assets>";

	TArray<AActor*> ActorsExported;
	TArray<UStaticMesh*> StaticMeshesExp;
	TArray<FString> TexturesExp;
	TArray<FString> MaterialsExported;

	for (TObjectIterator<AActor> Itr; Itr; ++Itr)
	{
		AActor *Actor = *Itr;

		FString Name = Actor->GetName();
		/*if (!Name.StartsWith("SM_Floor_R"))
		{
			continue;
		}*/

		if (Actor->IsHiddenEd())
		{
			continue;
		}

		ActorsExported.Add(Actor);

		TArray<UStaticMeshComponent*> StaticMeshes;
		Actor->GetComponents<UStaticMeshComponent>(StaticMeshes);
		for (int32 i = 0; i < StaticMeshes.Num(); i++)
		{
			UStaticMeshComponent* Component = StaticMeshes[i];
			UStaticMesh *Mesh = Component->StaticMesh;
			if (!Mesh)
			{
				continue;
			}

			if (Component->LODData.Num() > 0)
			//if (false)
			{
				FStaticMeshComponentLODInfo* LODInfo = &Component->LODData[0];
				FLightMap* LightMap = LODInfo->LightMap;
				FShadowMap* ShadowMap = LODInfo->ShadowMap;
				if (LightMap != NULL)
				{
					FLightMap2D* LightMap2D = LightMap->GetLightMap2D();
					UTexture2D* Texture = LightMap2D->GetTexture(0); // 0 = HQ LightMap
					FString TexName = Texture->GetName();
					if (TexturesExp.Contains(TexName))
					{
						continue;
					}

					TexturesExp.Add(TexName);
					ExportPNG(Texture, Root);
				}
				if (ShadowMap != NULL)
				{
					FShadowMap2D* ShadowMap2D = ShadowMap->GetShadowMap2D();
					UShadowMapTexture2D* ShadowTex = ShadowMap2D->GetTexture();
					FString TexName = ShadowTex->GetName();
					if (TexturesExp.Contains(TexName))
					{
						continue;
					}

					TexturesExp.Add(TexName);
					ExportPNG(ShadowTex, Root);
				}
			}

			if (!StaticMeshesExp.Contains(Mesh))
			{
				StaticMeshesExp.Add(Mesh);
				ExportFBX(Mesh, Root);
			}

			TArray<UMaterialInterface*> Materials = Component->GetMaterials();
			for (int32 j = 0; j < Materials.Num(); j++)
			{
				UMaterialInterface* Material = Materials[j];
				if (!Material)
				{
					continue;
				}

				FString MatName = Material->GetName();

				if (MaterialsExported.Contains(MatName))
				{
					continue;
				}

				MaterialsExported.Add(MatName);
				ExportMaterial(Root, Material, &TexturesExp);
			}
		}
	}

	// Models before textures so we can start showing the scene faster (textures take too long to load)
	for (int32 i = 0; i < StaticMeshesExp.Num(); i++)
	{
		UStaticMesh *mesh = StaticMeshesExp[i];

		Index.Append("\n\t\t\t\t<AssetObject id=\"" + mesh->GetName() + "\" src=\"" + mesh->GetName() + ".fbx\" />");
	}

	for (int32 i = 0; i < TexturesExp.Num(); i++)
	{
		FString Path = TexturesExp[i];

		Index.Append("\n\t\t\t\t<AssetImage id=\"" + Path + "\" src=\"" + Path + ".png\" />");
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
			UStaticMesh *Mesh = Component->StaticMesh;
			if (!Mesh)
			{
				continue;
			}

			FString ImageID = "";

			TArray<UMaterialInterface*> Materials = Component->GetMaterials();
			for (int32 j = 0; j < Materials.Num(); j++)
			{
				UMaterialInterface* Material = Materials[j];
				if (!Material)
				{
					continue;
				}
				ImageID = Material->GetName() + "_BaseColor";
				break;
			}

			if (ImageID == "")
			{
				Index.Append("\n\t\t\t\t<Object collision_id=\"" + Mesh->GetName() + "\" id=\"" + Mesh->GetName() + "\" lighting=\"true\" pos=\"");
			}
			else
			{
				Index.Append("\n\t\t\t\t<Object collision_id=\"" + Mesh->GetName() + "\" id=\"" + Mesh->GetName() + "\" image_id=\"" + ImageID + "\" lighting=\"true\" pos=\"");
			}

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
			
			Index.Append(FString::SanitizeFloat(Pos.X) + " " + FString::SanitizeFloat(Pos.Y) + " " + FString::SanitizeFloat(Pos.Z));
			if (Sca.X < 0 || Sca.Y < 0 || Sca.Z < 0)
			{
				Index.Append("\" cull_face=\"front");
			}

			Index.Append("\" scale=\"");
			Index.Append(FString::SanitizeFloat(Sca.X) + " " + FString::SanitizeFloat(Sca.Y) + " " + FString::SanitizeFloat(Sca.Z));

			Index.Append("\" xdir=\"");
			Index.Append(FString::SanitizeFloat(XDir.X) + " " + FString::SanitizeFloat(XDir.Y) + " " + FString::SanitizeFloat(XDir.Z));

			Index.Append("\" ydir=\"");
			Index.Append(FString::SanitizeFloat(YDir.X) + " " + FString::SanitizeFloat(YDir.Y) + " " + FString::SanitizeFloat(YDir.Z));

			Index.Append("\" zdir=\"");
			Index.Append(FString::SanitizeFloat(ZDir.X) + " " + FString::SanitizeFloat(ZDir.Y) + " " + FString::SanitizeFloat(ZDir.Z));

			Index.Append("\" />");
		}
	}

	Index.Append("\n\t\t\t</Room>\n\t\t</FireBoxRoom>\n\t</body>\n</html>");

	FString IndexPath = FString(ExportPath).Append("index.html");
	FFileHelper::SaveStringToFile(Index, *IndexPath);
}

#undef LOCTEXT_NAMESPACE