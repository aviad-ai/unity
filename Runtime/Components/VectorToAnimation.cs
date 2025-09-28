using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System.Linq;

namespace Aviad
{
    /// <summary>
    /// Maps cluster tags to animation triggers
    /// </summary>
    [System.Serializable]
    public class TagAnimationMapping
    {
        [SerializeField] private string clusterTag;
        [SerializeField] private List<string> animationTriggers = new List<string>();

        public string ClusterTag 
        { 
            get => clusterTag; 
            set => clusterTag = value; 
        }
        
        public List<string> AnimationTriggers 
        { 
            get => animationTriggers; 
            set => animationTriggers = value ?? new List<string>(); 
        }

        public TagAnimationMapping(string tag)
        {
            clusterTag = tag;
            animationTriggers = new List<string>();
        }

        public TagAnimationMapping(string tag, List<string> triggers)
        {
            clusterTag = tag;
            animationTriggers = triggers ?? new List<string>();
        }
    }

    /// <summary>
    /// Vector to Animation component that uses cluster contributors to drive animation parameters.
    /// </summary>
    public class VectorToAnimation : MonoBehaviour
    {
        [Header("Animation")]
        [SerializeField] private Animator animator;

        [Header("Cluster Configuration")]
        [SerializeField] private Clusters clusters;
        [SerializeField] private Runner runner;

        [Header("Tag Animation Mappings")]
        [SerializeField] private List<TagAnimationMapping> tagAnimationMappings = new List<TagAnimationMapping>();

        [Header("Output Tag Event")]
        [SerializeField] private UnityEvent<string> outputEmbeddingTagEvent;
        
        private string cachedString = "";

        // Public properties for editor access
        public List<TagAnimationMapping> TagAnimationMappings 
        { 
            get => tagAnimationMappings; 
            set => tagAnimationMappings = value ?? new List<TagAnimationMapping>(); 
        }

        public Clusters ClustersComponent => clusters;
        public Animator AnimatorComponent => animator;

        private void Start()
        {
            // Get animator component if not assigned
            if (animator == null)
                animator = GetComponent<Animator>();

            // Validate configuration
            ValidateConfiguration();
        }

        private void ValidateConfiguration()
        {
            if (clusters == null)
            {
                PackageLogger.Warning("VectorToAnimation: Clusters component not assigned.");
            }
            
            if (animator == null)
            {
                PackageLogger.Warning("VectorToAnimation: Animator component not found.");
            }
            
            if (runner == null)
            {
                PackageLogger.Warning("VectorToAnimation: Runner component not assigned.");
            }
        }
        public void GenerateAnimationFromString(string text)
        {
            cachedString = text;
            PackageLogger.Info("Text to be embedded:" + text);
            runner.GetEmbeddings(text, AnimateAndLog);
        }

        public void AnimateAndLog(float[] embeddings)
        {
            var result = AnimateWithEmbedding(embeddings);
            string message = $"Tag: '{result.clusterTag}', Triggers: [{string.Join(", ", result.triggeredAnimations)}]";
            PackageLogger.Info($"Animation result from embedding '{cachedString}': {message}");
            outputEmbeddingTagEvent?.Invoke(message);
        }

        public (string clusterTag, List<string> triggeredAnimations) AnimateWithEmbedding(float[] embedding)
        {
            if (clusters == null)
            {
                PackageLogger.Warning("VectorToAnimation: Clusters component not assigned.");
                return (null, new List<string>());
            }

            if (animator == null)
            {
                PackageLogger.Warning("VectorToAnimation: Animator component not found.");
                return (null, new List<string>());
            }

            // Get cluster prediction
            string clusterTag = clusters.Predict(embedding);
            if (string.IsNullOrEmpty(clusterTag))
            {
                PackageLogger.Warning("VectorToAnimation: No cluster prediction returned.");
                return (null, new List<string>());
            }

            // Find the tag mapping
            var tagMapping = tagAnimationMappings.FirstOrDefault(t => t.ClusterTag == clusterTag);
            if (tagMapping == null)
            {
                PackageLogger.Warning($"VectorToAnimation: No animation mapping found for tag '{clusterTag}'.");
                return (clusterTag, new List<string>());
            }

            // Trigger all assigned animations for this tag
            var triggeredAnimations = new List<string>();
            foreach (string trigger in tagMapping.AnimationTriggers)
            {
                if (!string.IsNullOrEmpty(trigger))
                {
                    try
                    {
                        animator.SetTrigger(trigger);
                        triggeredAnimations.Add(trigger);
                        PackageLogger.Debug($"VectorToAnimation: Triggered animation '{trigger}' for tag '{clusterTag}'");
                    }
                    catch (System.Exception ex)
                    {
                        PackageLogger.Warning($"VectorToAnimation: Failed to trigger animation '{trigger}': {ex.Message}");
                    }
                }
            }

            return (clusterTag, triggeredAnimations);
        }

        /// <summary>
        /// Synchronizes the animation mappings with the current cluster tags
        /// </summary>
        public void SyncWithClusters()
        {
            if (clusters == null)
            {
                PackageLogger.Warning("VectorToAnimation: Cannot sync - Clusters component not assigned.");
                return;
            }

            var clusterTags = clusters.GetClusterTags().ToList();
            
            // Remove mappings for tags that no longer exist
            tagAnimationMappings.RemoveAll(mapping => !clusterTags.Contains(mapping.ClusterTag));
            
            // Add mappings for new cluster tags
            foreach (string clusterTag in clusterTags)
            {
                // Find or create tag mapping
                var tagMapping = tagAnimationMappings.FirstOrDefault(t => t.ClusterTag == clusterTag);
                if (tagMapping == null)
                {
                    tagMapping = new TagAnimationMapping(clusterTag);
                    tagAnimationMappings.Add(tagMapping);
                }
            }

            PackageLogger.Debug("VectorToAnimation: Synchronized with cluster tags.");
        }

        /// <summary>
        /// Gets all available trigger parameters from the attached Animator Controller
        /// </summary>
        public List<string> GetAvailableAnimationTriggers()
        {
            var triggers = new List<string>();
            
            if (animator == null || animator.runtimeAnimatorController == null)
                return triggers;

            foreach (var parameter in animator.parameters)
            {
                if (parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    triggers.Add(parameter.name);
                }
            }

            return triggers;
        }
    }
}