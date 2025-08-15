using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Aviad.Samples
{
    public class DialogueStore : MonoBehaviour
    {

        //event to subscribe
        // variables
        public static event Action<DialogueLine> NewLineAdded;
        private readonly object lockObject = new object();
        private List<DialogueLine> dialogueLines = new List<DialogueLine>();
        [SerializeField] private List<string> lines;
        [SerializeField] private bool debug = false;

        private bool SaveFile(string filepath)
        {
            //Save to disk
            throw new NotImplementedException();
        }

        private bool LoadFile(string filepath)
        {
            //Load dialogue from object
            throw new NotImplementedException();

        }

        // instance of local dialogue data
        // queue to submit dialogue to history
        public DialogueLine[] ReadHistory()
        {
            lock (lockObject)
            {
                return dialogueLines.ToArray();
            }
        }

        public void SubmitLine(DialogueLine line)
        {
            Debug.Log($"Submitting line:{line.Line}");
            lock (lockObject)
            {
                if (dialogueLines != null)
                {
                    dialogueLines.Add(line);
                }
                else
                {
                    dialogueLines = new List<DialogueLine>();
                    dialogueLines.Add(line);
                }
            }

            NewLineAdded?.Invoke(line);

            // This should be an editor script but im being hacky. Note this wont refresh on clicking debug in inspector - Alex 6/26
            if (debug)
            {
                lines = new List<string>();
                foreach (DialogueLine dline in dialogueLines)
                {
                    lines.Add(dline.Line.ToString());
                }
            }
        }

        public void SubmitPlayerLine(String line)
        {
            // maybe change this to get nearest?
            SubmitLine(new DialogueLine(line, "player", new string[] { "everyone" }, DateTime.Now.ToLongTimeString()));
        }

        public void GetLastCharacterLine(string charID)
        {
            throw new NotImplementedException();
        }
    }
}