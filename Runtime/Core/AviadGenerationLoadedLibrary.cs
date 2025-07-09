using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace Aviad
{
    public class AviadGenerationLoadedLibrary : IAviadGeneration
    {
        private IntPtr _library;
        private bool _initialized;
        private bool _modelInitialized = false;
        private readonly List<IntPtr> _dependentLibraries = new();
        private readonly object _usageLock = new object();

        // Constructor arguments
        private readonly List<string> _dependencyPaths;
        private readonly string _mainLibraryPath;

        public AviadGenerationLoadedLibrary(string mainLibraryPath, List<string> dependencyPaths = null)
        {
            if (string.IsNullOrEmpty(mainLibraryPath))
                throw new ArgumentNullException(nameof(mainLibraryPath));

            _mainLibraryPath = mainLibraryPath;
            _dependencyPaths = dependencyPaths ?? new List<string>();
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool SetLogCallbackDelegate(AviadCallbacks.LogCallbackWithLevelDelegate callback);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool InitializeGenerationModelDelegate(ref NativeLlamaModelParams modelParams);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool ShutdownGenerationModelDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool LoadContextDelegate(string contextKey, string chatTemplate);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool UnloadContextDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool CacheContextDelegate();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool GetContextDelegate(
            string contextKey,
            ref NativeLlamaMessageSequence sequence,
            int maxTurnCount,
            int maxStringLength);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool InitContextDelegate(string contextKey, ref NativeLlamaMessageSequence messages);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool AddTurnDelegate(string contextKey, string role, string content);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool AppendToContextDelegate(string contextKey, string content);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool CopyContextDelegate(string sourceKey, string targetKey);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool GenerateResponseStreamingDelegate(
            string contextKey,
            string outContextKey,
            ref NativeLlamaGenerationConfig config,
            AviadCallbacks.TokenStreamCallback onToken,
            AviadCallbacks.StreamDoneCallback onDone,
            int chunkSize);

        private SetLogCallbackDelegate _setLogCallback;
        private InitializeGenerationModelDelegate _initializeModel;
        private ShutdownGenerationModelDelegate _shutdownModel;
        private LoadContextDelegate _loadContext;
        private UnloadContextDelegate _unloadContext;
        private CacheContextDelegate _cacheContext;
        private GetContextDelegate _getContext;
        private InitContextDelegate _initContext;
        private AddTurnDelegate _addTurn;
        private AppendToContextDelegate _appendToContext;
        private CopyContextDelegate _copyContext;
        private GenerateResponseStreamingDelegate _generateStreaming;

        private static readonly AviadCallbacks.LogCallbackWithLevelDelegate DefaultLogCallback = (level, message) =>
        {
            switch (level)
            {
                case 0: // Info
                    Debug.Log($"[Aviad] {message}");
                    break;
                case 1: // Warning
                    Debug.LogWarning($"[Aviad] {message}");
                    break;
                case 2: // Error
                    Debug.LogWarning($"[Aviad] {message}");
                    break;
                default:
                    Debug.Log($"[Aviad Level {level}] {message}");
                    break;
            }
        };

        public bool SetLoggingEnabled()
        {
            if (!SafeEnsureLoaded()) return false;
            lock (_usageLock)
            {
                try
                {
                    return _setLogCallback(DefaultLogCallback);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"AviadGenerationLoadedLibrary.SetLoggingEnabled failed: {ex.Message}");
                    return false;
                }
            }
        }

        public bool InitializeModel(LlamaModelParams modelParams, Action<bool> onComplete)
        {
            if (_modelInitialized) return true;
            if (!SafeEnsureLoaded()) return false;
            if (modelParams == null) return false;
            Task.Run(() =>
            {
                bool result = false;
                lock (_usageLock)
                {
                    try
                    {
                        var nativeParams = modelParams.ToStruct();
                        Debug.Log(modelParams.modelPath);
                        Debug.Log(modelParams.ToStruct().model_path);
                        result = _initializeModel(ref nativeParams);
                        _modelInitialized = result;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"AviadGenerationLoadedLibrary.InitializeModel failed: {ex.Message}");
                    }
                }
                onComplete?.Invoke(result);
            });
            return true;
        }

        public bool ShutdownModel(Action<bool> onComplete)
        {
            if (!SafeEnsureLoaded())
            {
                onComplete?.Invoke(false);
                return false;
            }
            if (!_modelInitialized)
            {
                onComplete?.Invoke(true);
                return true;
            }
            Task.Run(() =>
            {
                bool result = false;
                lock (_usageLock)
                {
                    try
                    {
                        result = _shutdownModel();
                        _modelInitialized = false;

                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"AviadGenerationLoadedLibrary.ShutdownModel failed: {ex.Message}");
                    }
                }
                onComplete?.Invoke(result);
            });
            return true;
        }

        public bool LoadContext(string contextKey, string templateString, Action<bool> onDone = null)
        {
            if (!SafeEnsureLoaded()) return false;
            lock (_usageLock)
            {
                try
                {
                    bool result = _loadContext(contextKey, templateString);
                    onDone?.Invoke(result);
                    return result;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"AviadGenerationLoadedLibrary.LoadContext failed: {ex.Message}");
                    return false;
                }
            }
        }

        public bool UnloadActiveContext(Action<bool> onDone = null)
        {
            if (!SafeEnsureLoaded()) return false;
            lock (_usageLock)
            {
                try
                {
                    bool result = _unloadContext();
                    onDone?.Invoke(result);
                    return result;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"AviadGenerationLoadedLibrary.UnloadActiveContext failed: {ex.Message}");
                    return false;
                }
            }
        }

        public bool CacheContext(Action<bool> onDone = null)
        {
            if (!SafeEnsureLoaded()) return false;
            lock (_usageLock)
            {
                try
                {
                    bool result = _cacheContext();
                    onDone?.Invoke(result);
                    return result;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"AviadGenerationLoadedLibrary.CacheContext failed: {ex.Message}");
                    return false;
                }
            }
        }

        public void GetContext(
            string contextKey,
            int maxTurnCount,
            int maxStringLength,
            Action<LlamaMessageSequence> onResult)
        {
            if (!SafeEnsureLoaded()) return;
            lock (_usageLock)
            {
                try
                {
                    using (var preallocator = new LlamaMessageSequencePreallocator(maxTurnCount, maxStringLength))
                    {
                        var outSeq = preallocator.GetLlamaMessageSequence();
                        _getContext(contextKey, ref outSeq, maxTurnCount, maxStringLength);
                        onResult?.Invoke(new LlamaMessageSequence(outSeq));
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"AviadGenerationLoadedLibrary.GetContext failed: {ex.Message}");
                }
            }
        }

        public bool InitContext(string contextKey, LlamaMessageSequence messages, Action<bool> onDone = null)
        {
            if (!SafeEnsureLoaded()) return false;
            lock (_usageLock)
            {
                try
                {
                    using (var wrapper = messages.ToNative())
                    {
                        var nativeSequence = wrapper.Native;
                        bool result = _initContext(contextKey, ref nativeSequence);
                        onDone?.Invoke(result);
                        return result;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"AviadGenerationLoadedLibrary.InitContext failed: {ex.Message}");
                    return false;
                }
            }
        }
        public bool AddTurnToContext(string contextKey, string role, string content, Action<bool> onDone = null)
        {
            if (!SafeEnsureLoaded()) return false;
            lock (_usageLock)
            {
                try
                {
                    bool result = _addTurn(contextKey, role, content);
                    onDone?.Invoke(result);
                    return result;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"AviadGenerationLoadedLibrary.AddTurnToContext failed: {ex.Message}");
                    return false;
                }
            }
        }

        public bool AppendToContext(string contextKey, string content, Action<bool> onDone = null)
        {
            if (!SafeEnsureLoaded()) return false;
            lock (_usageLock)
            {
                try
                {
                    bool result = _appendToContext(contextKey, content);
                    onDone?.Invoke(result);
                    return result;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"AviadGenerationLoadedLibrary.AppendToContext failed: {ex.Message}");
                    return false;
                }
            }
        }

        public bool CopyContext(string sourceContextKey, string targetContextKey, Action<bool> onDone = null)
        {
            if (!SafeEnsureLoaded()) return false;
            lock (_usageLock)
            {
                try
                {
                    bool result = _copyContext(sourceContextKey, targetContextKey);
                    onDone?.Invoke(result);
                    return result;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"AviadGenerationLoadedLibrary.CopyContext failed: {ex.Message}");
                    return false;
                }
            }
        }

        public bool GenerateResponseStreaming(
            string contextKey,
            string outContextKey,
            LlamaGenerationConfig config,
            Action<string> onToken,
            Action<bool> onDone,
            int chunkSize)
        {
            if (!SafeEnsureLoaded()) return false;
            Task.Run(() =>
            {
                lock (_usageLock)
                {
                    try
                    {
                        var nativeConfig = config.ToStruct();
                        AviadCallbacks.TokenStreamCallback onTokenCallback = AviadCallbacks.TokenStreamCallbackFromAction(onToken);
                        AviadCallbacks.StreamDoneCallback onDoneCallback = AviadCallbacks.StreamDoneCallbackFromAction(onDone);
                        _generateStreaming(
                            contextKey, outContextKey, ref nativeConfig, onTokenCallback, onDoneCallback, chunkSize);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"AviadGenerationLoadedLibrary.GenerateResponseStreaming failed: {ex.Message}");
                    }
                }
            });
            return true;
        }

        public bool SafeEnsureLoaded()
        {
            try
            {
                EnsureLoaded();
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"AviadGenerationLoadedLibrary.EnsureLoaded failed: {ex.Message}");
                FreeLibrary();
                return false;
            }
        }

        public void EnsureLoaded()
        {
            if (_initialized) return;

            lock (_usageLock)
            {
                string arch = Environment.Is64BitProcess ? "x86_64" : "x86";
                string basePath = Path.Combine(Application.streamingAssetsPath, "Aviad", "bin", arch);

                foreach (var depPath in _dependencyPaths)
                {
                    if (string.IsNullOrEmpty(depPath))
                    {
                        Debug.LogWarning("[Aviad] Skipping empty dependency path");
                        continue;
                    }

                    if (!File.Exists(depPath))
                    {
                        throw new FileNotFoundException($"Dependency library not found: {depPath}");
                    }

                    var handle = LibraryLoader.LoadLibrary(depPath);
                    if (handle == IntPtr.Zero)
                    {
                        int err = Marshal.GetLastWin32Error();
                        if (err == 126)
                        {
                            Debug.LogError("DLL loading failed likely due to missing dependencies or misordered load order.");
                        }
                        throw new DllNotFoundException($"Failed to load dependency: {depPath} (Error {err})");

                    }

                    _dependentLibraries.Add(handle);
                    Debug.Log($"[Aviad] Loaded dependency: {Path.GetFileName(depPath)}");
                }

                if (!File.Exists(_mainLibraryPath))
                {
                    throw new FileNotFoundException($"Main library not found: {_mainLibraryPath}");
                }

                _library = LibraryLoader.LoadLibrary(_mainLibraryPath);
                if (_library == IntPtr.Zero)
                {
                    int err = Marshal.GetLastWin32Error();
                    throw new DllNotFoundException($"Failed to load main library: {_mainLibraryPath} (Error {err})");
                }

                Debug.Log($"[Aviad] Loaded main library: {Path.GetFileName(_mainLibraryPath)}");

                _setLogCallback = LibraryLoader.GetSymbolDelegate<SetLogCallbackDelegate>(_library, "set_log_callback");
                _initializeModel =
                    LibraryLoader.GetSymbolDelegate<InitializeGenerationModelDelegate>(_library,
                        "initialize_generation_model");
                _shutdownModel =
                    LibraryLoader.GetSymbolDelegate<ShutdownGenerationModelDelegate>(_library,
                        "shutdown_generation_model");
                _getContext = LibraryLoader.GetSymbolDelegate<GetContextDelegate>(_library, "get_context");
                _initContext = LibraryLoader.GetSymbolDelegate<InitContextDelegate>(_library, "init_context");
                _addTurn = LibraryLoader.GetSymbolDelegate<AddTurnDelegate>(_library, "add_turn_to_context");
                _appendToContext =
                    LibraryLoader.GetSymbolDelegate<AppendToContextDelegate>(_library, "append_to_context");
                _copyContext = LibraryLoader.GetSymbolDelegate<CopyContextDelegate>(_library, "copy_context");
                _loadContext = LibraryLoader.GetSymbolDelegate<LoadContextDelegate>(_library, "load_context");
                _unloadContext =
                    LibraryLoader.GetSymbolDelegate<UnloadContextDelegate>(_library, "unload_active_context");
                _cacheContext = LibraryLoader.GetSymbolDelegate<CacheContextDelegate>(_library, "cache_context");
                _generateStreaming =
                    LibraryLoader.GetSymbolDelegate<GenerateResponseStreamingDelegate>(_library,
                        "generate_response_streaming");

                _initialized = true;
            }
        }

        public void FreeLibrary()
        {
            _initialized = false;
            _setLogCallback = null;
            _initializeModel = null;
            _shutdownModel = null;
            _loadContext = null;
            _unloadContext = null;
            _cacheContext = null;
            _getContext = null;
            _initContext = null;
            _addTurn = null;
            _appendToContext = null;
            _copyContext = null;
            _generateStreaming = null;

            foreach (var handle in _dependentLibraries)
            {
                if (handle != IntPtr.Zero)
                {
                    LibraryLoader.FreeLibrary(handle);
                }
            }

            _dependentLibraries.Clear();

            if (_library != IntPtr.Zero)
            {
                LibraryLoader.FreeLibrary(_library);
                _library = IntPtr.Zero;
            }

            Debug.Log("[AviadGenerationLoadedLibrary] Native library released");
        }
        public void Cleanup()
        {
            Action<bool> onShutdown = success =>
            {
                Debug.LogFormat("[AviadGenerationLoadedLibrary] Model has been shutdown. Success: {0}", success);
                FreeLibrary();
            };
            ShutdownModel(onShutdown);
        }
    }
}