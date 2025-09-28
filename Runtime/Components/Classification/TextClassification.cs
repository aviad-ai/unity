using System;
using UnityEngine;

namespace Aviad
{
    public class TextClassification : MonoBehaviour
    {
        public Runner runner;
        public Clusters clusters;

        public void Predict(string text, Action<string> OnPrediction)
        {
            if (runner == null) return;
            if (clusters == null) return;
            void OnEmbeddings(float[] probabilities)
            {
                string prediction = clusters.Predict(probabilities);
                OnPrediction?.Invoke(prediction);
#if UNITY_EDITOR
                var item = new PrototypingItem
                {
                    title = $"Embedding: {text}",
                    type = "embedding",
                    context = new StringStringMap
                    {
                        { "text", text }
                    },
                    prediction = new StringStringMap
                    {
                        { "prediction", prediction },
                        { "probabilities", string.Join(",", probabilities) }
                    },
                    feedback = new StringStringMap()
                };
                EditorRuntimeLogger.Instance?.Log(item);
#endif
            }
            runner.GetEmbeddings(text, OnEmbeddings);
        }
    }
}