using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Aviad
{
    public class TTSRunner : MonoBehaviour
    {
        [Header("TTS Settings")]
        [SerializeField] private string directory;
#if UNITY_EDITOR
        [Tooltip("Select a folder from the Assets to use as the TTS directory.")]
        [SerializeField] private DefaultAsset directoryAsset;
#endif
        [SerializeField] private int[] refExamples;
        [SerializeField] private bool initializeOnAwake = true;
        [SerializeField] private bool autoUnloadOnDestroy = true;
        [SerializeField] private int maxAudioSamples = 240000;

        private bool ttsLoaded;

        private void Awake()
        {
            if (initializeOnAwake)
            {
                ModelInstanceManager.Initialize();
            }
        }

        private bool TryGetLibrary(out BackgroundNativeLibrary lib)
        {
            lib = ModelInstanceManager.Instance as BackgroundNativeLibrary;
            if (lib == null)
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                PackageLogger.Warning("TTS is not supported on WebGL.");
#else
                if (!ModelInstanceManager.IsReady)
                {
                    PackageLogger.Warning("ModelInstanceManager is not initialized yet.");
                }
                else
                {
                    PackageLogger.Warning("Underlying instance is not BackgroundNativeLibrary. TTS unavailable.");
                }
#endif
                return false;
            }
            return true;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (directoryAsset != null)
            {
                var path = AssetDatabase.GetAssetPath(directoryAsset);
                if (!string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path))
                {
                    directory = path;
                }
                else
                {
                    PackageLogger.Warning("Selected asset is not a folder. Please select a folder under Assets/.");
                    directory = string.Empty;
                    directoryAsset = null;
                }
            }
        }
#endif

        private string ResolveDirectory()
        {
#if UNITY_EDITOR
            if (directoryAsset != null)
            {
                var path = AssetDatabase.GetAssetPath(directoryAsset);
                if (!string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path))
                {
                    return path;
                }
            }
#endif
            return directory;
        }

        public void LoadTTS(Action<bool> onDone = null)
        {
            if (!ModelInstanceManager.IsReady)
            {
                ModelInstanceManager.Initialize(success =>
                {
                    if (!success)
                    {
                        onDone?.Invoke(false);
                        return;
                    }
                    LoadTTS(onDone);
                });
                return;
            }

            if (!TryGetLibrary(out var lib))
            {
                onDone?.Invoke(false);
                return;
            }

            var p = new TTSParams(ResolveDirectory(), refExamples);
            lib.LoadTTS(p, success =>
            {
                ttsLoaded = success;
                onDone?.Invoke(success);
            });
        }

        public void LoadTTS(TTSParams parameters, Action<bool> onDone = null)
        {
            if (parameters == null)
            {
                onDone?.Invoke(false);
                return;
            }

            // Keep fields in sync for inspector visibility.
            directory = parameters.directory;
            refExamples = parameters.refExamples;

            if (!ModelInstanceManager.IsReady)
            {
                ModelInstanceManager.Initialize(success =>
                {
                    if (!success)
                    {
                        onDone?.Invoke(false);
                        return;
                    }
                    LoadTTS(parameters, onDone);
                });
                return;
            }

            if (!TryGetLibrary(out var lib))
            {
                onDone?.Invoke(false);
                return;
            }

            lib.LoadTTS(parameters, success =>
            {
                ttsLoaded = success;
                onDone?.Invoke(success);
            });
        }

        public void GenerateTTS(string text, Action<float[]> onAudio, Action<bool> onDone = null)
        {
            if (!ttsLoaded)
            {
                PackageLogger.Warning("GenerateTTS called before TTS is loaded.");
                onDone?.Invoke(false);
                return;
            }

            if (!TryGetLibrary(out var lib))
            {
                onDone?.Invoke(false);
                return;
            }

            lib.GenerateTTS(text, maxAudioSamples, onAudio, onDone);
        }

        public void UnloadTTS(Action<bool> onDone = null)
        {
            if (!TryGetLibrary(out var lib))
            {
                onDone?.Invoke(false);
                return;
            }

            lib.UnloadTTS(success =>
            {
                if (success) ttsLoaded = false;
                onDone?.Invoke(success);
            });
        }

        private void OnDestroy()
        {
            if (autoUnloadOnDestroy && ttsLoaded)
            {
                // Fire-and-forget; callback will be dispatched to main thread by BackgroundNativeLibrary
                UnloadTTS();
            }
        }
    }
}
