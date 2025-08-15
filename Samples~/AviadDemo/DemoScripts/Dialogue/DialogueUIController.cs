using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUIController : MonoBehaviour
{
    DialogueStore dStore;
    [SerializeField] TMP_Text speakerAndLine;
    [SerializeField] GameObject UIRootObject;
    [SerializeField] Sprite[] images;
    [SerializeField] Image profileImage;
    void OnEnable()
    {
        dStore = FindAnyObjectByType<DialogueStore>();
        if (dStore == null)
        {
            Debug.LogError("Dialogue controller has no storage to access. Check reference is present.");
        }
        else
        {
            DialogueStore.NewLineAdded += OnNewLine;
        }
    }
    void OnDisable()
    {
        DialogueStore.NewLineAdded -= OnNewLine;
    }
    void OnNewLine(DialogueLine line)
    {
        if (!UIRootObject.activeInHierarchy)
        {
            UIRootObject.SetActive(true);
        }
        speakerAndLine.text = "";
        speakerAndLine.text = $"{line.Speaker}: {line.Line}";
        if (line.Speaker == "player")
        {
            profileImage.overrideSprite = images[0];
        }
        else if (line.Speaker.ToLower().Contains("fan"))
        {
            profileImage.overrideSprite= images[5];
        }
        else if (line.Speaker.ToLower().Contains("bard"))
        {
            profileImage.overrideSprite = images[1];
        }
        else if (line.Speaker.ToLower().Contains("gobbin"))
        {
            profileImage.overrideSprite = images[2];
        }
        else if (line.Speaker.ToLower().Contains("groovy"))
        {
            profileImage.overrideSprite = images[3];
        }
        else
        {
            profileImage.overrideSprite = images[4];
        }
            return;
    }
    public void HideUI()
    {
        UIRootObject.SetActive(false);
        speakerAndLine.text = "";
    }
}
