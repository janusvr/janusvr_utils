using UnrealBuildTool;

public class JanusExporterModule : ModuleRules
{
    public JanusExporterModule(TargetInfo Target)
	{
        PrivateIncludePaths.AddRange(
            new string[]
            {
                "F:/Dev/SyntheticTruth/UnrealEngine/Engine/Source/ThirdParty/FBX/2016.1.1/include",
                "F:/Dev/SyntheticTruth/UnrealEngine/Engine/Source/ThirdParty/FBX/2016.1.1/include/fbxsdk",
            }
        );

        PublicAdditionalLibraries.Add("F:/Dev/SyntheticTruth/UnrealEngine/Engine/Source/ThirdParty/FBX/2016.1.1/lib/vs2015/x64/release/libfbxsdk.lib");

        PublicDependencyModuleNames.AddRange(
			new string[] {
				"Core",
				"CoreUObject",
				"Engine",
				"Slate",
				"UnrealEd",
                "UElibPNG"
            }
		);
		
		PrivateDependencyModuleNames.AddRange(
			new string[] {
				"InputCore",
				"SlateCore",
				"PropertyEditor",
				"LevelEditor"
			}
		);

        AddThirdPartyPrivateStaticDependencies(Target, "FBX");
    }
}