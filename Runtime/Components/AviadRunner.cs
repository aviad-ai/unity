using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Aviad
{
    public class AviadRunner : MonoBehaviour
    {
        [Header("Model Configuration")]
        [SerializeField] private string modelUrl = "";
        [SerializeField] private bool saveToStreamingAssets = true;
        [SerializeField] private bool continueConversationAfterGeneration = true;
        [SerializeField] private int maxContextLength = 4096;
        [SerializeField] private int gpuLayers = 0;
        [SerializeField] private int threads = 4;
        [SerializeField] private int maxBatchLength = 512;

        [Header("Generation Configuration")]
        [SerializeField] private string chatTemplate = "";
        [SerializeField] private string grammarString = "";
        [SerializeField] private float temperature = 0.7f;
        [SerializeField] private float topP = 0.9f;
        [SerializeField] private int maxTokens = 256;
        [SerializeField] private int chunkSize = 1;

        private IAviadGeneration _aviadInstance;
        private string _inputContextKey = "input_context";
        private string _outputContextKey = "output_context";
        private bool _isAvailable = false;
        private bool _isGenerating = false;

        private string _localModelPath = "";
        private string _lastModelUrl = "";
        private bool _lastSaveToStreamingAssets = false;

        public bool EnableNativeLogging => AviadGlobalSettings.IsNativeLoggingEnabled;

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

        public string GetModelUrl() => modelUrl;
        public bool IsAvailable => _isAvailable;
        public bool IsGenerating => _isGenerating;

        private void OnValidate()
        {
            if (_lastModelUrl != modelUrl || _lastSaveToStreamingAssets != saveToStreamingAssets)
            {
                _lastModelUrl = modelUrl;
                _lastSaveToStreamingAssets = saveToStreamingAssets;

                string hash = GetHash(modelUrl);
                string path = Path.Combine(Application.streamingAssetsPath, "Aviad/Models", hash);

                if (!File.Exists(path) && _lastSaveToStreamingAssets)
                {
                    if (EnableNativeLogging)
                        Debug.Log("[AviadRunner] Model not found. You can download it from the Inspector.");
                }
            }
        }


        private void Start()
        {
            Action<bool> onInitializeModel = success =>
            {
                Debug.Log($"Model Initialized: {success}");
                if (success)
                {
                    _isAvailable = true;
                    InitializeContext();
                }
            };
            Action<bool> onDownload = success =>
            {
                Debug.Log($"Model Downloaded: {success}");
                if(success) {
                    InitializeModel(onInitializeModel);
                }
            };
            Action<bool> onInitialize = success =>
            {
                Debug.Log($"Native plugin initialized: {AviadInteropManager.Instance != null}");
                if (AviadInteropManager.Instance != null)
                {
                    _aviadInstance = AviadInteropManager.Instance;
                    DownloadModel(onDownload);
                    if (EnableNativeLogging)
                    {
                        bool loggingSuccess = _aviadInstance.SetLoggingEnabled();
                        Debug.Log("[AviadRunner] Logging setup success: " + loggingSuccess);
                    }
                }
            };
            AviadInteropManager.Initialize(onInitialize);
        }

        internal string GetExpectedModelPath()
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

        internal void DownloadModel(Action<bool> onComplete)
        {
            _localModelPath = GetExpectedModelPath();
            if (File.Exists(_localModelPath)) {
                onComplete?.Invoke(true);
                return;
            }
            else if (saveToStreamingAssets)
            {
                Debug.LogError("[AviadRunner] Model is not present (Please trigger download in-editor).");
                onComplete?.Invoke(false);
                return;
            }
            Debug.Log("[AviadRunner] Downloading model...");
#if UNITY_WEBGL && !UNITY_EDITOR
            if (AviadInteropManager.WebGLInstance == null) {
                Debug.LogError("WebGL interface not available.");
                onComplete?.Invoke(false);
                return;
            }
            AviadInteropManager.WebGLInstance.DownloadFile(modelUrl, _localModelPath, onComplete);
#else
            Task.Run(async () =>
            {
                bool result = await DownloadModelToFile(modelUrl, _localModelPath);
                onComplete?.Invoke(result);
            });
#endif
        }

        internal async Task<bool> DownloadModelToFile(string url, string filePath)
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
                    Debug.LogError($"Download error: {request.error}");
                    return false;
                }
                return true;
            }
        }

        private void InitializeModel(Action<bool> onComplete)
        {
            if (_aviadInstance == null) return;
            _aviadInstance.InitializeModel(
                new LlamaModelParams(
                    _localModelPath,
                    maxContextLength,
                    gpuLayers,
                    GetSafeThreadCount(),
                    maxBatchLength
                ),
                onComplete);
        }

        private int GetSafeThreadCount()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
    return 1;
#else
            return threads;
#endif
        }

        private bool InitializeContext()
        {
            if (_aviadInstance == null) return false;
            var emptySequence = new LlamaMessageSequence();
            bool initSuccess = _aviadInstance.InitContext(_inputContextKey, emptySequence);
            return initSuccess;
        }

        public bool Reset()
        {
            if (_aviadInstance == null) return false;
            if (!_isAvailable) return false;
            bool success = _aviadInstance.UnloadActiveContext();
            if (!success) return false;
            return InitializeContext();
        }

        public bool AddTurnToContext(string role, string content)
        {
            if (_aviadInstance == null) return false;
            if (!_isAvailable || _isGenerating) return false;
            return _aviadInstance.AddTurnToContext(_inputContextKey, role, content);
        }

        public bool Generate(Action<string> onUpdate, Action<bool> onDone)
        {
            if (_aviadInstance == null) return false;
            if (!_isAvailable || _isGenerating) return false;

            var config = new LlamaGenerationConfig(
                chatTemplate,
                grammarString,
                temperature,
                topP,
                maxTokens
            );

            void WrappedOnDone(bool success)
            {
                onDone?.Invoke(success);
                OnGenerationSuccess(success);
            }

            _isGenerating = true;
            _aviadInstance.GenerateResponseStreaming(
                _inputContextKey,
                _outputContextKey,
                config,
                onUpdate,
                WrappedOnDone,
                chunkSize
            );
            return true;
        }

        private void OnGenerationSuccess(bool success)
        {
            if (_aviadInstance == null) return;
            _isGenerating = false;
            if (success && continueConversationAfterGeneration)
            {
                _aviadInstance.CopyContext(_outputContextKey, _inputContextKey);
            }
        }

        public bool CopyContextToInput()
        {
            if (_aviadInstance == null) return false;
            if (!_isAvailable || _isGenerating) return false;
            return _aviadInstance.CopyContext(_outputContextKey, _inputContextKey);
        }

        public void DebugContext()
        {
            Action<LlamaMessageSequence> onInputMessages = messages =>
            {
                Debug.LogFormat("[AviadRunner] Input Context: {0}", messages.messages.Count);
            };
            Action<LlamaMessageSequence> onOutputMessages = messages =>
            {
                Debug.LogFormat("[AviadRunner] Output Context: {0}", messages.messages.Count);
            };
            _aviadInstance.GetContext(_inputContextKey, 16, 128, onInputMessages);
            _aviadInstance.GetContext(_outputContextKey, 16, 128, onOutputMessages);
        }

        private void OnDisable()
        {
            if (_aviadInstance == null) return;
            _aviadInstance.Cleanup();
        }
    }
}