using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using UnityEngine;

namespace Aviad
{
    public class NativeLibrary : ITextGeneration
    {
        private IntPtr library;
        private bool initialized;
        private bool modelInitialized = false;
        private readonly List<IntPtr> dependentLibraries = new();
        private readonly string binPathAffix = "Aviad/bin";
        private static readonly HashSet<string> _registeredModelIds = new HashSet<string>();

        private readonly Dictionary<string, (List<string> dependencyPaths, string mainLibraryPath)> LibraryItems =
            new()
            {
                ["x86_64"] = (
                    new List<string> { },
                    "aviad_main.dll"
                ),
                ["macOS"] = (
                    new List<string> { },
                    "libaviad_main.dylib"
                ),
            };

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool SetLogCallbackDelegate(
            NativeCallbacks.LogCallbackWithLevelDelegate callback);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool InitializeModelDelegate(
            [MarshalAs(UnmanagedType.LPStr)] string modelId,
            ref NativeLlamaInitializationParams initParams);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool ShutdownModelDelegate(
            [MarshalAs(UnmanagedType.LPStr)] string modelId);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool LoadContextDelegate(
            [MarshalAs(UnmanagedType.LPStr)] string modelId,
            [MarshalAs(UnmanagedType.LPStr)] string contextKey,
            ref NativeChatTemplateParams templateParams);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool UnloadContextDelegate(
            [MarshalAs(UnmanagedType.LPStr)] string modelId);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool CacheContextDelegate(
            [MarshalAs(UnmanagedType.LPStr)] string modelId);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool GetContextDelegate(
            [MarshalAs(UnmanagedType.LPStr)] string contextKey,
            ref NativeLlamaMessageSequence sequence,
            int maxTurnCount,
            int maxStringLength);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool InitContextDelegate(
            [MarshalAs(UnmanagedType.LPStr)] string contextKey,
            ref NativeLlamaMessageSequence messages);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool AddTurnDelegate(
            [MarshalAs(UnmanagedType.LPStr)] string contextKey,
            [MarshalAs(UnmanagedType.LPStr)] string role,
            [MarshalAs(UnmanagedType.LPStr)] string content);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool AppendToContextDelegate(
            [MarshalAs(UnmanagedType.LPStr)] string contextKey,
            [MarshalAs(UnmanagedType.LPStr)] string content);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool CopyContextDelegate(
            [MarshalAs(UnmanagedType.LPStr)] string sourceKey,
            [MarshalAs(UnmanagedType.LPStr)] string targetKey);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool FreeContextDelegate(
            [MarshalAs(UnmanagedType.LPStr)] string contextKey);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool GenerateResponseDelegate(
            [MarshalAs(UnmanagedType.LPStr)] string modelId,
            [MarshalAs(UnmanagedType.LPStr)] string contextKey,
            [MarshalAs(UnmanagedType.LPStr)] string outContextKey,
            ref NativeLlamaGenerationParams config,
            NativeCallbacks.TokenStreamCallback onToken,
            NativeCallbacks.StreamDoneCallback onDone);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool AbortInitializeModelDelegate(
            [MarshalAs(UnmanagedType.LPStr)] string modelId);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool AbortGenerationDelegate(
            [MarshalAs(UnmanagedType.LPStr)] string modelId);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool ComputeEmbeddingsDelegate(
            [MarshalAs(UnmanagedType.LPStr)] string modelId,
            [MarshalAs(UnmanagedType.LPStr)] string context,
            ref NativeLlamaEmbeddingParams embeddingParams,
            IntPtr output);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate uint GetEmbeddingsSizeDelegate(
            [MarshalAs(UnmanagedType.LPStr)] string modelId);

        // --- TTS ---
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool LoadTtsDelegate(
            [MarshalAs(UnmanagedType.LPStr)] string directory,
            [MarshalAs(UnmanagedType.LPArray, ArraySubType = UnmanagedType.I4)] int[] refExamples,
            int refCount);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool GenerateTtsDelegate(
            [MarshalAs(UnmanagedType.LPStr)] string text,
            IntPtr data,
            int maxLength);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool UnloadTtsDelegate();

