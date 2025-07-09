using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace Aviad
{
    public class AviadInteropManager
    {
        private static IAviadGeneration _instance = null;
        private static IWebGLUtilities _webglInstance = null;
        private static bool _isInitialized = false;
        private static readonly string binPathAffix = "Aviad/bin";
        private static readonly Dictionary<string, (List<string> dependencyPaths, string mainLibraryPath)> LibraryItems =
            new()
            {
                ["x86_64"] = (
                    new List<string> { "ggml-base.dll", "ggml-cpu.dll", "ggml.dll", "llama.dll" },
                    "aviad-main.dll"
                ),
                ["macOS"] = (
                    new List<string> { "libggml.dylib", "libllama.dylib" },
                    "libaviad-main.dylib"
                ),
            };

        public bool IsReady => _isInitialized;

        public static void Initialize(Action<bool> callback = null)
        {
            if (!_isInitialized)
            {
                try
                {
                    InternalInitialize(callback);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AviadInteropManager] Failed to initialize AviadGeneration instance: " + ex);
                }
            }
        }

        public static IAviadGeneration Instance
        {
            get
            {
                if (!_isInitialized)
                    return null;
                return _instance;
            }
        }

        public static IWebGLUtilities WebGLInstance
        {
            get
            {
                if (!_isInitialized)
                    return null;
                return _webglInstance;
            }
        }

        private static void SetInitialized()
        {
            _isInitialized = true;
        }

        private static void InternalInitialize(Action<bool> callback = null)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var rawInstance = new AviadGenerationWebgl();
            Action<bool> onStarted = success =>
            {
                if (success)
                {
                    SetInitialized();
                }
                callback?.Invoke(success);
            };
            rawInstance.Start(onStarted);
            _webglInstance = rawInstance;
            _instance = rawInstance;
#else
            List<string> dependencyPaths;
            string mainLibraryPath;
            (dependencyPaths, mainLibraryPath) = GetPlatformLibraryPaths();
            var rawInstance = new AviadGenerationLoadedLibrary(mainLibraryPath, dependencyPaths);
            // TODO: Avoid blocking if possible.
            bool result = rawInstance.SafeEnsureLoaded();
            if (!result)
            {
                callback?.Invoke(false);
                return;
            }
            _instance = rawInstance;
            SetInitialized();
            callback?.Invoke(true);
#endif
        }

        private static (List<string> dependencyPaths, string mainLibraryPath) GetPlatformLibraryPaths()
        {
            string platformKey;

            if (PlatformConfiguration.IsWindows && PlatformConfiguration.Is64Bit)
            {
                platformKey = "x86_64";
            }
            else if (PlatformConfiguration.IsMac)
            {
                platformKey = "macOS";
            }
            else
            {
                throw new PlatformNotSupportedException("Unsupported platform for Aviad runtime libraries.");
            }
            if (!LibraryItems.TryGetValue(platformKey, out var libraryData))
            {
                throw new InvalidOperationException($"No library data found for platform: {platformKey}");
            }
            string basePath = Path.Combine(Application.streamingAssetsPath, binPathAffix, platformKey);
            var fullDependencyPaths = new List<string>();
            foreach (var filename in libraryData.dependencyPaths)
            {
                fullDependencyPaths.Add(Path.Combine(basePath, filename));
            };
            string fullMainLibraryPath = Path.Combine(basePath, libraryData.mainLibraryPath);
            return (fullDependencyPaths, fullMainLibraryPath);
        }
    }
}