#include "FJanusExporterModulePrivatePCH.h"
#include "JanusExporterTool.h"
#include "ScopedTransaction.h"
#include "EngineUtils.h"
#include "Editor/UnrealEd/Public/ObjectTools.h"
#include "Editor/UnrealEd/Public/EditorDirectories.h"
#include "Editor/MainFrame/Public/Interfaces/IMainFrameModule.h"
#include "Developer/DesktopPlatform/Public/DesktopPlatformModule.h"
#include "Runtime/Core/Public/Misc/CoreMisc.h"
#include "Editor/UnrealEd/Classes/Exporters/StaticMeshExporterFBX.h"
#include "Editor/UnrealEd/Classes/Exporters/TextureExporterTGA.h"
#include "Editor/UnrealEd/Public/BusyCursor.h"
#include "Runtime/Engine/Public/ImageUtils.h"

#define private public   // POG = Programacao Orientada a Gambiarra
#include "Editor/UnrealEd/Private/FbxExporter.h"
#undef private

#include <fbxsdk.h>

#define LOCTEXT_NAMESPACE "DemoTools"

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

void ExportPNG(UTexture* Texture, FString RootFolder)
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

	for (int i = 0; i < Elements; i++)
	{
		FColor Color = Data[i];
		ColorData.Add(Color);
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

float Absolute(float Value)
{
	if (Value < 0)
	{
		Value *= -1;
	}
	return Value;
}

FVector ChangeSpace(FVector Vector)
{
	return FVector(Vector.Y, Vector.Z, -Vector.X);
}

FVector ChangeSpaceScalar(FVector Vector)
{
	return FVector(Absolute(Vector.Y), Absolute(Vector.Z), Absolute(Vector.X));
}

void UJanusExporterTool::Export()
{
	TArray<UObject*> ObjectsToExport;
	const bool SkipRedirectors = false;

	FString root = FString(ExportPath); // copy so we dont mess with the original reference
	FString index = "<html><head><title>Unreal Export</title></head><body><FireBoxRoom><Assets>";

	TArray<AActor*> actorsExp;
	TArray<UStaticMesh*> StaticMeshesExp;
	TArray<UTexture*> TexturesExp;

	for (TObjectIterator<AActor> Itr; Itr; ++Itr)
	{
		AActor *Actor = *Itr;

		if (Actor->IsHiddenEd())
		{
			continue;
		}

		actorsExp.Add(Actor);

		TArray<UStaticMeshComponent*> staticMeshes;
		Actor->GetComponents<UStaticMeshComponent>(staticMeshes);
		for (int32 i = 0; i < staticMeshes.Num(); i++)
		{
			UStaticMeshComponent* Component = staticMeshes[i];
			UStaticMesh *mesh = Component->StaticMesh;
			if (!mesh || StaticMeshesExp.Contains(mesh))
			{
				continue;
			}

			StaticMeshesExp.Add(mesh);

			ExportFBX(mesh, root);

			TArray<UMaterialInterface*> mats = mesh->Materials;
			for (int32 j = 0; j < mats.Num(); j++)
			{
				UMaterialInterface* mat = mats[j];
				if (!mat)
				{
					continue;
				}

				TArray<UTexture*> OutTextures;
				mat->GetUsedTextures(OutTextures, EMaterialQualityLevel::Type::High, false, ERHIFeatureLevel::SM5, true);

				for (int32 n = 0; n < OutTextures.Num(); n++)
				{
					UTexture* tex = OutTextures[n];

					if (TexturesExp.Contains(tex))
					{
						continue;
					}

					TexturesExp.Add(tex);

					ExportPNG(tex, root);
				}
			}
		}
	}

	index.Append("\n");

	for (int32 i = 0; i < TexturesExp.Num(); i++)
	{
		UTexture *tex = TexturesExp[i];

		index.Append("<AssetImage id=\"" + tex->GetName() + "\" src=\"" + tex->GetName() + ".png" + "\"/>");
		index.Append("\n");
	}

	index.Append("\n");

	for (int32 i = 0; i < StaticMeshesExp.Num(); i++)
	{
		UStaticMesh *mesh = StaticMeshesExp[i];

		index.Append("<AssetObject id=\"" + mesh->GetName() + "\" src=\"" + mesh->GetName() + ".fbx\" />");
		index.Append("\n");
	}

	index.Append("</Assets><Room>");

	for (int32 i = 0; i < actorsExp.Num(); i++)
	{
		AActor *actor = actorsExp[i];

		if (actor->IsHiddenEd())
		{
			continue;
		}

		TArray<UStaticMeshComponent*> staticMeshes;
		actor->GetComponents<UStaticMeshComponent>(staticMeshes);
		for (int32 i = 0; i < staticMeshes.Num(); i++)
		{
			UStaticMeshComponent* Component = staticMeshes[i];
			UStaticMesh *mesh = Component->StaticMesh;
			if (!mesh)
			{
				continue;
			}

			FString ImageID = "";

			TArray<UMaterialInterface*> mats = mesh->Materials;
			for (int32 j = 0; j < mats.Num(); j++)
			{
				UMaterialInterface* Material = mats[j];
				if (!Material)
				{
					continue;
				}

				TArray<UTexture*> OutTextures;
				Material->GetUsedTextures(OutTextures, EMaterialQualityLevel::Type::High, false, ERHIFeatureLevel::SM5, true);

				for (int32 n = 0; n < OutTextures.Num(); n++)
				{
					UTexture* Texture = OutTextures[n];
					if (Texture->CompressionSettings == TextureCompressionSettings::TC_Default)
					{
						ImageID = Texture->GetName();
					}
					break;
				}
			}

			if (ImageID == "")
			{
				index.Append("<Object collision_id=\"" + mesh->GetName() + "\" id=\"" + mesh->GetName() + "\" lighting=\"true\" pos=\"");
			}
			else
			{
				index.Append("<Object collision_id=\"" + mesh->GetName() + "\" id=\"" + mesh->GetName() + "\" image_id=\"" + ImageID + "\"  lighting=\"true\" pos=\"");
			}

			FRotator rot = actor->GetActorRotation();
			FVector xdir = rot.RotateVector(FVector::RightVector);
			FVector ydir = rot.RotateVector(FVector::UpVector);
			FVector zdir = rot.RotateVector(FVector::ForwardVector);

			FVector pos = actor->GetActorLocation() * UniformScale;
			FVector sca = actor->GetActorScale() * UniformScale;

			pos = ChangeSpace(pos);
			sca = ChangeSpaceScalar(sca);

			xdir = ChangeSpace(xdir);
			ydir = ChangeSpace(ydir);
			zdir = ChangeSpace(zdir);

			// Unreal Engine uses a left-handed, z-up world coordinate system
			// Unity 3D uses a left-handed, y-up world coordinate system.

			/*xdir.Y *= -1;
			ydir.Y *= -1;
			zdir.Y *= -1;
			pos.Y *= -1;*/

			index.Append(FString::SanitizeFloat(pos.X) + " " + FString::SanitizeFloat(pos.Y) + " " + FString::SanitizeFloat(pos.Z));

			index.Append("\" scale=\"");
			index.Append(FString::SanitizeFloat(sca.X) + " " + FString::SanitizeFloat(sca.Y) + " " + FString::SanitizeFloat(sca.Z));

			index.Append("\" xdir=\"");
			index.Append(FString::SanitizeFloat(xdir.X) + " " + FString::SanitizeFloat(xdir.Y) + " " + FString::SanitizeFloat(xdir.Z));

			index.Append("\" ydir=\"");
			index.Append(FString::SanitizeFloat(ydir.X) + " " + FString::SanitizeFloat(ydir.Y) + " " + FString::SanitizeFloat(ydir.Z));

			index.Append("\" zdir=\"");
			index.Append(FString::SanitizeFloat(zdir.X) + " " + FString::SanitizeFloat(zdir.Y) + " " + FString::SanitizeFloat(zdir.Z));

			index.Append("\" />");

			index.Append("\n");
		}
	}

	index.Append("\n");
	index.Append("</Room></FireBoxRoom></body></html>");

	FString indexPath = FString(ExportPath).Append("index.html");
	FFileHelper::SaveStringToFile(index, *indexPath);
}

#undef LOCTEXT_NAMESPACE