using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Aviad {
    public class AIDialogue : CharacterComponent
{
    private bool init = true;
    private string assistantOutput = "";
    private string partialOutput = "";
    private bool needsUIUpdate = false;

    [Header("Character bio")]
    private string _charName = "Gobbin the Goblin";
    private string _charDesc = "Fantasy character.";
    [SerializeField] private string initMessage = "You are an audience member at ye olde tavern listening to the performance. Respond to the performance using your character description. You are described by the following:";

    [Header("Aviad Required Systems")]
    [SerializeField] private Runner aviadRunner;

    [Header("Data output")]
    // Exposed events to allow connecting data to other systems
    [SerializeField] UnityEvent<string> OnToken;
    [SerializeField] UnityEvent<string> OnGenerationComplete;
    string characterDialogueText;

    Action<bool> onCompleteCallback = null;

    private async void OnEnable()
    {
        SelfPopulateSerializedFields();
        try
        {
            await WaitForRunner();
        }
        catch (System.Exception ex)
        {
            PackageLogger.Error($"Runner failed to initialize. {ex.Message}");
        }
    }

    private void SelfPopulateSerializedFields()
    {
        if (aviadRunner == null)
        {
            aviadRunner = FindFirstObjectByType<Runner>();
            if (aviadRunner == null)
            {
                PackageLogger.Warning($"{nameof(aviadRunner)} field not set in inspector.");
            }
        }
    }

    private void Update()
    {
        if (needsUIUpdate)
        {
            characterDialogueText = partialOutput;
            needsUIUpdate = false;
        }
    }

    public override void SetCharacterDetails(string[] details)
    {
        _charName = details[0];
        _charDesc = details[1];
    }
    private async Task WaitForRunner()
    {
        float timeout = 10f;
        float elapsed = 0f;
        while (!aviadRunner.IsAvailable && elapsed < timeout)
        {
            await Task.Delay(100);
            elapsed += 0.1f;
        }
    }

    public override void TriggerTask(Action<bool> onComplete)
    {
        onCompleteCallback = onCompleteCallback == null ? onCompleteCallback = onComplete : onCompleteCallback;
        Respond(UpdateAssistantResponse, GenerationComplete);
    }

    public void Respond(Action<string> onUpdateCallback, Action<bool> onComplete)
    {
        if (init)
        {
            aviadRunner.AddTurnToContext("user", $"{initMessage} {_charDesc}. Introduce thyself as {_charName} to the taverne. *IN 10 WORDES OR LESSE*");
            init = false;
        }
        else
            aviadRunner.AddTurnToContext("user", $"{_charName} sayeth/thinketh: *IN 10 WORDES OR LESSE*");

        aviadRunner.Generate(onUpdateCallback, onComplete);
    }

    private void UpdateAssistantResponse(string partial)
    {
        partialOutput += partial;
        needsUIUpdate = true;
        OnToken?.Invoke(partialOutput);
    }

    private void GenerationComplete(bool success)
    {
        if (success)
        {
            Debug.Log(partialOutput);
            assistantOutput = partialOutput;
            OnGenerationComplete?.Invoke(new string(assistantOutput));
            needsUIUpdate = false;
            partialOutput = "";
        }
        else
        {
            assistantOutput = "Generation failed.";
            Debug.LogError(assistantOutput);
        }

        onCompleteCallback?.Invoke(success);
    }

    private void ResetConversation()
    {
        aviadRunner.Reset();
    }
}


}