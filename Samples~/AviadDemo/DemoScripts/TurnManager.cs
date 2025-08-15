using System.Collections.Generic;
using System.Linq;
using Aviad;
using UnityEngine;
using System;
using System.Collections;

public class TurnManager : MonoBehaviour
{
    // Needs to manage all ITurnTakers

    ITurnTaker[] turnTakers;
    List<ITurnTaker> sortedTurnTakers;
    int currentTurn = 0;
    bool roundInProgress = false;
    [SerializeField] GameObject[] conversationParticipants;
    
    // Callback for when a round is complete
    private Action onRoundComplete;

    public void SetRoundCompleteCallback(Action callback)
    {
        onRoundComplete = callback;
    }

    void Start()
    {
        InitializeTurnTakers();
    }

    void InitializeTurnTakers()
    {
        // Find all ITurnTaker components in the scene
        turnTakers = conversationParticipants
            .SelectMany(go => go.GetComponents<ITurnTaker>())
            .ToArray();

        // Set up callbacks for each turn taker
        foreach (var turnTaker in turnTakers)
        {
            turnTaker.SetTurnCompleteCallback(OnTurnComplete);
            //turnTaker.EnterTurnBased();
        }
        
        SortTurnTakers();
    }

    // Orders those to take turns by initiative
    void SortTurnTakers()
    {
        if (turnTakers == null || turnTakers.Length == 0) return;
        
        sortedTurnTakers = turnTakers.ToList();
        // Fixed: Actually assign the sorted result back to the list
        sortedTurnTakers = sortedTurnTakers.OrderByDescending(t => t.GetInitiative()).ToList();
        Debug.Log($"Returning turn taker count. {sortedTurnTakers.Count}");
        
    }

    public void StartRound()
    {
        if (sortedTurnTakers == null || sortedTurnTakers.Count == 0)
        {
            Debug.LogWarning("No turn takers available to start round");
            return;
        }

        currentTurn = 0;
        roundInProgress = true;
        
        //some gui stuff
        //some setup
        
        // Start first turn taker
        Debug.Log($"Starting round. First turn: {sortedTurnTakers[currentTurn].GetType().Name}");
        sortedTurnTakers[currentTurn].Tick();
    }

    // This method is called when a turn taker calls Tock()
    void OnTurnComplete()
    {
        if (!roundInProgress) return;

        Debug.Log($"Turn complete for: {sortedTurnTakers[currentTurn].GetType().Name}");
        
        // Move to next turn
        currentTurn++;
        
        // Check if round is complete
        if (currentTurn >= sortedTurnTakers.Count)
        {
            EndRound();
        }
        else
        {
            // Start next turn taker
            Debug.Log($"Next turn number: {currentTurn}");
            StartCoroutine("DelayedTick");
        }
    }

    IEnumerator DelayedTick()
    {
        yield return new WaitForSeconds(1f);
        sortedTurnTakers[currentTurn].Tick();

    }

    void EndRound()
    {
        roundInProgress = false;
        Debug.Log("Round complete!");
        
        // Notify GameStateManager that round is complete
        onRoundComplete?.Invoke();
    }

    // Public method to manually trigger next turn (for debugging or special cases)
    public void ForceNextTurn()
    {
        OnTurnComplete();
    }

    // Get current turn information
    public ITurnTaker GetCurrentTurnTaker()
    {
        if (roundInProgress && currentTurn < sortedTurnTakers.Count)
            return sortedTurnTakers[currentTurn];
        return null;
    }

    public int GetCurrentTurnIndex()
    {
        return currentTurn;
    }

    public int GetTotalTurnTakers()
    {
        return sortedTurnTakers?.Count ?? 0;
    }

    public bool IsRoundInProgress()
    {
        return roundInProgress;
    }
}
