#pragma once

UENUM()
enum JanusTextureFormat
{
	OpenEXR,
	PNG
};

UENUM()
enum JanusLightmapExportType
{
	None,
	PackedOpenEXR,
	PackedLDR
};

struct AssetImage
{
	JanusTextureFormat Format;
	FString Path;
};
struct AssetObject
{
	UStaticMesh* Mesh;
	UStaticMeshComponent* Component;
	FString Path;
};



struct JanusExporter
{
public:
	

	static FString GetTextureFormatExtension(JanusTextureFormat format)
	{
		switch (format)
		{
		case JanusTextureFormat::OpenEXR:
			return ".exr";
		case JanusTextureFormat::PNG:
			return ".png";
		default:
			return "";
		}
	}
};