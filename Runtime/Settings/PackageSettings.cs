using UnityEngine;

namespace Aviad
{
    public enum LogLevel
    {
        Verbose = 0,
        Debug = 1,
        Info = 2,
        Warning = 3,
        Error = 4,
        None = 5
    }

    [System.Serializable]
    public class PackageSettings : ScriptableObject
    {
        public const string k_SettingsPath = "Assets/Settings/PackageSettings.asset";
        public const string k_ResourcesPath = "PackageSettings";

        [SerializeField]
        private bool enableNativeLogging = true;

        [SerializeField]
        private LogLevel logLevel = LogLevel.Info;

        [SerializeField]
        private bool m_HasBeenInitialized = false;

        // --- New fields ---
        [SerializeField, HideInInspector]
        private bool shouldOpenAviadWindow = true; // hidden from UI, defaults true

        [SerializeField]
        private bool alwaysOpenAviadWindow = false; // user-facing

        public bool EnableNativeLogging
        {
            get => enableNativeLogging;
            set => enableNativeLogging = value;
        }

        public LogLevel LogLevel
        {
            get => logLevel;
            set => logLevel = value;
        }

        public bool HasBeenInitialized() => m_HasBeenInitialized;

        public bool ShouldOpenAviadWindow
        {
            get => shouldOpenAviadWindow;
            set => shouldOpenAviadWindow = value;
        }

        public bool AlwaysOpenAviadWindow
        {
            get => alwaysOpenAviadWindow;
            set => alwaysOpenAviadWindow = value;
        }

        private static PackageSettings s_Instance;

        public static PackageSettings GetSettings()
        {
            if (s_Instance == null)
            {
#if UNITY_EDITOR
                s_Instance = UnityEditor.AssetDatabase.LoadAssetAtPath<PackageSettings>(k_SettingsPath);
#else
                s_Instance = Resources.Load<PackagexSettings>(k_ResourcesPath);
#endif

                if (s_Instance == null)
                {
                    s_Instance = CreateDefaultSettings();
                }
            }

            return s_Instance;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Get or create settings (editor only)
        /// </summary>
        public static PackageSettings GetOrCreateSettings()
        {
            var settings = UnityEditor.AssetDatabase.LoadAssetAtPath<PackageSettings>(k_SettingsPath);

            if (settings == null)
            {
                settings = CreateInstance<PackageSettings>();

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

        private static void CreateResourcesCopy(PackageSettings settings)
        {
            var resourcesDir = "Assets/Resources";
            if (!System.IO.Directory.Exists(resourcesDir))
            {
                System.IO.Directory.CreateDirectory(resourcesDir);
            }

            var resourcesPath = $"{resourcesDir}/{k_ResourcesPath}.asset";
            var existingResource = UnityEditor.AssetDatabase.LoadAssetAtPath<PackageSettings>(resourcesPath);

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
            m_HasBeenInitialized = true;
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();

            // Update Resources copy
            var resourcesPath = $"Assets/Resources/{k_ResourcesPath}.asset";
            var resourcesCopy = UnityEditor.AssetDatabase.LoadAssetAtPath<PackageSettings>(resourcesPath);

            if (resourcesCopy != null)
            {
                resourcesCopy.enableNativeLogging = this.enableNativeLogging;
                resourcesCopy.logLevel = this.logLevel;
                resourcesCopy.shouldOpenAviadWindow = this.shouldOpenAviadWindow;
                resourcesCopy.alwaysOpenAviadWindow = this.alwaysOpenAviadWindow;

                UnityEditor.EditorUtility.SetDirty(resourcesCopy);
                UnityEditor.AssetDatabase.SaveAssets();
            }
        }
#endif

        private static PackageSettings CreateDefaultSettings()
        {
            var settings = CreateInstance<PackageSettings>();
            settings.enableNativeLogging = false;
            settings.logLevel = LogLevel.Info;
            settings.shouldOpenAviadWindow = true;  // default true
            settings.alwaysOpenAviadWindow = false; // default false
            return settings;
        }

        public static bool IsNativeLoggingEnabled => GetSettings().EnableNativeLogging;
        public static LogLevel CurrentLogLevel => GetSettings().LogLevel;
    }
}