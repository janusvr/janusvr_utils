using System;
using System.IO;
// Copyright 1998-2017 Epic Games, Inc. All Rights Reserved.

namespace UnrealBuildTool.Rules
{
    public class JanusExporter : ModuleRules
    {
        public JanusExporter(ReadOnlyTargetRules Target) : base(Target)
        {
            // fbx
            string FBXSDKDir = UEBuildConfiguration.UEThirdPartySourceDirectory + "FBX/2016.1.1/";
            PublicIncludePaths.AddRange(
                new string[] {
                    FBXSDKDir + "include",
                    FBXSDKDir + "include/fbxsdk",
                    }
                );

            if (Target.Platform == UnrealTargetPlatform.Win64)
            {
                string FBxLibPath = FBXSDKDir + "lib/vs" + WindowsPlatform.GetVisualStudioCompilerVersionName() + "/";

                FBxLibPath += "x64/release/";
                PublicLibraryPaths.Add(FBxLibPath);

                PublicAdditionalLibraries.Add("libfbxsdk.lib");

                // We are using DLL versions of the FBX libraries
                Definitions.Add("FBXSDK_SHARED");

                RuntimeDependencies.Add(new RuntimeDependency("$(EngineDir)/Binaries/Win64/libfbxsdk.dll"));
            }
            else if (Target.Platform == UnrealTargetPlatform.Mac)
            {
                string LibDir = FBXSDKDir + "lib/clang/release/";
                PublicAdditionalLibraries.Add(LibDir + "libfbxsdk.dylib");
            }
            else if (Target.Platform == UnrealTargetPlatform.Linux)
            {
                string LibDir = FBXSDKDir + "lib/gcc4/" + Target.Architecture + "/release/";
                if (!Directory.Exists(LibDir))
                {
                    string Err = string.Format("FBX SDK not found in {0}", LibDir);
                    System.Console.WriteLine(Err);
                    throw new BuildException(Err);
                }

                PublicAdditionalLibraries.Add(LibDir + "/libfbxsdk.a");
                /* There is a bug in fbxarch.h where is doesn't do the check
                 * for clang under linux */
                Definitions.Add("FBXSDK_COMPILER_CLANG");

                // libfbxsdk has been built against libstdc++ and as such needs this library
                PublicAdditionalLibraries.Add("stdc++");
            }

            PrivateIncludePaths.AddRange(
                new string[] {
                    "Developer/JanusExporter/Private",
					// ... add other private include paths required here ...
				}
                );

            PublicDependencyModuleNames.AddRange(
                new string[]
                {
                    "Core",
                    "CoreUObject",
                    "Engine",
                    "Slate",
                    "UnrealEd",
                    "UElibPNG",
                    "MaterialUtilities",
                    "RenderCore",
                    "RHI"
                }
                );

            PrivateDependencyModuleNames.AddRange(
                new string[]
                {
                    "Engine",
                    "InputCore",
                    "SlateCore",
                    "PropertyEditor",
                    "LevelEditor",
                    "MaterialUtilities",
                    "RenderCore",
                    "RHI",
                    "MovieScene"
                }
                );

            DynamicallyLoadedModuleNames.AddRange(
                new string[]
                {
					// ... add any modules that your module loads dynamically here ...
				}
                );

            //AddThirdPartyPrivateStaticDependencies(Target, "FBX");

            // openexr
            if (Target.Platform == UnrealTargetPlatform.Win64 || Target.Platform == UnrealTargetPlatform.Win32 || Target.Platform == UnrealTargetPlatform.Mac)
            {
                bool bDebug = (Target.Configuration == UnrealTargetConfiguration.Debug && BuildConfiguration.bDebugBuildsActuallyUseDebugCRT);
                string LibDir = UEBuildConfiguration.UEThirdPartySourceDirectory + "openexr/Deploy/lib/";
                string Platform;
                switch (Target.Platform)
                {
                    case UnrealTargetPlatform.Win64:
                        Platform = "x64";
                        LibDir += "VS" + WindowsPlatform.GetVisualStudioCompilerVersionName() + "/";
                        break;
                    case UnrealTargetPlatform.Win32:
                        Platform = "Win32";
                        LibDir += "VS" + WindowsPlatform.GetVisualStudioCompilerVersionName() + "/";
                        break;
                    case UnrealTargetPlatform.Mac:
                        Platform = "Mac";
                        bDebug = false;
                        break;
                    default:
                        return;
                }
                LibDir = LibDir + "/" + Platform;
                LibDir = LibDir + "/Static" + (bDebug ? "Debug" : "Release");
                PublicLibraryPaths.Add(LibDir);

                if (Target.Platform == UnrealTargetPlatform.Win64 || Target.Platform == UnrealTargetPlatform.Win32)
                {
                    PublicAdditionalLibraries.AddRange(
                        new string[] {
                        "Half.lib",
                        "Iex.lib",
                        "IlmImf.lib",
                        "IlmThread.lib",
                        "Imath.lib",
                        }
                    );
                }
                else if (Target.Platform == UnrealTargetPlatform.Mac)
                {
                    PublicAdditionalLibraries.AddRange(
                        new string[] {
                        LibDir + "/libHalf.a",
                        LibDir + "/libIex.a",
                        LibDir + "/libIlmImf.a",
                        LibDir + "/libIlmThread.a",
                        LibDir + "/libImath.a",
                        }
                    );
                }

                PublicSystemIncludePaths.AddRange(
                    new string[] {
                    UEBuildConfiguration.UEThirdPartySourceDirectory + "openexr/Deploy/include",
                    }
                );
            }

            // ZLIB
            string zlibPath = UEBuildConfiguration.UEThirdPartySourceDirectory + "zlib/v1.2.8/";

            // TODO: recompile for consoles and mobile platforms
            string OldzlibPath = UEBuildConfiguration.UEThirdPartySourceDirectory + "zlib/zlib-1.2.5/";

            if (Target.Platform == UnrealTargetPlatform.Win64)
            {
                string platform = "/Win64/VS" + WindowsPlatform.GetVisualStudioCompilerVersionName();
                PublicIncludePaths.Add(zlibPath + "include" + platform);
                PublicLibraryPaths.Add(zlibPath + "lib" + platform);
                PublicAdditionalLibraries.Add("zlibstatic.lib");
            }

            else if (Target.Platform == UnrealTargetPlatform.Win32 ||
                    (Target.Platform == UnrealTargetPlatform.HTML5 && Target.Architecture == "-win32")) // simulator
            {
                string platform = "/Win32/VS" + WindowsPlatform.GetVisualStudioCompilerVersionName();
                PublicIncludePaths.Add(zlibPath + "include" + platform);
                PublicLibraryPaths.Add(zlibPath + "lib" + platform);
                PublicAdditionalLibraries.Add("zlibstatic.lib");
            }

            else if (Target.Platform == UnrealTargetPlatform.Mac)
            {
                string platform = "/Mac/";
                PublicIncludePaths.Add(zlibPath + "include" + platform);
                // OSX needs full path
                PublicAdditionalLibraries.Add(zlibPath + "lib" + platform + "libz.a");
            }

            else if (Target.Platform == UnrealTargetPlatform.IOS ||
                     Target.Platform == UnrealTargetPlatform.TVOS)
            {
                PublicIncludePaths.Add(OldzlibPath + "inc");
                PublicAdditionalLibraries.Add("z");
            }

            else if (Target.Platform == UnrealTargetPlatform.Android)
            {
                PublicIncludePaths.Add(OldzlibPath + "inc");
                PublicAdditionalLibraries.Add("z");
            }

            else if (Target.Platform == UnrealTargetPlatform.HTML5)
            {
                string OpimizationSuffix = "";
                if (UEBuildConfiguration.bCompileForSize)
                {
                    OpimizationSuffix = "_Oz";
                }
                else
                {
                    if (Target.Configuration == UnrealTargetConfiguration.Development)
                    {
                        OpimizationSuffix = "_O2";
                    }
                    else if (Target.Configuration == UnrealTargetConfiguration.Shipping)
                    {
                        OpimizationSuffix = "_O3";
                    }
                }
                PublicIncludePaths.Add(OldzlibPath + "Inc");
                PublicAdditionalLibraries.Add(OldzlibPath + "Lib/HTML5/zlib" + OpimizationSuffix + ".bc");
            }

            else if (Target.Platform == UnrealTargetPlatform.Linux)
            {
                string platform = "/Linux/" + Target.Architecture;
                PublicIncludePaths.Add(zlibPath + "include" + platform);
                PublicAdditionalLibraries.Add(zlibPath + "/lib/" + platform + ((Target.LinkType == TargetLinkType.Monolithic) ? "/libz" : "/libz_fPIC") + ".a");
            }

            else if (Target.Platform == UnrealTargetPlatform.PS4)
            {
                PublicIncludePaths.Add(OldzlibPath + "Inc");
                PublicLibraryPaths.Add(OldzlibPath + "Lib/PS4");
                PublicAdditionalLibraries.Add("z");
            }
            else if (Target.Platform == UnrealTargetPlatform.XboxOne)
            {
                // Use reflection to allow type not to exist if console code is not present
                System.Type XboxOnePlatformType = System.Type.GetType("UnrealBuildTool.XboxOnePlatform,UnrealBuildTool");
                if (XboxOnePlatformType != null)
                {
                    System.Object VersionName = XboxOnePlatformType.GetMethod("GetVisualStudioCompilerVersionName").Invoke(null, null);
                    PublicIncludePaths.Add(OldzlibPath + "Inc");
                    PublicLibraryPaths.Add(OldzlibPath + "Lib/XboxOne/VS" + VersionName.ToString());
                    PublicAdditionalLibraries.Add("zlib125_XboxOne.lib");
                }
            }
            else if (Target.Platform == UnrealTargetPlatform.Switch)
            {
                PublicIncludePaths.Add(OldzlibPath + "inc");
                PublicAdditionalLibraries.Add(System.IO.Path.Combine(OldzlibPath, "Lib/Switch/libz.a"));
            }
        }
    }
}