        private SetLogCallbackDelegate setLogCallback;
        private InitializeModelDelegate initializeModel;
        private ShutdownModelDelegate shutdownModel;
        private LoadContextDelegate loadContext;
        private UnloadContextDelegate unloadContext;
        private CacheContextDelegate cacheContext;
        private GetContextDelegate getContext;
        private InitContextDelegate initContext;
        private AddTurnDelegate addTurn;
        private AppendToContextDelegate appendToContext;
        private CopyContextDelegate copyContext;
        private GenerateResponseDelegate generateResponse;
        private FreeContextDelegate freeContext;
        private AbortInitializeModelDelegate abortInitializeModel;
        private AbortGenerationDelegate abortGeneration;
        private ComputeEmbeddingsDelegate computeEmbeddings;
        private GetEmbeddingsSizeDelegate getEmbeddingsSize;
        private LoadTtsDelegate loadTts;
        private GenerateTtsDelegate generateTts;
        private UnloadTtsDelegate unloadTts;


        public bool SafeEnsureLoaded()
        {
            try
            {
                EnsureLoaded();
                return true;
            }
            catch (Exception ex)
            {
                PackageLogger.Error($"Aviad.NativeLibrary.EnsureLoaded failed: {ex.Message}");
                FreeLibrary();
                return false;
            }
        }

        public void EnsureLoaded()
        {
            if (initialized) return;

            List<string> dependencyPaths;
            string mainLibraryPath;
            (dependencyPaths, mainLibraryPath) = GetPlatformLibraryPaths();
            foreach (var depPath in dependencyPaths)
            {
                if (string.IsNullOrEmpty(depPath))
                {
                    PackageLogger.Warning("[Aviad] Skipping empty dependency path");
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
                        PackageLogger.Error(
                            "DLL loading failed likely due to missing dependencies or misordered load order.");
                    }

                    throw new DllNotFoundException($"Failed to load dependency: {depPath} (Error {err})");

                }

                dependentLibraries.Add(handle);
                PackageLogger.Debug($"Loaded dependency: {Path.GetFileName(depPath)}");
            }

            if (!File.Exists(mainLibraryPath))
            {
                throw new FileNotFoundException($"Main library not found: {mainLibraryPath}");
            }

            library = LibraryLoader.LoadLibrary(mainLibraryPath);
            if (library == IntPtr.Zero)
            {
                int err = Marshal.GetLastWin32Error();
                throw new DllNotFoundException($"Failed to load main library: {mainLibraryPath} (Error {err})");
            }

            PackageLogger.Debug($"Loaded main library: {Path.GetFileName(mainLibraryPath)}");

            setLogCallback = LibraryLoader.GetSymbolDelegate<SetLogCallbackDelegate>(library, "set_log_callback");
            initializeModel =
                LibraryLoader.GetSymbolDelegate<InitializeModelDelegate>(library, "initialize_model");
            shutdownModel =
                LibraryLoader.GetSymbolDelegate<ShutdownModelDelegate>(library, "shutdown_model");
            getContext = LibraryLoader.GetSymbolDelegate<GetContextDelegate>(library, "get_context");
            initContext = LibraryLoader.GetSymbolDelegate<InitContextDelegate>(library, "init_context");
            addTurn = LibraryLoader.GetSymbolDelegate<AddTurnDelegate>(library, "add_turn_to_context");
            appendToContext =
                LibraryLoader.GetSymbolDelegate<AppendToContextDelegate>(library, "append_to_context");
            copyContext = LibraryLoader.GetSymbolDelegate<CopyContextDelegate>(library, "copy_context");
            loadContext = LibraryLoader.GetSymbolDelegate<LoadContextDelegate>(library, "load_context");
            unloadContext =
                LibraryLoader.GetSymbolDelegate<UnloadContextDelegate>(library, "unload_active_context");
            cacheContext = LibraryLoader.GetSymbolDelegate<CacheContextDelegate>(library, "cache_context");
            generateResponse =
                LibraryLoader.GetSymbolDelegate<GenerateResponseDelegate>(library,
                    "generate_response");
            freeContext = LibraryLoader.GetSymbolDelegate<FreeContextDelegate>(library, "free_context");
            abortInitializeModel =
                LibraryLoader.GetSymbolDelegate<AbortInitializeModelDelegate>(library, "abort_initialize_model");
            abortGeneration = LibraryLoader.GetSymbolDelegate<AbortGenerationDelegate>(library, "abort_generation");
            computeEmbeddings =
                LibraryLoader.GetSymbolDelegate<ComputeEmbeddingsDelegate>(library, "compute_embeddings");
            getEmbeddingsSize =
                LibraryLoader.GetSymbolDelegate<GetEmbeddingsSizeDelegate>(library, "get_embeddings_size");

