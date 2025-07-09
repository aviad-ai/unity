using UnityEngine;

namespace Aviad
{
    [System.Serializable]
    public class AviadGlobalSettings : ScriptableObject
    {
        public const string k_SettingsPath = "Assets/Settings/AviadGlobalSettings.asset";
        public const string k_ResourcesPath = "AviadGlobalSettings";

        [SerializeField]
        private bool enableNativeLogging = true;

        public bool EnableNativeLogging
        {
            get => enableNativeLogging;
            set => enableNativeLogging = value;
        }

        private static AviadGlobalSettings s_Instance;

        public static AviadGlobalSettings GetSettings()
        {
            if (s_Instance == null)
            {
#if UNITY_EDITOR
                s_Instance = UnityEditor.AssetDatabase.LoadAssetAtPath<AviadGlobalSettings>(k_SettingsPath);
#else
                s_Instance = Resources.Load<AviadGlobalSettings>(k_ResourcesPath);
#endif

                if (s_Instance == null)
                {
                    Debug.LogWarning("AviadGlobalSettings not found. Using default settings.");
                    s_Instance = CreateDefaultSettings();
                }
            }

            return s_Instance;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Get or create settings (editor only)
        /// </summary>
        public static AviadGlobalSettings GetOrCreateSettings()
        {
            var settings = UnityEditor.AssetDatabase.LoadAssetAtPath<AviadGlobalSettings>(k_SettingsPath);

            if (settings == null)
            {
                settings = CreateInstance<AviadGlobalSettings>();

                // Ensure the Settings directory exists
                var directory = System.IO.Path.GetDirectoryName(k_SettingsPath);
                if (!System.IO.Directory.Exists(directory))
                {
                    System.IO.Directory.CreateDirectory(directory);
                }

                UnityEditor.AssetDatabase.CreateAsset(settings, k_SettingsPath);
                UnityEditor.AssetDatabase.SaveAssets();

                // Also create a copy in Resources for runtime access
                CreateResourcesCopy(settings);
            }

            s_Instance = settings;
            return settings;
        }

        private static void CreateResourcesCopy(AviadGlobalSettings settings)
        {
            var resourcesDir = "Assets/Resources";
            if (!System.IO.Directory.Exists(resourcesDir))
            {
                System.IO.Directory.CreateDirectory(resourcesDir);
            }

            var resourcesPath = $"{resourcesDir}/{k_ResourcesPath}.asset";
            var existingResource = UnityEditor.AssetDatabase.LoadAssetAtPath<AviadGlobalSettings>(resourcesPath);

            if (existingResource == null)
            {
                UnityEditor.AssetDatabase.CreateAsset(Instantiate(settings), resourcesPath);
                UnityEditor.AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// Save settings and update Resources copy
        /// </summary>
        public void SaveSettings()
        {
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();

            // Update Resources copy
            var resourcesPath = $"Assets/Resources/{k_ResourcesPath}.asset";
            var resourcesCopy = UnityEditor.AssetDatabase.LoadAssetAtPath<AviadGlobalSettings>(resourcesPath);

            if (resourcesCopy != null)
            {
                resourcesCopy.enableNativeLogging = this.enableNativeLogging;
                UnityEditor.EditorUtility.SetDirty(resourcesCopy);
                UnityEditor.AssetDatabase.SaveAssets();
            }
        }
#endif

        private static AviadGlobalSettings CreateDefaultSettings()
        {
            var settings = CreateInstance<AviadGlobalSettings>();
            settings.enableNativeLogging = false;
            return settings;
        }

        public static bool IsNativeLoggingEnabled => GetSettings().EnableNativeLogging;
    }
}