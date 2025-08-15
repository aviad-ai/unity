using Aviad;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

namespace Aviad.Samples
{
    public class SimpleDemoStateDialogueLoop : MonoBehaviour
    {
        public static event Action<int> OnRoundCompleted;
        TurnManager turnManager;
        int _roundsCompleted = 0;
        public bool active = true;
        public bool paused = false;
        bool init = false;

        [SerializeField] Light activeLight;
        //private void OnTriggerStay(Collider other)
        //{
        //    if(other.gameObject.name == "Player")
        //    {
        //        active = true;
        //    }

        //}
        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.name == "Player")
            {
                active = false;
                activeLight.gameObject.SetActive(active);

            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.name == "Player")
            {
                active = true;
                activeLight.gameObject.SetActive(active);
                if (paused)
                {
                    StartNextRound();
                    paused = false;
                }
                else
                {
                    if (init == false)
                    {
                        StartNextRound();
                        init = true;
                    }
                }
            }
        }

        void InitializeManagers()
        {
            // Find and set up TurnManager callback (only used during PERFORMANCE state)
            if (turnManager == null)
                turnManager = this.gameObject.GetComponent<TurnManager>();
            if (turnManager != null)
            {
                turnManager.SetRoundCompleteCallback(OnRoundComplete);
                Debug.Log("GameStateManager: TurnManager callback set up");
            }
            else
            {
                Debug.LogWarning("GameStateManager: No TurnManager found in scene");
            }
        }

        void OnRoundComplete()
        {
            _roundsCompleted++;
            Debug.Log($"GameStateManager: Round {_roundsCompleted} completed");

            // Notify other systems about round completion
            OnRoundCompleted?.Invoke(_roundsCompleted);

            if (ShouldEndGame())
            {
                Debug.Log("Demo Ended! This was 10 rounds of dialogue, cant wait to see what you build!");
                return; // Move to ENDSTATE
            }
            else
            {
                if (active)
                {
                    StartNextRound();
                }
                else
                {
                    paused = true;
                }
            }

        }

        void StartNextRound()
        {
            if (turnManager != null && !turnManager.IsRoundInProgress())
            {
                Debug.Log("Starting new round");
                turnManager.StartRound();
            }
            else
            {
                throw new Exception("Turn manager failed to start round!");
            }
        }

        bool ShouldEndGame()
        {
            if (_roundsCompleted > 10)
            {
                return true;
            }

            return false;
        }

        void Start()
        {
            InitializeManagers();
            //StartCoroutine("DelayedStartNextRound");
        }

        IEnumerator DelayedStartNextRound()
        {
            yield return new WaitForSeconds(1);
            StartNextRound();
        }
    }
}