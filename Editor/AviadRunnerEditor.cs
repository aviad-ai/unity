using System.Collections;
using UnityEditor;
using UnityEngine;

namespace Aviad
{
    [CustomEditor(typeof(AviadRunner))]
    public class AviadRunnerEditor : Editor
    {
        private AviadRunner runner;
        private bool isDownloading = false;
        private SerializedObject globalSettingsSerializedObject;

        private void OnEnable()
        {
            runner = (AviadRunner)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(10);
            GUI.enabled = !isDownloading;
            if (GUILayout.Button("Download Model"))
            {
                EditorCoroutineRunner.Start(DownloadModelEditorCoroutine());
            }
            GUI.enabled = true;
            DrawGlobalSettingsSection();
        }

        private void DrawGlobalSettingsSection()
        {
            EditorGUILayout.LabelField("Global Settings", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                var settings = AviadGlobalSettings.GetSettings();
                string loggingStatus = settings.EnableNativeLogging ? "Enabled" : "Disabled";
                EditorGUILayout.LabelField($"Logging: {loggingStatus}", EditorStyles.miniLabel);

                if (GUILayout.Button("Open Project Settings", GUILayout.Width(150)))
                {
                    SettingsService.OpenProjectSettings("Project/Aviad/Global Settings");
                }
            }
        }

        private IEnumerator DownloadModelEditorCoroutine()
        {
            isDownloading = true;

            string url = runner.GetModelUrl();
            string hash = AviadRunner.GetHash(url);
            string modelPath = System.IO.Path.Combine(Application.streamingAssetsPath, "Aviad/Models", hash);

            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(modelPath));

            using var request = UnityEngine.Networking.UnityWebRequest.Get(url);
            request.downloadHandler = new UnityEngine.Networking.DownloadHandlerFile(modelPath);

            var op = request.SendWebRequest();

            while (!op.isDone)
            {
                float progress = request.downloadProgress;
                EditorUtility.DisplayProgressBar("Downloading Model", $"{(int)(progress * 100)}% Complete", progress);
                yield return null;
            }

#if UNITY_2020_1_OR_NEWER
            if (request.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
#else
            if (request.isHttpError || request.isNetworkError)
#endif
            {
                Debug.LogError($"[AviadRunnerEditor] Download failed: {request.error}");
            }
            else
            {
                Debug.Log("[AviadRunnerEditor] Model downloaded successfully.");
            }

            EditorUtility.ClearProgressBar();
            isDownloading = false;

            // Repaint to update the UI
            Repaint();
        }

        private void OnDisable()
        {
            globalSettingsSerializedObject?.Dispose();
        }
    }
}