using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Aviad
{
    [FilePath("Assets/Editor/Aviad/ModelEditorHelperState.asset", FilePathAttribute.Location.ProjectFolder)]
    public class ModelEditorHelperState : ScriptableSingleton<ModelEditorHelperState>
    {
        [System.Serializable]
        public class ModelBinding
        {
            public string targetGUID;
            public ModelConfiguration model;
        }

        [SerializeField]
        List<ModelBinding> m_Bindings;

        void OnEnable()
        {
            if (m_Bindings == null)
                m_Bindings = new List<ModelBinding>();
        }

        /// <summary>
        /// Get the AviadModel associated with the given target GUID
        /// Automatically cleans up invalid entries on access
        /// </summary>
        public ModelConfiguration GetModel(string targetGuid)
        {
            if (string.IsNullOrEmpty(targetGuid))
                return null;

            var entry = m_Bindings.Find(e => e.targetGUID == targetGuid);

            if (entry != null)
            {
                // Clean up invalid entry if necessary
                if (entry.model == null)
                {
                    m_Bindings.Remove(entry);
                    Save(true);
                    return null;
                }

                return entry.model;
            }

            return null;
        }


        /// <summary>
        /// Set the AviadModel for the given target GUID
        /// Automatically cleans up when setting to null
        /// </summary>
        public void SetModel(string targetGuid, ModelConfiguration model)
        {
            if (string.IsNullOrEmpty(targetGuid))
                return;

            var entry = m_Bindings.Find(e => e.targetGUID == targetGuid);

            if (entry != null)
            {
                if (model == null)
                {
                    // Remove the binding
                    m_Bindings.Remove(entry);
                }
                else
                {
                    // Update existing binding
                    entry.model = model;
                }
            }
            else if (model != null)
            {
                // Add new binding
                m_Bindings.Add(new ModelBinding { targetGUID = targetGuid, model = model });
            }

            Save(true);
        }

        /// <summary>
        /// Remove all bindings for invalid GUIDs or null models
        /// </summary>
        public int CleanupInvalidBindings()
        {
            int beforeCount = m_Bindings.Count;

            m_Bindings.RemoveAll(entry =>
                string.IsNullOrEmpty(entry.targetGUID) ||
                string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(entry.targetGUID)) ||
                entry.model == null
            );

            int removedCount = beforeCount - m_Bindings.Count;
            if (removedCount > 0)
            {
                Save(true);
            }

            return removedCount;
        }

        /// <summary>
        /// Get the number of currently stored bindings
        /// </summary>
        public int GetBindingCount()
        {
            return m_Bindings.Count;
        }

        /// <summary>
        /// Clear all stored bindings
        /// </summary>
        public void ClearAllBindings()
        {
            m_Bindings.Clear();
            Save(true);
        }
    }
}
