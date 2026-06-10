using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Root serializable container for all data written to the JSON save file.
/// Holds player state, world state, and progression data for every subsystem.
/// </summary>
[System.Serializable]
public class SaveData
{
    public Vector3 playerPosition;

    public string mapBoundary; // The boundary of the map
    public List<InventorySaveData> inventorySaveData;
    public List<InventorySaveData> hotbarSaveData;
    public int selectedHotbarSlot;
    public int playerGold;
    public int playerHealth;
    public int playerEnergy;
    public int playerState;

    public List<ShopInstanceData> shopStates = new();
    public List<ChestStorageData> chestStates = new();

    public List<TileSaveData> tileStates = new();
    public List<CropSaveData> cropStates = new();

    public List<SpawnSaveData> spawnStates = new();
    public List<ReparableSaveData> reparableStates = new();
    public List<QuestProgressSaveData> questProgressData = new();
    public List<string> completedQuestIDs = new();
    public List<int> unlockedRecipeIds = new List<int>();
    public List<RecipeCraftData> recipesCrafted = new List<RecipeCraftData>();


    // Fecha/Hora del juego
    public DateTime dateTime;

    // Story progression
    public int storyBeatIndex;
    public bool gameCompleted;

    [System.Serializable]
    public class TileSaveData
    {
        public Vector3Int position;
        public string tileType; // e.g., "Tilled", "Watered", "Hidden", etc.
    }

    [System.Serializable]
    public class CropSaveData
    {
        public Vector3Int position;
        public int cropID;
        public int currentStage;
        public int daysInCurrentStage;
        public bool isWateredToday;
    }

    [System.Serializable]
    public class ShopInstanceData
    {
        public string shopID;
        public List<ShopItemData> currentStock;
    }

    [System.Serializable]
    public class ShopItemData
    {
        public int itemID;
        public int quantity;
        public bool catalogItem;
    }

    [System.Serializable]
    public class ChestStorageData
    {
        public string chestID;
        public List<ChestStorageItemData> items;
    }

    [System.Serializable]
    public class ChestStorageItemData
    {
        public int itemID;
        public int quantity;
    }

    [System.Serializable]
    public class SpawnSaveData
    {
        public Vector3 position;
        public string spawnId;
        public string enemyDataId; // optional: name/id of EnemyData ScriptableObject for enemy spawns
    }

    [System.Serializable]
    public class RecipeCraftData
    {
        public int recipeId;
        public int totalTimesCrafted;
    }

    [System.Serializable]
    public class ReparableSaveData
    {
        public string reparableID;
        public bool isRepaired;
        public bool isUnlocked;
    }

    [System.Serializable]
    public class QuestProgressSaveData
    {
        public string questID;
        public List<ObjectiveProgressSaveData> objectives;
    }

    [System.Serializable]
    public class ObjectiveProgressSaveData
    {
        public string objectiveID;
        public int currentAmount;
    }

    // NPC dialogue sequence persistence
    public List<NPCDialogueStateSaveData> npcDialogueStates = new();

    [System.Serializable]
    public class NPCDialogueStateSaveData
    {
        public string npcID;
        public int sequenceIndex;
    }
}
