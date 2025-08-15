using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.WSA;
namespace Aviad
{
    public abstract class AviadCharacterComponent : MonoBehaviour
    {
        public abstract void TriggerTask(Action<bool> onComplete);
        public abstract void SetCharacterDetails(string[] details);
    }
}