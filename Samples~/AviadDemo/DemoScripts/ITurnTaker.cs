using UnityEngine;
using System;

namespace Aviad
{
    public interface ITurnTaker
    {
        // Tick should enable actions and start turn
        void Tick();

        // Tock should clean up turn and pass the puck, disabling actions
        void Tock();

        void ExitTurnBased();
        void EnterTurnBased();
        int GetInitiative();

        // Set the callback to notify TurnManager when turn is complete
        void SetTurnCompleteCallback(Action onTurnComplete);
    }
}
