using UnityEngine;
using System;
using UnityEngine.Events;

namespace Aviad
{
    public class AviadAICharacter : MonoBehaviour
    {
        
        [SerializeField] private AviadCharacterComponent[] characterComponents;
        [SerializeField] private string _charName = "Goblin";
        [SerializeField] private string _charDesc = "Fantasy character.";
        int completeCounter = 0;
        bool isDone;
        public UnityEvent OnComponentsComplete;


        private void Update()
        {
            if (isDone)
            {
                isDone = false;
                Debug.Log($"{_charName} finished turn.");
                OnComponentsComplete?.Invoke();
            }
        }


        public void PerformAllCharacterComponents()
        {
            Debug.Log($"{_charName}'s turn started - Tick()");
            completeCounter = 0;
            foreach(var task in characterComponents)
            {
                task.SetCharacterDetails(new string[] { _charName, _charDesc });
                task.TriggerTask(SetDone);
            }
        }

        public void SetDone(bool value) {
            completeCounter = value ? completeCounter+1: completeCounter;
            if(completeCounter==characterComponents.Length)
                isDone = value;
        }

    }
}
