using System;
using System.Linq;
using UnityEngine;

[Serializable]
public class DialogueLine
{
    public string Line;
    public string Speaker;
    public string[] Audience;
    public string TimeOfUtterance;

    public DialogueLine(string line, string speaker, string[] audience, string timeOfUtterance) {
        this.Line = line;
        this.Speaker = speaker;
        this.Audience = audience?.ToArray();
        this.TimeOfUtterance = timeOfUtterance;
    }
}
