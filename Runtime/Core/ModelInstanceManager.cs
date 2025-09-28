using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace Aviad
{
    public static class ModelInstanceManager
    {
        private static ITextGeneration _instance = null;
        private static IWebGLUtilities _webglInstance = null;
        private static bool _isInitialized = false;
        public static bool IsReady => _isInitialized;
        private static readonly HashSet<string> _registeredModelIds = new HashSet<string>();
        public static bool EnableNativeLogging => PackageSettings.IsNativeLoggingEnabled;

        private static readonly object _initLock = new object();
        private static bool _isInitializing = false;
        private static readonly List<Action<bool>> _pendingInitCallbacks = new List<Action<bool>>();

        public static void Initialize(Action<bool> callback = null)
        {
            bool shouldStart = false;

            lock (_initLock)
            {
                if (_isInitialized)
                {
                    // Already initialized — call back immediately.
                    callback?.Invoke(true);
                    return;
                }

                // If initialization already in progress, just queue callback.
                if (_isInitializing)
                {
                    if (callback != null)
                        _pendingInitCallbacks.Add(callback);
                    return;
                }

                // Not initialized and not initializing — start init now.
                _isInitializing = true;
                if (callback != null)
                    _pendingInitCallbacks.Add(callback);
                shouldStart = true;
            }

            if (!shouldStart) return;
            try
            {
                InternalInitialize(success =>
                {
                    Action<bool>[] callbacks;
                    lock (_initLock)
                    {
                        _isInitialized = success;
                        _isInitializing = false;

                        // Copy and clear callbacks list inside lock
                        callbacks = _pendingInitCallbacks.ToArray();
                        _pendingInitCallbacks.Clear();
                    }

                    // Fire callbacks outside lock
                    foreach (var cb in callbacks)
                    {
                        try { cb?.Invoke(success); }
                        catch (Exception ex)
                        {
                            PackageLogger.Error($"[AviadManager] Init callback error: {ex}");
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                PackageLogger.Error($"[AviadInteropManager] Failed to initialize AviadGeneration instance: {ex}");
                lock (_initLock)
                {
                    _isInitializing = false;
                    var callbacks = _pendingInitCallbacks.ToArray();
                    _pendingInitCallbacks.Clear();
                    foreach (var cb in callbacks)
                        cb?.Invoke(false);
                }
            }
        }

        public static ITextGeneration Instance
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

        private static void MaybeEnableLogging()
        {
            if (EnableNativeLogging && _instance != null)
            {
                _instance.SetLoggingEnabled(success =>
                {
                    PackageLogger.Debug("[AviadManager] Logging setup success: " + success);
                });
            }
        }

        private static void InternalInitialize(Action<bool> callback = null)
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var rawInstance = new AviadGenerationWebgl();
            Action<bool> onStarted = success =>
            {
                if (success)
                {
                    MaybeEnableLogging();
                    // TODO: make sure logging enabled by now.
                    SetInitialized();
                }
                callback?.Invoke(success);

            };
            rawInstance.Start(onStarted);
            _webglInstance = rawInstance;
            _instance = rawInstance;
#else
            var rawInstance = new BackgroundNativeLibrary(new NativeLibrary());
            bool result = rawInstance.SafeEnsureLoaded();
            if (!result)
            {
                callback?.Invoke(false);
                return;
            }
            _instance = rawInstance;
            MaybeEnableLogging();
            SetInitialized();
            callback?.Invoke(_instance != null);
#endif
        }

        public static void Cleanup()
        {
            if (_instance != null)
            {
                _instance.Dispose();
                _instance = null;
                _isInitialized = false;
            }
        }

        /// <summary>
        /// Registers a unique modelId.
        /// If a prefix is provided and the base id exists, it appends a numeric suffix until unique.
        /// Returns the unique id that was registered, or null if registration failed.
        /// </summary>
        public static string RegisterModelId(string modelIdPrefix = "")
        {
            if (string.IsNullOrWhiteSpace(modelIdPrefix))
            {
                modelIdPrefix = "model";
            }

            string candidate = modelIdPrefix;

            int counter = 1;
            // If already exists, append suffix until unique
            while (_registeredModelIds.Contains(candidate))
            {
                candidate = $"{modelIdPrefix}_{counter++}";
            }

            _registeredModelIds.Add(candidate);
            return candidate;
        }

        public static bool HasModelId(string modelId)
        {
            return _registeredModelIds.Contains(modelId);
        }

        public static bool RemoveModelId(string modelId)
        {
            return _registeredModelIds.Remove(modelId);
        }

        public static IReadOnlyCollection<string> GetAllModelIds()
        {
            return _registeredModelIds;
        }

        public static void ClearModelIds()
        {
            _registeredModelIds.Clear();
        }
    }
}