using UnityEngine;
using UnityEditor;

namespace Aviad
{
    [InitializeOnLoad]
    public static class AviadRunnerPlayModeValidator
    {
        static AviadRunnerPlayModeValidator()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Only check when entering play mode
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                // Find all AviadRunner instances in the scene
                AviadRunner[] runners = Object.FindObjectsByType<AviadRunner>(
                    FindObjectsInactive.Exclude,
                    FindObjectsSortMode.None
                );
                foreach (var runner in runners)
                {
                    if (runner.saveToStreamingAssets &&
                        (runner.modelAsset == null ||
                         !System.IO.File.Exists(AviadModelRuntime.GetExpectedModelPath(runner.modelAsset.modelUrl))))
                    {
                        Debug.LogError(
                            $"Cannot enter Play Mode: The model asset '{runner.modelAsset?.name ?? "NULL"}' is missing on '{runner.name}'. Please download it first.");

                        // Cancel play mode
                        EditorApplication.isPlaying = false;
                        return;
                    }
                }
            }
        }
    }
}