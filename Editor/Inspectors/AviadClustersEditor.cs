using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Aviad
{
    [CustomEditor(typeof(AviadClusters))]
    public class AviadClustersEditor : AviadManagerEditorBase
    {
        private AviadClusters clusters;
        private string newClusterTag = "";
        private string newContributorInput = "";
        private string selectedClusterTag = "";
        private Vector2 clustersScrollPosition;
        private Vector2 contributorsScrollPosition;
        private bool showAddCluster = true;
        private bool showAddContributor = true;
        private bool showTestEmbedding = false;
        private string testEmbeddingInput = "";
        private Dictionary<string, float> lastProbabilities;
        private bool isComputingEmbedding = false;
        private string computingFor = "";

        // Override to disable automatic manager controls since we want custom placement
        protected override bool ShouldDrawManagerControls => false;

        public override void OnInspectorGUI()
        {
            clusters = (AviadClusters)base.target;

            // Draw the default inspector first
            DrawDefaultInspector();

            EditorGUILayout.Space(10);

            // Draw manager controls at the top of our custom section
            DrawAviadManagerControls();

            EditorGUILayout.LabelField("Cluster Management", EditorStyles.boldLabel);

            if (!IsFullyInitialized())
            {
                EditorGUILayout.HelpBox("AviadManager not ready. Cannot compute embeddings.", MessageType.Warning);
                DrawClusterManagementReadOnly();
                return;
            }

            DrawClusterManagement();
            EditorGUILayout.Space(10);
            DrawTestInterface();
        }

        private void DrawClusterManagementReadOnly()
        {
            EditorGUILayout.Space(5);

            // Show existing clusters in read-only mode
            var clusterTags = clusters.GetClusterTags().ToList();
            if (clusterTags.Count > 0)
            {
                EditorGUILayout.LabelField($"Existing Clusters ({clusterTags.Count}):", EditorStyles.miniLabel);

                clustersScrollPosition = EditorGUILayout.BeginScrollView(clustersScrollPosition, GUILayout.MaxHeight(100));
                foreach (string tag in clusterTags)
                {
                    var cluster = clusters.GetCluster(tag);
                    EditorGUILayout.LabelField($"• {tag} ({cluster.Contributors.Count} contributors)", EditorStyles.miniLabel);
                }
                EditorGUILayout.EndScrollView();
            }
            else
            {
                EditorGUILayout.LabelField("No clusters defined", EditorStyles.miniLabel);
            }
        }

        private void DrawClusterManagement()
        {
            // Add new cluster section
            showAddCluster = EditorGUILayout.Foldout(showAddCluster, "Add New Cluster", true);
            if (showAddCluster)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginHorizontal();
                newClusterTag = EditorGUILayout.TextField("Cluster Tag:", newClusterTag);

                GUI.enabled = !string.IsNullOrEmpty(newClusterTag) && !clusters.GetClusterTags().Contains(newClusterTag);
                if (GUILayout.Button("Add Cluster", GUILayout.Width(100)))
                {
                    clusters.AddCluster(newClusterTag);
                    newClusterTag = "";
                    EditorUtility.SetDirty(clusters);
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            // Add contributor section
            showAddContributor = EditorGUILayout.Foldout(showAddContributor, "Add Contributor to Cluster", true);
            if (showAddContributor)
            {
                EditorGUI.indentLevel++;

                // Cluster selection dropdown
                var clusterTags = clusters.GetClusterTags().ToArray();
                if (clusterTags.Length > 0)
                {
                    int selectedIndex = System.Array.IndexOf(clusterTags, selectedClusterTag);
                    if (selectedIndex < 0) selectedIndex = 0;

                    selectedIndex = EditorGUILayout.Popup("Target Cluster:", selectedIndex, clusterTags);
                    selectedClusterTag = clusterTags[selectedIndex];

                    // Input field for contributor text
                    newContributorInput = EditorGUILayout.TextField("Input Text:", newContributorInput);

                    EditorGUILayout.BeginHorizontal();

                    GUI.enabled = !string.IsNullOrEmpty(newContributorInput) && !isComputingEmbedding;
                    if (GUILayout.Button(isComputingEmbedding ? "Computing..." : "Add Contributor"))
                    {
                        AddContributorWithEmbedding(selectedClusterTag, newContributorInput);
                    }
                    GUI.enabled = true;

                    if (isComputingEmbedding)
                    {
                        EditorGUILayout.LabelField($"Computing embedding for: {computingFor}", EditorStyles.miniLabel);
                    }

                    EditorGUILayout.EndHorizontal();
                }
                else
                {
                    EditorGUILayout.HelpBox("Create a cluster first before adding contributors.", MessageType.Info);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);

            // Display existing clusters and contributors
            DrawExistingClusters();
        }

        private void DrawExistingClusters()
        {
            var clusterTags = clusters.GetClusterTags().ToList();
            if (clusterTags.Count == 0) return;

            EditorGUILayout.LabelField($"Existing Clusters ({clusterTags.Count}):", EditorStyles.boldLabel);

            clustersScrollPosition = EditorGUILayout.BeginScrollView(clustersScrollPosition, GUILayout.MaxHeight(200));

            foreach (string tag in clusterTags)
            {
                var cluster = clusters.GetCluster(tag);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"• {tag} ({cluster.Contributors.Count} contributors)", EditorStyles.miniLabel);

                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    if (EditorUtility.DisplayDialog("Remove Cluster",
                        $"Are you sure you want to remove cluster '{tag}' and all its contributors?",
                        "Remove", "Cancel"))
                    {
                        clusters.RemoveCluster(tag);
                        EditorUtility.SetDirty(clusters);
                        break; // Exit loop since we modified the collection
                    }
                }
                EditorGUILayout.EndHorizontal();

                // Show contributors
                if (cluster.Contributors.Count > 0)
                {
                    EditorGUI.indentLevel++;
                    for (int i = cluster.Contributors.Count - 1; i >= 0; i--)
                    {
                        var contributor = cluster.Contributors[i];
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField($"    \"{contributor.Input}\"", EditorStyles.miniLabel);

                        if (GUILayout.Button("×", GUILayout.Width(20), GUILayout.Height(16)))
                        {
                            if (EditorUtility.DisplayDialog("Remove Contributor",
                                $"Remove contributor \"{contributor.Input}\" from cluster '{tag}'?",
                                "Remove", "Cancel"))
                            {
                                clusters.RemoveContributor(tag, i);
                                EditorUtility.SetDirty(clusters);
                                break; // Exit loop since we modified the collection
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUI.indentLevel--;
                }
                else
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField("    (no contributors)", EditorStyles.miniLabel);
                    EditorGUI.indentLevel--;
                }
            }

            EditorGUILayout.EndScrollView();
        }
        private void DrawTestInterface()
        {
            showTestEmbedding = EditorGUILayout.Foldout(showTestEmbedding, "Test Embedding Classification", true);
            if (!showTestEmbedding) return;

            EditorGUI.indentLevel++;

            if (clusters.ClusterCount == 0)
            {
                EditorGUILayout.HelpBox("Add some clusters and contributors first to test classification.", MessageType.Info);
                EditorGUI.indentLevel--;
                return;
            }

            testEmbeddingInput = EditorGUILayout.TextField("Test Input:", testEmbeddingInput);

            EditorGUILayout.BeginHorizontal();
            GUI.enabled = !string.IsNullOrEmpty(testEmbeddingInput) && !isComputingEmbedding;
            if (GUILayout.Button(isComputingEmbedding ? "Computing..." : "Test Classification"))
            {
                TestEmbeddingClassification(testEmbeddingInput);
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            // Display last results
            if (lastProbabilities != null && lastProbabilities.Count > 0)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Classification Results:", EditorStyles.boldLabel);

                var sortedResults = lastProbabilities.OrderByDescending(kvp => kvp.Value).ToList();
                foreach (var result in sortedResults)
                {
                    EditorGUILayout.LabelField($"{result.Key}: {result.Value:P1}");
                }

                var prediction = sortedResults.First();
                EditorGUILayout.HelpBox($"Predicted: {prediction.Key} ({prediction.Value:P1} confidence)", MessageType.Info);
            }

            EditorGUI.indentLevel--;
        }

        private void StartEmbeddingComputation(string input)
        {
            isComputingEmbedding = true;
            computingFor = input;
            Repaint();
        }

        private void EndEmbeddingComputation()
        {
            isComputingEmbedding = false;
            computingFor = "";
            Repaint();
        }

        private bool ValidateManagerReady()
        {
            return IsFullyInitialized();
        }

        private void AddContributorWithEmbedding(string clusterTag, string input)
        {
            if (!ValidateManagerReady())
                return;

            StartEmbeddingComputation(input);

            AviadManager.Instance.ComputeEmbeddings(AviadEditorLifecycle.Runtime.ModelId, input, new LlamaEmbeddingParams(), embedding =>
            {
                if (embedding != null)
                {
                    clusters.AddContributor(clusterTag, input, embedding);
                    newContributorInput = "";
                    EditorUtility.SetDirty(clusters);
                    EndEmbeddingComputation();
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", $"Failed to compute embedding for: {input}", "OK");
                    EndEmbeddingComputation();
                }
            });
        }

        private void TestEmbeddingClassification(string input)
        {
            if (!ValidateManagerReady())
                return;

            StartEmbeddingComputation(input);

            AviadManager.Instance.ComputeEmbeddings(AviadEditorLifecycle.Runtime.ModelId, input, new LlamaEmbeddingParams(), embedding =>
            {
                if (embedding != null)
                {
                    lastProbabilities = clusters.ComputeProbabilities(embedding);
                    EndEmbeddingComputation();
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", $"Failed to compute embedding for: {input}", "OK");
                    lastProbabilities = null;
                    EndEmbeddingComputation();
                }
            });
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            clusters = (AviadClusters)base.target;
        }

        protected override void OnDisable()
        {
            // Reset computing state so buttons are clickable if inspector reopens
            isComputingEmbedding = false;
            computingFor = "";

            base.OnDisable();
        }
    }
}