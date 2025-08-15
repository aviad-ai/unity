using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Aviad
{
    [System.Serializable]
    public class ClusterContributor
    {
        [SerializeField] private string input;
        [SerializeField] private float[] embedding;

        public string Input => input;
        public float[] Embedding => embedding;

        public ClusterContributor(string input, float[] embedding)
        {
            this.input = input ?? throw new ArgumentNullException(nameof(input));
            this.embedding = embedding ?? throw new ArgumentNullException(nameof(embedding));
        }
    }

    [System.Serializable]
    public class Cluster
    {
        [SerializeField] private string tag;
        [SerializeField] private List<ClusterContributor> contributors;

        public string Tag => tag;
        public List<ClusterContributor> Contributors => contributors;

        public Cluster(string tag)
        {
            this.tag = tag ?? throw new ArgumentNullException(nameof(tag));
            this.contributors = new List<ClusterContributor>();
        }

        public void AddContributor(string input, float[] embedding)
        {
            if (embedding == null)
            {
                AviadLogger.Warning("Contributor embedding cannot be null.");
                return;
            }
            contributors.Add(new ClusterContributor(input, embedding));
        }

        // Compute the centroid of all contributors in this cluster
        public float[] GetCentroid()
        {
            if (contributors == null || contributors.Count == 0)
            {
                AviadLogger.Warning("Cannot compute centroid: no contributors in cluster.");
                return null;
            }

            // Get the first embedding to determine dimensionality
            var firstEmbedding = contributors[0].Embedding;
            if (firstEmbedding == null)
            {
                AviadLogger.Warning("Cannot compute centroid: first contributor has null embedding.");
                return null;
            }

            int dimensions = firstEmbedding.Length;
            float[] centroid = new float[dimensions];

            // Sum all embeddings
            foreach (var contributor in contributors)
            {
                if (contributor.Embedding == null)
                {
                    AviadLogger.Warning("Skipping contributor with null embedding in centroid calculation.");
                    continue;
                }

                if (contributor.Embedding.Length != dimensions)
                {
                    AviadLogger.Warning("Skipping contributor with mismatched embedding dimensions in centroid calculation.");
                    continue;
                }

                for (int i = 0; i < dimensions; i++)
                {
                    centroid[i] += contributor.Embedding[i];
                }
            }

            // Average the sum
            for (int i = 0; i < dimensions; i++)
            {
                centroid[i] /= contributors.Count;
            }

            return centroid;
        }
    }

    public class AviadClusters : MonoBehaviour
    {
        [SerializeField] private List<Cluster> clusters = new List<Cluster>();
        [SerializeField] private bool useCosineDistance = true;

        public int ClusterCount => clusters.Count;
        public bool UseCosineDistance
        {
            get => useCosineDistance;
            set => useCosineDistance = value;
        }

        private void Awake()
        {
            if (clusters == null)
                clusters = new List<Cluster>();
        }

        // Add a new cluster
        public void AddCluster(string tag)
        {
            if (string.IsNullOrEmpty(tag))
            {
                AviadLogger.Error("Tag cannot be null or empty");
                return;
            }

            if (clusters.Any(c => c.Tag == tag))
            {
                AviadLogger.Warning($"Cluster with tag '{tag}' already exists");
                return;
            }

            clusters.Add(new Cluster(tag));
            AviadLogger.Debug($"Added cluster: {tag}");
        }

        // Add a contributor to an existing cluster
        public void AddContributor(string clusterTag, string input, float[] embedding)
        {
            var cluster = clusters.FirstOrDefault(c => c.Tag == clusterTag);
            if (cluster == null)
            {
                AviadLogger.Error($"Cluster with tag '{clusterTag}' not found");
                return;
            }

            if (embedding == null || embedding.Length == 0)
            {
                AviadLogger.Error("Embedding cannot be null or empty");
                return;
            }

            cluster.AddContributor(input, embedding);

            AviadLogger.Debug($"Added contributor to cluster '{clusterTag}': {input}");
        }

        // Get all cluster tags
        public IEnumerable<string> GetClusterTags()
        {
            return clusters.Select(c => c.Tag);
        }

        // Get cluster by tag
        public Cluster GetCluster(string tag)
        {
            return clusters.FirstOrDefault(c => c.Tag == tag);
        }

        // Compute cosine similarity between two vectors
        private static float CosineSimilarity(float[] vector1, float[] vector2)
        {
            if (vector1 == null || vector2 == null)
            {
                AviadLogger.Warning("Cannot compute cosine similarity: one or both vectors are null.");
                return 0f;
            }

            if (vector1.Length != vector2.Length)
            {
                AviadLogger.Warning("Cannot compute cosine similarity: vectors have different dimensions.");
                return 0f;
            }

            float dotProduct = 0f;
            float magnitude1 = 0f;
            float magnitude2 = 0f;

            for (int i = 0; i < vector1.Length; i++)
            {
                dotProduct += vector1[i] * vector2[i];
                magnitude1 += vector1[i] * vector1[i];
                magnitude2 += vector2[i] * vector2[i];
            }


            magnitude1 = Mathf.Sqrt(magnitude1);
            magnitude2 = Mathf.Sqrt(magnitude2);

            if (magnitude1 == 0f || magnitude2 == 0f)
            {
                AviadLogger.Warning("Cannot compute cosine similarity: one or both vectors have zero magnitude.");
                return 0f;
            }

            return dotProduct / (magnitude1 * magnitude2);
        }

        // Compute Euclidean distance between two vectors
        private static float EuclideanDistance(float[] vector1, float[] vector2)
        {
            if (vector1 == null || vector2 == null)
            {
                AviadLogger.Warning("Cannot compute Euclidean distance: one or both vectors are null.");
                return float.MaxValue;
            }

            if (vector1.Length != vector2.Length)
            {
                AviadLogger.Warning("Cannot compute Euclidean distance: vectors have different dimensions.");
                return float.MaxValue;
            }

            float sumSquaredDifferences = 0f;

            for (int i = 0; i < vector1.Length; i++)
            {
                float difference = vector1[i] - vector2[i];
                sumSquaredDifferences += difference * difference;
            }

            return Mathf.Sqrt(sumSquaredDifferences);
        }

        // Compute output probability vector for a given embedding
        public Dictionary<string, float> ComputeProbabilities(float[] inputEmbedding)
        {
            if (inputEmbedding == null)
            {
                AviadLogger.Warning("Cannot compute probabilities: input embedding is null.");
                return new Dictionary<string, float>();
            }

            if (clusters.Count == 0)
            {
                AviadLogger.Warning("Cannot compute probabilities: no clusters available.");
                return new Dictionary<string, float>();
            }

            var similarities = new Dictionary<string, float>();

            foreach (var cluster in clusters)
            {
                var centroid = cluster.GetCentroid();
                if (centroid == null)
                {
                    AviadLogger.Warning($"Skipping cluster '{cluster.Tag}': centroid is null.");
                    continue;
                }

                float similarity;
                if (useCosineDistance)
                {
                    similarity = CosineSimilarity(inputEmbedding, centroid);
                }
                else
                {
                    float distance = EuclideanDistance(inputEmbedding, centroid);
                    // Convert distance to similarity (inverse relationship)
                    similarity = distance == 0f ? 1f : 1f / (1f + distance);
                }

                similarities[cluster.Tag] = similarity;
            }
            return Softmax(similarities);
        }

        // Apply softmax to convert similarities to probabilities
        private static Dictionary<string, float> Softmax(Dictionary<string, float> similarities)
        {
            if (similarities == null || similarities.Count == 0)
            {
                AviadLogger.Warning("Cannot apply softmax: similarities dictionary is null or empty.");
                return new Dictionary<string, float>();
            }

            var probabilities = new Dictionary<string, float>();

            // Find the maximum value to prevent overflow
            float maxValue = similarities.Values.Max();

            // Compute exponentials and sum
            float sum = 0f;
            var exponentials = new Dictionary<string, float>();

            foreach (var kvp in similarities)
            {
                float exp = Mathf.Exp(kvp.Value - maxValue);
                exponentials[kvp.Key] = exp;
                sum += exp;
            }

            if (sum == 0f)
            {
                AviadLogger.Warning("Softmax sum is zero, returning uniform probabilities.");
                float uniformProb = 1f / similarities.Count;
                foreach (var key in similarities.Keys)
                {
                    probabilities[key] = uniformProb;
                }
                return probabilities;
            }

            // Normalize to get probabilities
            foreach (var kvp in exponentials)
            {
                probabilities[kvp.Key] = kvp.Value / sum;
            }

            return probabilities;
        }

        // Get the most likely cluster tag for a given embedding
        public string Predict(float[] inputEmbedding)
        {
            var probabilities = ComputeProbabilities(inputEmbedding);
            var prediction = probabilities.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key;

            if (prediction != null)
            {
                AviadLogger.Debug($"Prediction: {prediction} (probability: {probabilities[prediction]:F3})");
            }

            return prediction;
        }

        // Validate serialized data
        private void OnValidate()
        {
            if (clusters == null)
                clusters = new List<Cluster>();
        }

        // Public method to add cluster from Inspector or other scripts
        public bool TryAddCluster(string tag)
        {
            if (string.IsNullOrEmpty(tag) || clusters.Any(c => c.Tag == tag))
                return false;

            clusters.Add(new Cluster(tag));
            return true;
        }

        // Public method to remove cluster
        public bool RemoveCluster(string tag)
        {
            var cluster = clusters.FirstOrDefault(c => c.Tag == tag);
            if (cluster != null)
            {
                clusters.Remove(cluster);
                AviadLogger.Debug($"Removed cluster: {tag}");
                return true;
            }
            return false;
        }

        public bool RemoveContributor(string clusterTag, int contributorIndex)
        {
            var cluster = clusters.FirstOrDefault(c => c.Tag == clusterTag);
            if (cluster == null)
            {
                AviadLogger.Error($"Cluster with tag '{clusterTag}' not found");
                return false;
            }

            if (contributorIndex < 0 || contributorIndex >= cluster.Contributors.Count)
            {
                AviadLogger.Error($"Invalid contributor index {contributorIndex} for cluster '{clusterTag}'. Valid range: 0-{cluster.Contributors.Count - 1}");
                return false;
            }

            var contributor = cluster.Contributors[contributorIndex];
            cluster.Contributors.RemoveAt(contributorIndex);

            AviadLogger.Debug($"Removed contributor from cluster '{clusterTag}': {contributor.Input}");
            return true;
        }
    }
}