            // --- TTS ---
            loadTts =
                LibraryLoader.GetSymbolDelegate<LoadTtsDelegate>(library, "load_tts");
            generateTts =
                LibraryLoader.GetSymbolDelegate<GenerateTtsDelegate>(library, "generate_tts");
            unloadTts =
                LibraryLoader.GetSymbolDelegate<UnloadTtsDelegate>(library, "unload_tts");

            initialized = true;
        }

        public void FreeLibrary()
        {
            initialized = false;
            setLogCallback = null;
            initializeModel = null;
            shutdownModel = null;
            loadContext = null;
            unloadContext = null;
            cacheContext = null;
            getContext = null;
            initContext = null;
            addTurn = null;
            appendToContext = null;
            copyContext = null;
            generateResponse = null;
            freeContext = null;
            abortInitializeModel = null;
            abortGeneration = null;
            computeEmbeddings = null;
            getEmbeddingsSize = null;
            loadTts = null;
            generateTts = null;
            unloadTts = null;

            if (library != IntPtr.Zero)
            {
                LibraryLoader.FreeLibrary(library);
                library = IntPtr.Zero;
            }

            foreach (var handle in dependentLibraries)
            {
                if (handle != IntPtr.Zero)
                {
                    LibraryLoader.FreeLibrary(handle);
                }
            }

            dependentLibraries.Clear();

            PackageLogger.Debug("Native library released");
        }

        public void Dispose()
        {
            if (_registeredModelIds.Count == 0)
            {
                FreeLibrary();
                return;
            }

            var remainingModels = _registeredModelIds.Count;
            var modelIds = new string[_registeredModelIds.Count];
            _registeredModelIds.CopyTo(modelIds);

            foreach (var modelId in modelIds)
            {
                ShutdownModel(modelId, success =>
                {
                    PackageLogger.DebugFormat("Model {0} shutdown. Success: {1}", modelId, success);

                    remainingModels--;
                    if (remainingModels == 0)
                    {
                        PackageLogger.Debug("All models completed shutdown, freeing library");
                        FreeLibrary();
                    }
                });
            }

            _registeredModelIds.Clear();
        }

