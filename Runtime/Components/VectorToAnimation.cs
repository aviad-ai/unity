using UnityEngine;
using UnityEngine.Events;


namespace Aviad
{
    /// <summary>
    /// Vector to Animation demo script to show using arbitrary string input to drive predefined list of animations.
    /// </summary>
    public class VectorToAnimation : MonoBehaviour
    {

        [Header("Animation")]
        [SerializeField] private Animator animator;

        [Header("Character State")]
        [SerializeField] private CharacterState currentState = CharacterState.Idle;

        [SerializeField] AviadClusters clusters;

        [SerializeField] AviadRunner runner;

        [SerializeField] UnityEvent<string> outputEventString;
        private string cachedString = "";
        public enum CharacterState
        {
            Dance,
            Idle,
            Sit,
            Angry
        }

        // Property to get/set the current state
        public CharacterState CurrentState
        {
            get { return currentState; }
            set
            {
                if (currentState != value)
                {
                    currentState = value;
                }
            }
        }

        private void Start()
        {
            // Get animator component if not assigned
            if (animator == null)
                animator = GetComponent<Animator>();

            // Set initial animation state
            UpdateAnimation();
        }

        private void UpdateAnimation()
        {
            if (animator == null) return;

            // Set the appropriate trigger based on current state
            switch (currentState)
            {
                case CharacterState.Dance:
                    animator.SetTrigger("Dance");
                    break;
                case CharacterState.Idle:
                    animator.SetTrigger("Idle");
                    break;
                case CharacterState.Angry:
                    animator.SetTrigger("Angry");
                    break;
            }
        }
        public void GenerateAnimationFromString(string text)
        {
            cachedString = text;
            Debug.Log("Text to be embedded:" + text);
            runner.GetEmbeddings(text, AnimateAndLog);
        }

        public void AnimateAndLog(float[] embeddings)
        {
            string tag = AnimateWithEmbedding(embeddings);
            string message = $"Tag generated from embedding \'{cachedString} \':" + tag;
            Debug.Log(message);
            outputEventString?.Invoke(message);
        }
        public string AnimateWithEmbedding(float[] embedding)
        {
            Debug.Log(embedding);
            Debug.Log(clusters);

            string clusterTag = clusters.Predict(embedding);
            if (clusterTag.ToLower() == "dance")
            {
                SetDanceState();
            }
            else if (clusterTag.ToLower() == "not dance")
            {
                SetIdleState();
            }
            else if (clusterTag.ToLower() == "angry")
            {
                SetAngryState();
            }
            return clusterTag;
        }

        // Public methods to change states
        public void SetDanceState()
        {
            CurrentState = CharacterState.Dance;
            UpdateAnimation();
        }
        public void SetIdleState()
        {
            CurrentState = CharacterState.Idle;
            UpdateAnimation();
        }
        public void SetAngryState()
        {
            CurrentState = CharacterState.Angry;
            UpdateAnimation();
        }
    }
}