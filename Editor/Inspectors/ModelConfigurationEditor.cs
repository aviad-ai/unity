using System.Collections;
using UnityEditor;
using UnityEngine;

namespace Aviad
{
    [CustomEditor(typeof(ModelConfiguration))]
    public class ModelConfigurationEditor : UnityEditor.Editor
    {
        private ModelConfiguration model;
        private bool isDownloading = false;

        private void OnEnable()
        {
            model = (ModelConfiguration)target;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(5);

            var modelUrlProp = serializedObject.FindProperty("modelUrl");
            if (modelUrlProp != null)
                EditorGUILayout.PropertyField(modelUrlProp);

            var modelParamsProp = serializedObject.FindProperty("modelParams");
            if (modelParamsProp != null)
                EditorGUILayout.PropertyField(modelParamsProp, GUIContent.none, true);

            var generationConfigProp = serializedObject.FindProperty("generationConfig");
            if (generationConfigProp != null)
                EditorGUILayout.PropertyField(generationConfigProp, GUIContent.none, true);

            var embeddingParamsProp = serializedObject.FindProperty("embeddingParams");
            if (embeddingParamsProp != null)
                EditorGUILayout.PropertyField(embeddingParamsProp, true);

            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(5);
            GUI.enabled = !isDownloading;
            if (GUILayout.Button("Download Model"))
            {
                EditorCoroutineRunner.Start(DownloadModelEditorCoroutine());
            }
            GUI.enabled = true;
        }

        private IEnumerator DownloadModelEditorCoroutine()
        {
            isDownloading = true;
            string url = model.modelUrl;
            string hash = ModelRuntime.GetHash(url);
            string modelPath = System.IO.Path.Combine(Application.streamingAssetsPath, "Aviad/Models", hash);
            string tempPath = modelPath + ".tmp";
            DeleteFileIfExists(tempPath);

            using var request = UnityEngine.Networking.UnityWebRequest.Get(url);
            request.downloadHandler = new UnityEngine.Networking.DownloadHandlerFile(tempPath);
            var op = request.SendWebRequest();
            while (!op.isDone)
            {
                if (request == null)
                {
                    yield break;
                }
                float progress = request.downloadProgress;
                if (EditorUtility.DisplayCancelableProgressBar("Downloading Model", $"{(int)(progress * 100)}% Complete", progress))
                {
                    if (request != null)
                    {
                        request.Abort();
                        request.Dispose();
                    }
                    EditorUtility.ClearProgressBar();
                    DeleteFileIfExists(tempPath);
                    isDownloading = false;
                    yield break;
                }
                yield return null;
            }

            EditorUtility.ClearProgressBar();
            bool success = request.result == UnityEngine.Networking.UnityWebRequest.Result.Success &&
                           System.IO.File.Exists(tempPath) &&
                           new System.IO.FileInfo(tempPath).Length > 0;
            if (success)
            {
                DeleteFileIfExists(modelPath);
                System.IO.File.Move(tempPath, modelPath);
            }
            else
            {
                PackageLogger.Error($"Download failed: {request.error}");
            }
            DeleteFileIfExists(tempPath);
            isDownloading = false;
        }

        private void DeleteFileIfExists(string path)
        {
            try
            {
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }
            catch (System.Exception ex)
            {
                PackageLogger.Warning($"Failed to delete file {path}: {ex.Message}");
            }
        }
    }
}