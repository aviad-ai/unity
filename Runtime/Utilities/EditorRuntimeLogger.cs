using System;
using System.Collections.Generic;
using UnityEngine;

namespace Aviad
{
public class EditorRuntimeLogger : MonoBehaviour
    {
#if UNITY_EDITOR
        private static EditorRuntimeLogger _instance;

        public static EditorRuntimeLogger Instance
        {
            get
            {
                if (!Application.isPlaying) return null;

                if (_instance == null)
                {
                    _instance = FindObjectOfType<EditorRuntimeLogger>();
                    if (_instance == null)
                    {
                        var go = new GameObject("EditorRuntimeLogger");
                        _instance = go.AddComponent<EditorRuntimeLogger>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        private readonly List<PrototypingItem> _logs = new List<PrototypingItem>();

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Log(PrototypingItem item)
        {
            if (!Application.isPlaying) return;
            if (item != null && string.IsNullOrEmpty(item.generationId))
            {
                item.generationId = Guid.NewGuid().ToString("D");
            }
            _logs.Add(item);
        }

        public IReadOnlyList<object> GetLogs() => _logs.AsReadOnly();

        public void ClearLogs() => _logs.Clear();

#else
        // noop in builds
        public static EditorRuntimeLogger Instance => null;
        public void Log(PrototypingItem item) { }
        public IReadOnlyList<object> GetLogs() => new List<object>();
        public void ClearLogs() { }
#endif
    }
}
