#pragma once

UENUM()
enum JanusTextureFormat
{
	OpenEXR,
	PNG
};

struct AssetImage
{
	JanusTextureFormat Format;
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