using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace JanusVR
{
    public class ProbeExporter : Exporter
    {
        private JanusRoom room;
        private Cubemap skyProbe;

        public override void Initialize(JanusRoom room)
        {
            this.room = room;
        }

        public override void PreExport()
        {
            skyProbe = null;
            if (room.EnvironmentProbeOverride)
            {
                skyProbe = room.EnvironmentProbeOverride.bakedTexture as Cubemap;
            }
            else
            { 
                // find the Reflection probe for the sky, if we have one
#if !UNITY_5_0
                string lightMapsFolder = UnityUtil.GetLightmapsFolder();
                DirectoryInfo lightMapsDir = new DirectoryInfo(lightMapsFolder);
                Cubemap cubemap = RenderSettings.customReflection;
                if (cubemap == null)
                {
                    // search thorugh files
#if !UNITY_5_3
                    if (lightMapsDir.Exists)
                    {
                        // on Unity 5.1 the Probe is called Skybox instead of Reflection Probe, so we search
                        // for anything with probe in the name
                        FileInfo[] probes = lightMapsDir.GetFiles("*Probe-*");
                        FileInfo first = probes.FirstOrDefault();
                        if (first == null)
                        {
                            return;
                        }

                        string probePath = Path.Combine(lightMapsFolder, first.Name);
                        cubemap = AssetDatabase.LoadAssetAtPath<Cubemap>(probePath);
                    }
#endif
                }
                skyProbe = cubemap;
#endif
            }
        }

        public override void Export()
        {
            if (!room.EnvironmentProbeExport || skyProbe == null)
            {
                return;
            }

            EditorUtility.DisplayProgressBar("Janus VR Exporter", "Generating Radiance map...", 0.0f);
            room.CubemapIrradiance = GenerateCmftIrrad(room.EnvironmentProbeIrradResolution, skyProbe, "irradiance");
            EditorUtility.DisplayProgressBar("Janus VR Exporter", "Generating Irradiance map...", 0.0f);
            room.CubemapRadiance = GenerateCmftRad(room.EnvironmentProbeRadResolution, skyProbe, "radiance");
        }

        public override void Cleanup()
        {

        }

        private AssetImage GenerateCmftRad(int res, Cubemap cubemap, string forceName = "")
        {
            string path = AssetDatabase.GetAssetPath(cubemap);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            // remove assets
            path = path.Remove(0, path.IndexOf('/'));

            string appPath = Application.dataPath;
            string fullPath = appPath + path;
            string name = forceName;
            if (string.IsNullOrEmpty(name))
            {
                name = cubemap.name + "_radiance";
            }

            // we know the format were exporting, so we set it now
            AssetImage data = new AssetImage();
            data.id = name;
            data.src = name + ".dds";
            room.AddAssetImage(data);

            if (room.ExportOnlyHtml)
            {
                return data;
            }

            string exportPath = room.RootFolder;
            string radPath = Path.Combine(exportPath, name);

            string dstFaceSize = res.ToString(CultureInfo.InvariantCulture);// "256";
            string excludeBase = "false";
            string mipCount = "9";
            string glossScale = "10";
            string glossBias = "1";
            string lightingModel = "phongbrdf";
            string inputGammaNumerator = "1.0";
            string inputGammaDenominator = "1.0";
            string outputGammaNumerator = "1.0";
            string outputGammaDenominator = "1.0";
            string generateMipChain = "false";
            string numCpuProcessingThreads = "1";


            StringBuilder builder = new StringBuilder();
            builder.Append("--input \"" + fullPath + "\"");
            builder.Append(" --srcFaceSize " + cubemap.width);
            builder.Append(" --filter radiance");
            builder.Append(" --dstFaceSize " + dstFaceSize);
            builder.Append(" --excludeBase " + excludeBase);
            builder.Append(" --mipCount " + mipCount);
            builder.Append(" --glossBias " + glossBias);
            builder.Append(" --glossScale " + glossScale);
            builder.Append(" --lightingModel " + lightingModel);
            builder.Append(" --numCpuProcessingThreads " + numCpuProcessingThreads);
            builder.Append(" --inputGammaNumerator " + inputGammaNumerator);
            builder.Append(" --inputGammaDenominator " + inputGammaDenominator);
            builder.Append(" --outputGammaNumerator " + outputGammaNumerator);
            builder.Append(" --outputGammaDenominator " + outputGammaDenominator);
            builder.Append(" --generateMipChain " + generateMipChain);
            builder.Append(" --outputNum 1");
            builder.Append(" --output0 \"" + radPath + "\"");
            builder.Append(" --output0params dds,bgra8,cubemap");
            string cmd = builder.ToString();

            // we refer by namespace so Unity never really imports CMFT on Unity 5.0
            CMFT.CmftInterop.DoExecute(cmd);

            
            return data;
        }
        private AssetImage GenerateCmftIrrad(int size, Cubemap cubemap, string forceName = "")
        {
            string path = AssetDatabase.GetAssetPath(cubemap);
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            // remove assets
            path = path.Remove(0, path.IndexOf('/'));

            string appPath = Application.dataPath;
            string fullPath = appPath + path;
            string name = forceName;
            if (string.IsNullOrEmpty(name))
            {
                name = cubemap.name + "_irradiance";
            }

            AssetImage data = new AssetImage();
            data.id = name;
            data.src = name + ".dds";
            room.AddAssetImage(data);

            if (room.ExportOnlyHtml)
            {
                return data;
            }

            string exportPath = room.RootFolder;
            string irradPath = Path.Combine(exportPath, name);

            string cmd = "--input \"" + fullPath
                + "\" --srcFaceSize " + cubemap.width + " --dstFaceSize" + size
                + " --filter irradiance --outputNum 1 --output0 \""
                + irradPath + "\" --output0params dds,bgra8,cubemap";
            CMFT.CmftInterop.DoExecute(cmd);

            return data;
        }
    }
}
