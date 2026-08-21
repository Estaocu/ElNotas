using System;
using System.Collections.Generic;
using CarterGames.Assets.SaveManager;
using UnityEngine;

namespace Save
{
    [Serializable]
    public struct NpcTalkEntry
    {
        public string npcName;
        public int timesSpoken;

        public NpcTalkEntry(string npcName, int timesSpoken)
        {
            this.npcName = npcName;
            this.timesSpoken = timesSpoken;
        }
    }

    public class NpcsSaveObject : SaveObject
    {
        [SerializeField] 
        private SaveValue<List<NpcTalkEntry>> npcsTalkedTo = new SaveValue<List<NpcTalkEntry>>("npcs_talked_to", new List<NpcTalkEntry>());
        
        public SaveValue<List<NpcTalkEntry>> NpcsTalkedTo => npcsTalkedTo;

        // --- Métodos Helper para emular la funcionalidad de un Diccionario ---

        public bool ContainsKey(string name)
        {
            if (npcsTalkedTo.Value == null) return false;
            return npcsTalkedTo.Value.Exists(x => x.npcName == name);
        }

        public int GetTimesSpoken(string name)
        {
            if (npcsTalkedTo.Value == null) return 0;
            var entry = npcsTalkedTo.Value.Find(x => x.npcName == name);
            return entry.timesSpoken;
        }

        public void SetTimesSpoken(string name, int times)
        {
            if (npcsTalkedTo.Value == null) 
                npcsTalkedTo.Value = new List<NpcTalkEntry>();

            int index = npcsTalkedTo.Value.FindIndex(x => x.npcName == name);
            if (index >= 0)
            {
                npcsTalkedTo.Value[index] = new NpcTalkEntry(name, times);
            }
            else
            {
                npcsTalkedTo.Value.Add(new NpcTalkEntry(name, times));
            }
        }
    }
}