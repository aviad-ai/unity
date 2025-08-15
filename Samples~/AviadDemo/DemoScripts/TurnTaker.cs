using System;
using Aviad;
using UnityEngine;
using UnityEngine.Events;

public class TurnTaker : MonoBehaviour, ITurnTaker
{
    [SerializeField] int initiaitive = 0;
    [SerializeField] GameObject _turnIndicator;
    [SerializeField] UnityEvent OnTick;
    
    private Action onTurnComplete;
    public void EnterTurnBased()
    {
        throw new NotImplementedException();
    }

    public void ExitTurnBased()
    {
        throw new NotImplementedException();
    }

    public int GetInitiative()
    {
        return initiaitive;
    }
     
    public void SetTurnCompleteCallback(Action callback)
    {
        onTurnComplete = callback;

    }

    public void Tock()
    {
        if (_turnIndicator != null)
            SetTurnIndicator(false);
        // Notify TurnManager that turn is complete
        onTurnComplete?.Invoke();
    }


    public void Tick()
    {
        SetTurnIndicator(true);
        OnTick?.Invoke();
    }
    void SetTurnIndicator(bool turnIndicatorActive)
    {
        _turnIndicator?.SetActive(turnIndicatorActive);
    }
}
