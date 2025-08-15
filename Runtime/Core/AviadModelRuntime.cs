using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Aviad
{
    public class AviadModelRuntime : IDisposable
    {
        protected bool _isDownloaded = false;
        protected bool _isAvailable = false;
        protected string _modelId = "";

        protected string _localModelPath = "";
        protected AviadModel modelAsset;
        protected bool enableDownload;

        public Action onStateUpdate;

        public bool IsPluginAvailable => AviadManager.Instance != null;
        public bool IsDownloaded => _isDownloaded;
        public bool IsAvailable => _isAvailable;
        public string ModelId => _modelId;

        public AviadModelRuntime(
            AviadModel modelAsset,
            bool enableDownload,
            string modelNamePrefix="")
        {
            this._modelId = modelNamePrefix;
            this.modelAsset = modelAsset;
            this.enableDownload = enableDownload;
        }

        public void Initialize()
        {
            _modelId = AviadManager.RegisterModelId(_modelId);
            AviadLogger.Debug("[AviadModelRuntime] Initialize");
            RetryManager.ExecuteWithRetry(
                "AviadModelRuntime.InitializePlugin",
                AviadManager.Initialize,
                OnPluginInitialized,
                OnStageFailure
            );
        }

        protected void OnPluginInitialized()
        {
            AviadLogger.Debug("[AviadModelRuntime] OnPluginInitialized");
            onStateUpdate?.Invoke();
            AviadLogger.Debug("[AviadModelRuntime] Download model");
            RetryManager.ExecuteWithRetry(
                "AviadModelRuntime.DownloadModel",
                DownloadModel,
                OnModelDownloadComplete,
                OnStageFailure
            );
        }

        protected void OnModelDownloadComplete()
        {
            AviadLogger.Debug("[AviadModelRuntime] OnModelDownloadComplete");
            _isDownloaded = true;
            onStateUpdate?.Invoke();
            AviadLogger.Debug("[AviadModelRuntime] Initialize Model");
            RetryManager.ExecuteWithRetry(
                "AviadModelRuntime.InitializeModel",
                InitializeModel,
                OnModelInitialized,
                OnStageFailure
            );
        }

        protected void OnModelInitialized()
        {
            AviadLogger.Debug("[AviadModelRuntime] OnModelInitialized");
            _isAvailable = true;
            onStateUpdate?.Invoke();
        }

        protected void OnStageFailure()
        {
            AviadLogger.Debug("[AviadModelRuntime] Failed to initialize aviad runtime completely.");
            AviadManager.Cleanup();
        }

        public static string GetHash(string input)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                    builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }

        protected int GetSafeThreadCount()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return 1;
#else
            return modelAsset.modelParams.threads.hasValue ? modelAsset.modelParams.threads.value : 4;
#endif
        }

        public string GetExpectedModelPath()
        {
            return AviadModelRuntime.GetExpectedModelPath(modelAsset.modelUrl);
        }

        public static string GetExpectedModelPath(string modelUrl)
        {
            string modelHash = GetHash(modelUrl);
#if UNITY_WEBGL && !UNITY_EDITOR
            return "/" + modelHash + ".bin";
#else
            string modelDirectory = Path.Combine(Application.streamingAssetsPath, "Aviad/Models");
            Directory.CreateDirectory(modelDirectory);
            return Path.Combine(modelDirectory, modelHash);
#endif
        }

        protected void DownloadModel(Action<bool> onComplete)
        {
            _localModelPath = GetExpectedModelPath();

            if (File.Exists(_localModelPath))
            {
                onComplete?.Invoke(true);
                return;
            }

            if (!enableDownload)
            {
                AviadLogger.Error("[AviadModelRuntime] Model not present but expected.");
                onComplete?.Invoke(false);
                return;
            }

#if UNITY_WEBGL && !UNITY_EDITOR
            if (AviadManager.WebGLInstance == null)
            {
                AviadLogger.Error("WebGL interface not available.");
                onComplete?.Invoke(false);
                return;
            }
            AviadManager.WebGLInstance.DownloadFile(modelAsset.modelUrl, _localModelPath, onComplete);
#else
            Task.Run(async () =>
            {
                bool result = await DownloadModelToFile(modelAsset.modelUrl, _localModelPath);
                onComplete?.Invoke(result);
            });
#endif
        }

        protected void InitializeModel(Action<bool> onComplete)
        {
            if (AviadManager.Instance == null) return;

            var runtimeParams = new LlamaInitializationParams(modelAsset.modelParams)
                .WithModelPath(_localModelPath)
                .WithThreads(GetSafeThreadCount());
            AviadManager.Instance.InitializeModel(_modelId, runtimeParams, onComplete);
        }

        protected internal async Task<bool> DownloadModelToFile(string url, string filePath)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.downloadHandler = new DownloadHandlerFile(filePath);
                var operation = request.SendWebRequest();
                while (!operation.isDone)
                    await Task.Yield();

#if UNITY_2020_1_OR_NEWER
                if (request.result != UnityWebRequest.Result.Success)
#else
                if (request.isNetworkError || request.isHttpError)
#endif
                {
                    AviadLogger.Error($"Download error: {request.error}");
                    return false;
                }
                return true;
            }
        }

        public void Dispose()
        {
            if (AviadManager.Instance != null) AviadManager.Instance.ShutdownModel(_modelId, OnShutdownModel);
        }

        public void OnShutdownModel(bool success)
        {
            _isAvailable = false;
            _isDownloaded = false;
            onStateUpdate?.Invoke();
        }

    }
}