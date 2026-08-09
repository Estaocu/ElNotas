using System.Collections.Generic;
using CarterGames.Assets.SaveManager;
using UnityEngine;

namespace Save
{
    public class BridgesSaveObject : SaveObject
    {
        [SerializeField] private SaveValue<List<string>> unlockedBridgeSpawners = new SaveValue<List<string>>("unlockedBridgeSpawners", new List<string>());
        
        public SaveValue<List<string>> unlockedBridges => unlockedBridgeSpawners;
    }   
}