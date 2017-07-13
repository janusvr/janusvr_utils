#pragma once

enum JanusTextureFormat
{
	OpenEXR,
	PNG
};
struct JanusExporterOptions
{
	int MaterialResolution;
	JanusTextureFormat LightmapFormat;
};
struct AssetImage
{
	JanusTextureFormat Format;
	FString Path;
};
