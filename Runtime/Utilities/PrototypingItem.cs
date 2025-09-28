using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Aviad
{
    [Serializable]
    public class StringStringMap : ISerializationCallbackReceiver, IEnumerable<KeyValuePair<string, string>>
    {
        [SerializeField] private List<string> keys = new List<string>();
        [SerializeField] private List<string> values = new List<string>();

        private Dictionary<string, string> dict = new Dictionary<string, string>();

        public void OnBeforeSerialize()
        {
            keys.Clear();
            values.Clear();
            if (dict == null) return;
            foreach (var kv in dict)
            {
                keys.Add(kv.Key);
                values.Add(kv.Value);
            }
        }

        public void OnAfterDeserialize()
        {
            dict = new Dictionary<string, string>();
            int count = Math.Min(keys.Count, values.Count);
            for (int i = 0; i < count; i++)
            {
                dict[keys[i]] = values[i];
            }
        }

        public string Get(string key, string defaultValue = "")
        {
            if (dict != null && key != null && dict.TryGetValue(key, out var value))
            {
                return value;
            }
            return defaultValue;
        }

        public void Set(string key, string value)
        {
            if (key == null) return;
            if (dict == null) dict = new Dictionary<string, string>();
            if (value == null)
            {
                dict.Remove(key);
            }
            else
            {
                dict[key] = value;
            }
        }

        // 👇 For collection initializer support
        public void Add(string key, string value)
        {
            Set(key, value);
        }

        // 👇 Generic enumerator
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return dict.GetEnumerator();
        }

        // 👇 Non-generic enumerator
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
    [Serializable]
    public enum UploadState
    {
        NotUploaded,        // Item exists but hasn't been sent to server
        Uploaded,           // Item successfully uploaded to server
        FeedbackOutOfDate,  // Item uploaded but feedback changed since upload
        UploadFailed        // Upload attempt failed, needs retry
    }

    [Serializable]
    public class PrototypingItem
    {
        public string title;
        public string type;
        public string generationId;
        public StringStringMap context; // Assumed to be immutable.
        public StringStringMap prediction; // Assumed to be immutable.
        public StringStringMap feedback; // May be updated as the user interacts with the prototyping window.
        
        // Upload state tracking
        public UploadState uploadState = UploadState.NotUploaded;
        public string lastUploadedAt; // ISO 8601 timestamp of last successful upload
        public string feedbackLastModifiedAt; // ISO 8601 timestamp of last feedback change
        public int uploadRetryCount = 0; // Number of failed upload attempts
    }
}
