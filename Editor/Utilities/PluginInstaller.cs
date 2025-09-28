using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using System.IO;
using System;

/// https://docs.unity3d.com/6000.0/Documentation/ScriptReference/InitializeOnLoadAttribute.html
/// Asset operations such as asset loading should be avoided in InitializeOnLoad methods.
/// InitializeOnLoad methods are called before asset importing is completed and therefore
/// the asset loading can fail resulting in a null object. To do initialization after a domain
/// reload which requires asset operations use the AssetPostprocessor.OnPostprocessAllAssets
/// callback. This callback supports all asset operations and has a parameter signaling if
/// there was a domain reload. - Alex 6122025 , we should be aware of this if we choose to
/// implement different loading logic in the future relying on more engine native type assets.
namespace Aviad
{
    [InitializeOnLoad]
    public class PluginInstaller : UnityEditor.Editor
    {
        static PluginInstaller()
        {
            RunOnce();
        }

        static void RunOnce()
        {
            if (!Application.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Install(overwriteBin: true);
            }
        }

        public static void Install(bool overwriteBin = true)
        {
            string packagePath = GetPackagePath("ai.aviad.core");
            if (string.IsNullOrEmpty(packagePath))
            {
                // Fallback for unitypackage-based install
                string fallbackPath = Path.Combine(Application.dataPath, "AviadAI");
                if (Directory.Exists(fallbackPath))
                {
                    packagePath = fallbackPath;
                }
                else
                {
                    PackageLogger.Warning("Could not locate package path for ai.aviad.core (expected in Packages/ or Assets/AviadAI/)");
                    return;
                }
            }

            string sourceBinDir = Path.Combine(packagePath, "StreamingAssets/Aviad/bin");
            string destBinDir = Path.Combine(Application.streamingAssetsPath, "Aviad/bin");

            if (!Directory.Exists(sourceBinDir))
            {
                PackageLogger.Warning($"Source bin directory not found at {sourceBinDir}");
                return;
            }

            if (Directory.Exists(destBinDir) && overwriteBin)
            {
                try
                {
                    Directory.Delete(destBinDir, recursive: true);
                }
                catch (Exception ex)
                {
                    PackageLogger.Warning($"Failed to delete existing bin directory: {ex.Message}");
                }
            }

            foreach (var file in Directory.GetFiles(sourceBinDir, "*", SearchOption.AllDirectories))
            {
                if (file.EndsWith(".meta"))
                    continue;

                string relativePath = file.Substring(sourceBinDir.Length + 1);
                string destFile = Path.Combine(destBinDir, relativePath);

                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destFile));
                    File.Copy(file, destFile, overwrite: true);
                }
                catch (Exception ex)
                {
                    PackageLogger.Warning($"Failed to copy '{file}' to '{destFile}': {ex.Message}");
                }
            }

            AssetDatabase.Refresh();
            PackageLogger.Debug("Bin files installed to StreamingAssets/Aviad/bin.");
        }

        private static string GetPackagePath(string packageName)
        {
            var request = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages();
            foreach (var package in request)
            {
                if (package.name == packageName)
                {
                    return package.resolvedPath;
                }
            }

            return null;
        }
    }
}