        private (List<string> dependencyPaths, string mainLibraryPath) GetPlatformLibraryPaths()
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
            }
            string fullMainLibraryPath = Path.Combine(basePath, libraryData.mainLibraryPath);
            return (fullDependencyPaths, fullMainLibraryPath);
        }

        private static readonly NativeCallbacks.LogCallbackWithLevelDelegate DefaultLogCallback = (level, message) =>
        {
            switch (level)
            {
                case 0: // Info
                    PackageLogger.Debug($"{message}");
                    break;
                case 1: // Warning
                    PackageLogger.Warning($"{message}");
                    break;
                case 2: // Error
                    PackageLogger.Warning($"{message}");
                    break;
                default:
                    PackageLogger.Debug($"[Aviad Level {level}] {message}");
                    break;
            }
        };

        public void SetLoggingEnabled(Action<bool> onDone = null)
        {
            if (!SafeEnsureLoaded())
            {
                onDone?.Invoke(false);
                return;
            }
            bool result = false;
            try
            {
                result = setLogCallback(DefaultLogCallback);
            }
            catch (Exception ex)
            {
                PackageLogger.Error($"NativeLibrary.SetLoggingEnabled failed: {ex.Message}");
            }
            onDone?.Invoke(result);
        }

        public void InitializeModel(
            string modelId,
            LlamaInitializationParams modelParams,
            Action<bool> onDone = null)
        {
            if (modelInitialized)
            {
                onDone?.Invoke(true);
                return;
            }
            if (!SafeEnsureLoaded())
            {
                onDone?.Invoke(false);
                return;
            }
            if (modelParams == null)
            {
                onDone?.Invoke(false);
                return;
            }
            bool result = false;
            try
            {
                PackageLogger.Debug($"NativeLibrary.InitializeModel modelId: {modelId}");

                var nativeParams = modelParams.ToStruct();
                PackageLogger.Debug(modelParams.modelPath);
                result = initializeModel(modelId, ref nativeParams);
                _registeredModelIds.Add(modelId);
                modelInitialized = result;
            }
            catch (Exception ex)
            {
                PackageLogger.Error($"GenerationLoadedLibrary.InitializeModel failed: {ex.Message}");
            }
            onDone?.Invoke(result);
        }

        public void ShutdownModel(
            string modelId,
            Action<bool> onDone = null)
        {
            if (!SafeEnsureLoaded())
            {
                onDone?.Invoke(false);
                return;
            }
            if (!SafeEnsureLoaded())
            {
                onDone?.Invoke(true);
                return;
            }
            bool result = false;
            try
            {
                result = shutdownModel(modelId);
                modelInitialized = false;

            }
            catch (Exception ex)
            {
                PackageLogger.Error($"GenerationLoadedLibrary.ShutdownModel failed: {ex.Message}");
            }
            onDone?.Invoke(result);
        }

        public void LoadContext(
            string modelId,
            string contextKey,
            string templateString,
            Action<bool> onDone = null)
        {
            if (!SafeEnsureLoaded())
            {
                onDone?.Invoke(false);
                return;
            }
            bool result = false;
            try
            {
                var templateParams = (new LlamaChatTemplateParams(templateString)).ToNative();
                result = loadContext(modelId, contextKey, ref templateParams);
            }
            catch (Exception ex)
            {
                PackageLogger.Error($"GenerationLoadedLibrary.LoadContext failed: {ex.Message}");
            }
            onDone?.Invoke(result);
        }

        public void UnloadActiveContext(
            string modelId,
            Action<bool> onDone = null)
        {
            if (!SafeEnsureLoaded())
            {
                onDone?.Invoke(false);
                return;
            }
            bool result = false;
            try
            {
                result = unloadContext(modelId);
            }
            catch (Exception ex)
            {
                PackageLogger.Error($"GenerationLoadedLibrary.UnloadActiveContext failed: {ex.Message}");
            }
            onDone?.Invoke(result);
        }

        public void CacheContext(
            string modelId,
            Action<bool> onDone = null)
        {
            if (!SafeEnsureLoaded())
            {
                onDone?.Invoke(false);
                return;
            }
            bool result = false;
            try
            {
                result = cacheContext(modelId);
            }
            catch (Exception ex)
            {
                PackageLogger.Error($"GenerationLoadedLibrary.CacheContext failed: {ex.Message}");
            }
            onDone?.Invoke(result);
        }

        public void GetContext(
            string contextKey,
            int maxTurnCount,
            int maxStringLength,
            Action<LlamaMessageSequence> onResult)
        {
            if (!SafeEnsureLoaded())
            {
                onResult?.Invoke(null);
                return;
            }
            try
            {
                using (var preallocator = new LlamaMessageSequencePreallocator(maxTurnCount, maxStringLength))
                {
                    var outSeq = preallocator.GetLlamaMessageSequence();
                    getContext(contextKey, ref outSeq, maxTurnCount, maxStringLength);
                    onResult.Invoke(new LlamaMessageSequence(outSeq));
                }
            }
            catch (Exception ex)
            {
                PackageLogger.Error($"Aviad.NativeLibrary.GetContext failed: {ex.Message}");
                onResult.Invoke(null);
            }
        }

        public void InitContext(
            string contextKey,
            LlamaMessageSequence messages,
            Action<bool> onDone = null)
        {
            if (!SafeEnsureLoaded())
            {
                onDone?.Invoke(false);
                return;
            }
            bool result = false;
            try
            {
                using (var wrapper = messages.ToNative())
                {
                    var nativeSequence = wrapper.Native;
                    result = initContext(contextKey, ref nativeSequence);
                }
            }
            catch (Exception ex)
            {
                PackageLogger.Error($"Aviad.NativeLibrary.InitContext failed: {ex.Message}");
            }
            onDone?.Invoke(result);
        }

        public void AddTurnToContext(
            string contextKey,
            string role,
            string content,
            Action<bool> onDone = null)
        {
            if (!SafeEnsureLoaded())
            {
                onDone?.Invoke(false);
                return;
            }
            bool result = false;
            try
            {
                result = addTurn(contextKey, role, content);
            }
            catch (Exception ex)
            {
                PackageLogger.Error($"Aviad.NativeLibrary.AddTurnToContext failed: {ex.Message}");
            }
            onDone?.Invoke(result);
        }

        public void AppendToContext(
            string contextKey,
            string content,
            Action<bool> onDone = null)
        {
            if (!SafeEnsureLoaded())
            {
                onDone?.Invoke(false);
                return;
            }
            bool result = false;
            try
            {
                result = appendToContext(contextKey, content);
            }
            catch (Exception ex)
            {
                PackageLogger.Error($"Aviad.NativeLibrary.AppendToContext failed: {ex.Message}");
            }
            onDone?.Invoke(result);
        }

        public void CopyContext(
            string sourceContextKey,
            string targetContextKey,
            Action<bool> onDone = null)
        {
            if (!SafeEnsureLoaded())
            {
                onDone?.Invoke(false);
                return;
            }
            bool result = false;
            try
            {
                result = copyContext(sourceContextKey, targetContextKey);
            }
            catch (Exception ex)
            {
                PackageLogger.Error($"Aviad.NativeLibrary.CopyContext failed: {ex.Message}");
            }
            onDone?.Invoke(result);
        }

        public void FreeContext(
            string contextKey,
            Action<bool> onDone = null)
        {
            if (!SafeEnsureLoaded())
            {
                onDone?.Invoke(false);
                return;
            }
            bool result = false;
            try
            {
                result = freeContext(contextKey);
            }
            catch (Exception ex)
            {
                PackageLogger.Error($"NativeLibrary.FreeContext failed: {ex.Message}");
            }
            onDone?.Invoke(result);
        }

        public void GenerateResponse(
            string modelId,
            string contextKey,
            string outContextKey,
            LlamaGenerationConfig config,
            Action<string> onToken,
            Action<bool> onDone)
        {
            PackageLogger.Verbose($"NativeLibrary.GenerateResponse called.");
            if (!SafeEnsureLoaded())
            {
                onDone?.Invoke(false);
                return;
            }
            // generateResponse is expected to trigger onDone in native code.
            // onDone might be triggered twice.
            try
            {
                using (var wrapper = config.ToNative())
                {
                    var nativeConfig = wrapper.Native;
                    NativeCallbacks.TokenStreamCallback onTokenCallback =
                        NativeCallbacks.TokenStreamCallbackFromAction(onToken);
                    NativeCallbacks.StreamDoneCallback onDoneCallback =
                        NativeCallbacks.StreamDoneCallbackFromAction(onDone);
                    generateResponse(
                        modelId, contextKey, outContextKey, ref nativeConfig, onTokenCallback, onDoneCallback);
                }
            }
            catch (Exception ex)
            {
                PackageLogger.Error($"NativeLibrary.GenerateResponse failed: {ex.Message}");
                // Trigger onDone again in case the native code exist unexpectedly
                onDone?.Invoke(false);
            }
        }

        public void AbortInitializeModel(
            string modelId,
            Action<bool> onDone = null)
        {
            if (!SafeEnsureLoaded())
            {
                onDone?.Invoke(false);
                return;
            }
            bool result = false;
            try
            {
                result = abortInitializeModel(modelId);
            }
            catch (Exception ex)
            {
                PackageLogger.Error($"NativeLibrary.AbortInitializeModel failed: {ex.Message}");
            }
            onDone?.Invoke(result);
        }

        public void AbortGeneration(
            string modelId,
            Action<bool> onDone = null)
        {
            if (!SafeEnsureLoaded())
            {
                onDone?.Invoke(false);
                return;
            }
            bool result = false;
            try
            {
                result = abortGeneration(modelId);
            }
            catch (Exception ex)
            {
                PackageLogger.Error($"Aviad.NativeLibrary.AbortGeneration failed: {ex.Message}");
            }
            onDone?.Invoke(result);
        }

        public void ComputeEmbeddings(
            string modelId,
            string context,
            LlamaEmbeddingParams embeddingParams,
            Action<float[]> onResult)
        {
            if (!SafeEnsureLoaded())
            {
                PackageLogger.Warning("ComputeEmbeddings: Library not loaded.");
                onResult.Invoke(null);
                return;
            }
            // Embedding size should be checked in Editor.
            // Embedding size is validated in C++
            int floatCount = (int)embeddingParams.maxEmbeddingsSize;
            int byteCount = floatCount * sizeof(float);
            float[] resultArray = new float[floatCount];
            IntPtr outputPtr = IntPtr.Zero;
            try
            {
                outputPtr = Marshal.AllocHGlobal(byteCount);
                // TODO: Why doesn't this work without zeroing memory?
                byte[] zeroBytes = new byte[byteCount];
                Marshal.Copy(zeroBytes, 0, outputPtr, byteCount);
                NativeLlamaEmbeddingParams nativeParams = embeddingParams.ToStruct();
                bool success = computeEmbeddings(modelId, context, ref nativeParams, outputPtr);
                if (!success)
                {
                    PackageLogger.Warning("ComputeEmbeddings: Native call returned false.");
                    onResult.Invoke(null);
                    return;
                }
                // Marshal as byte[] because Marshal.Copy does not support float[]
                byte[] byteBuffer = new byte[byteCount];
                Marshal.Copy(outputPtr, byteBuffer, 0, byteCount);
                // Convert to float[]
                Buffer.BlockCopy(byteBuffer, 0, resultArray, 0, byteCount);

                // Check for NaNs
                for (int i = 0; i < resultArray.Length; i++)
                {
                    if (float.IsNaN(resultArray[i]))
                    {
                        PackageLogger.Warning("ComputeEmbeddings: NaN detected in result array.");
                        onResult?.Invoke(null);
                        return;
                    }
                }
                onResult?.Invoke(resultArray);
            }
            catch (Exception ex)
            {
                PackageLogger.Error($"ComputeEmbeddings failed: {ex.Message}");
                onResult.Invoke(null);
            }
            finally
            {
                if (outputPtr != IntPtr.Zero)
                    Marshal.FreeHGlobal(outputPtr);
            }
        }

        // --- TTS ---
        public void LoadTTS(TTSParams ttsParams, Action<bool> onDone = null)
        {
            if (!SafeEnsureLoaded())
            {
                onDone?.Invoke(false);
                return;
            }

            bool result = false;
            try
            {
                if (ttsParams == null || string.IsNullOrEmpty(ttsParams.directory))
                {
                    PackageLogger.Error("LoadTTS: Invalid parameters.");
                    onDone?.Invoke(false);
                    return;
                }

                var refsArr = ttsParams.refExamples ?? Array.Empty<int>();
                result = loadTts(ttsParams.directory, refsArr, refsArr.Length);
            }
            catch (Exception ex)
            {
                PackageLogger.Error($"NativeLibrary.LoadTTS failed: {ex.Message}");
            }
            onDone?.Invoke(result);
        }

        public void GenerateTTS(string text, int maxLength, Action<float[]> onAudio, Action<bool> onDone)
        {
            if (!SafeEnsureLoaded())
            {
                onDone?.Invoke(false);
                return;
            }

            bool result = false;
            IntPtr outputPtr = IntPtr.Zero;
            int length = Math.Max(0, maxLength);
            int byteCount = length * sizeof(float);
            float[] samples = new float[length];
            try
            {
                if (string.IsNullOrEmpty(text))
                {
                    onDone?.Invoke(false);
                    return;
                }

                outputPtr = Marshal.AllocHGlobal(byteCount);
                byte[] zeroBytes = new byte[byteCount];
                Marshal.Copy(zeroBytes, 0, outputPtr, byteCount);
                result = generateTts(text, outputPtr, length);
                if (result)
                {
                    byte[] byteBuffer = new byte[byteCount];
                    Marshal.Copy(outputPtr, byteBuffer, 0, byteCount);
                    Buffer.BlockCopy(byteBuffer, 0, samples, 0, byteCount);
                    onAudio?.Invoke(samples);
                }
            }
            catch (Exception ex)
            {
                PackageLogger.Error($"NativeLibrary.GenerateTTS failed: {ex.Message}");
                result = false;
            }
            finally
            {
                onDone?.Invoke(result);
            }
        }

        public void UnloadTTS(Action<bool> onDone = null)
        {
            if (!SafeEnsureLoaded())
            {
                onDone?.Invoke(false);
                return;
            }

            bool result = false;
            try
            {
                result = unloadTts();
            }
            catch (Exception ex)
            {
                PackageLogger.Error($"NativeLibrary.UnloadTTS failed: {ex.Message}");
            }
            onDone?.Invoke(result);
        }

        public uint GetEmbeddingsSize(string modelId)
        {
            try
            {
                return getEmbeddingsSize(modelId);
            }
            catch (Exception ex)
            {
                PackageLogger.Error($"GetEmbeddingsSize failed: {ex.Message}");
                return 0;
            }
        }
    }
}