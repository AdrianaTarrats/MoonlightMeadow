using UnityEngine;
using Unity.Cinemachine;
using System.IO;
using System.Linq;
using System.Collections.Generic;


/// <summary>
/// Orchestrates full game save and load using <see cref="JsonUtility"/>.
/// On startup it fades in the screen after all subsystems have been restored from disk.
/// </summary>
public class SaveController : MonoBehaviour
{
    private string saveLocation;
    private InventoryController inventoryController;
    private HotbarController hotbarController;
    private Shop[] shops;
    private Chest[] chests;
    private Reparable[] reparables;

    /// <summary>
    /// Singleton instance of the SaveController.
    /// </summary>
    public static SaveController Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        InitializeComponents();
        if (ScreenFader.Instance != null)
            ScreenFader.Instance.SetOpaqueInstant();
        _ = LoadAndReveal();
    }

    /// <summary>
    /// Loads the game and then fades the screen in once all subsystems are ready.
    /// Waits one frame before and after loading so Cinemachine and other components can settle.
    /// </summary>
    private async Awaitable LoadAndReveal()
    {
        await Awaitable.NextFrameAsync(); // wait for all Start() to run
        LoadGame();
        InitializeZoneMusic();
        await Awaitable.NextFrameAsync(); // let Cinemachine process new position/confiner
        if (ScreenFader.Instance != null)
            await ScreenFader.Instance.FadeIn();

        if (GameController.GameCompleted)
            CreditsController.Instance?.ShowCredits();
    }

    private void InitializeZoneMusic()
    {
        foreach (MusicZone zone in FindObjectsByType<MusicZone>(FindObjectsSortMode.None))
        {
            if (zone.ContainsPlayer())
            {
                zone.Play();
                return;
            }
        }
    }

    private void InitializeComponents()
    {
        saveLocation = Path.Combine(Application.persistentDataPath, "saveData.json");
        inventoryController = FindFirstObjectByType<InventoryController>();
        hotbarController = FindFirstObjectByType<HotbarController>();
        shops = FindObjectsByType<Shop>(FindObjectsSortMode.None);
        chests = FindObjectsByType<Chest>(FindObjectsSortMode.None);
        reparables = FindObjectsByType<Reparable>(FindObjectsSortMode.None);
    }

    /// <summary>
    /// Returns true if a save file exists at the persistent data path.
    /// </summary>
    public static bool HasSave()
    {
        return File.Exists(Path.Combine(Application.persistentDataPath, "saveData.json"));
    }

    /// <summary>
    /// Collects state from all subsystems and writes it to the JSON save file.
    /// </summary>
    public void SaveGame()
    {
        SaveData saveData = new SaveData
        {
            playerPosition = GameObject.FindWithTag("Player").transform.position,
            mapBoundary = FindFirstObjectByType<CinemachineConfiner2D>().BoundingShape2D.gameObject.name,
            inventorySaveData = inventoryController.GetInventoryItems(),
            hotbarSaveData = hotbarController.GetHotbarItems(),
            selectedHotbarSlot = hotbarController.GetSelectedSlotIndex(),
  
            playerGold = CurrencyController.Instance.GetGold(),
            playerHealth = PlayerState.Instance.GetHealth(),
            playerEnergy = PlayerState.Instance.GetEnergy(),
            playerState = (int)PlayerState.Instance.CurrentState,
            reparableStates = GetReparableStates(),
            shopStates = GetShopStates(),
            chestStates = GetChestStates(),
            tileStates = TileController.Instance.GetTileStates(),
            cropStates = CropController.Instance.GetCropStates(),
            dateTime = TimeController.Instance.GetCurrentDateTime(),
            questProgressData = GetQuestProgressSaveData(),
            completedQuestIDs = QuestController.Instance.completedQuestIDs,
            recipesCrafted = RecipeBook.Instance.GetCraftedRecipeData(),
            unlockedRecipeIds = RecipeBook.Instance.GetUnlockedRecipeIds(),
            spawnStates = SpawnController.Instance.GetSpawnStates(),
            storyBeatIndex = StoryController.Instance != null ? StoryController.Instance.GetCurrentBeatIndex() : 0,
            gameCompleted = GameController.GameCompleted,
            npcDialogueStates = DialogueSequenceController.Instance != null
                ? DialogueSequenceController.Instance.GetDialogueSequenceStates()
                : new List<SaveData.NPCDialogueStateSaveData>()
        };

        //File.WriteAllText(saveLocation, JsonUtility.ToJson(saveData));
        File.WriteAllText(saveLocation, JsonUtility.ToJson(saveData, true));

    }

    /// <summary>
    /// Loads the save file and restores all subsystems. If no save exists or
    /// <see cref="GameController.IsNewGame"/> is set, starts a fresh game instead.
    /// </summary>
    public void LoadGame()
    {
        if (!GameController.IsNewGame && File.Exists(saveLocation))
        {
            SaveData saveData = JsonUtility.FromJson<SaveData>(File.ReadAllText(saveLocation));

            // Block forced sleep from firing when SetDateTime fires OnDataTimeChanged with
            // the saved time (which can be 2-3 AM). Without this, TriggerForcedSleep starts
            // asynchronously mid-load, causing a rogue fade/teleport and corrupting the game state.
            if (SleepController.Instance != null)
                SleepController.Instance.MarkAsSlept();

            GameObject.FindWithTag("Player").transform.position = saveData.playerPosition;

            // Guard against null — if the boundary object is inactive or renamed,
            // GameObject.Find returns null and would otherwise throw here, aborting the
            // rest of the load silently in builds.
            if (!string.IsNullOrEmpty(saveData.mapBoundary))
            {
                GameObject boundaryObj = GameObject.Find(saveData.mapBoundary);
                CinemachineConfiner2D confiner = FindFirstObjectByType<CinemachineConfiner2D>();
                if (confiner != null && boundaryObj != null)
                    confiner.BoundingShape2D = boundaryObj.GetComponent<PolygonCollider2D>();
            }

            inventoryController.SetInventoryItems(saveData.inventorySaveData);
            hotbarController.SetHotbarItems(saveData.hotbarSaveData, saveData.selectedHotbarSlot);

            LoadShopStates(saveData.shopStates);
            LoadChestStates(saveData.chestStates);
            CurrencyController.Instance.SetGold(saveData.playerGold);
            PlayerState.Instance.SetHealth(saveData.playerHealth);
            PlayerState.Instance.SetEnergy(saveData.playerEnergy);
            PlayerState.Instance.LoadState((State)saveData.playerState);
            LoadReparableStates(saveData.reparableStates);
            TileController.Instance.LoadTileStates(saveData.tileStates);
            CropController.Instance.LoadCropStates(saveData.cropStates);
            TimeController.Instance.SetDateTime(saveData.dateTime);
            TimeController.Instance.SyncTimeEvents();
            QuestController.Instance.LoadQuestProgress(saveData.questProgressData ?? new());
            QuestController.Instance.completedQuestIDs = saveData.completedQuestIDs;
            RecipeBook.Instance.LoadUnlockedRecipeIds(saveData.unlockedRecipeIds);
            RecipeBook.Instance.LoadCraftedRecipeData(saveData.recipesCrafted);
            SpawnController.Instance.LoadSpawnStates(saveData.spawnStates);
            StoryController.Instance?.LoadStoryProgress(saveData.storyBeatIndex);
            if (DialogueSequenceController.Instance != null)
                DialogueSequenceController.Instance.LoadDialogueSequenceStates(saveData.npcDialogueStates);

            if (saveData.gameCompleted)
                GameController.GameCompleted = true;
        }
        else
        {
            GameController.IsNewGame = false;
            GameController.GameCompleted = false;
            inventoryController.SetInventoryItems(new System.Collections.Generic.List<InventorySaveData>());
            hotbarController.SetHotbarItems(new System.Collections.Generic.List<InventorySaveData>());
            RecipeBook.Instance?.ResetAllCraftCounts();
            StoryController.Instance?.InitializeNewGame();
            SaveGame();
        }
    }

    private List<SaveData.QuestProgressSaveData> GetQuestProgressSaveData()
    {
        var result = new List<SaveData.QuestProgressSaveData>();
        foreach (QuestProgress qp in QuestController.Instance.activeQuests)
        {
            var data = new SaveData.QuestProgressSaveData
            {
                questID = qp.quest.questID,
                objectives = new List<SaveData.ObjectiveProgressSaveData>()
            };
            foreach (QuestObjective obj in qp.objectives)
            {
                data.objectives.Add(new SaveData.ObjectiveProgressSaveData
                {
                    objectiveID = obj.objectiveID,
                    currentAmount = obj.currentAmount
                });
            }
            result.Add(data);
        }
        return result;
    }

    private List<SaveData.ShopInstanceData> GetShopStates()
    {
        List<SaveData.ShopInstanceData> shopStates = new List<SaveData.ShopInstanceData>();

        foreach (var shop in shops)
        {
            SaveData.ShopInstanceData shopData = new SaveData.ShopInstanceData
            {
                shopID = shop.shopID,
                currentStock = new List<SaveData.ShopItemData>()
            };

            foreach (var stockItem in shop.GetShopStock())
            {
                shopData.currentStock.Add(new SaveData.ShopItemData
                {
                    itemID = stockItem.itemID,
                    quantity = stockItem.quantity,
                    catalogItem = stockItem.catalogItem
                });
            }

            shopStates.Add(shopData);
        }

        return shopStates;
    }

    private void LoadShopStates(List<SaveData.ShopInstanceData> shopStates)
    {
        if(shopStates == null) return;

        foreach (var shop in shops)
        {
            SaveData.ShopInstanceData shopData = shopStates.FirstOrDefault(s => s.shopID == shop.shopID);

            if(shopData != null)
            {
                List<Shop.ShopStockItem> loadedStock = new List<Shop.ShopStockItem>();

                foreach (var itemData in shopData.currentStock)
                {
                    loadedStock.Add(new Shop.ShopStockItem
                    {
                        itemID = itemData.itemID,
                        quantity = itemData.quantity,
                        catalogItem = itemData.catalogItem
                    });
                }
                shop.SetStock(loadedStock);
            }
        }
    }

    private List<SaveData.ChestStorageData> GetChestStates()
    {
        List<SaveData.ChestStorageData> chestStates = new List<SaveData.ChestStorageData>();

        foreach (var chest in chests)
        {
            SaveData.ChestStorageData chestData = new SaveData.ChestStorageData
            {
                chestID = chest.chestID,
                items = new List<SaveData.ChestStorageItemData>()
            };

            var storageItems = chest.GetStorageItems();
            foreach (var kvp in storageItems)
            {
                chestData.items.Add(new SaveData.ChestStorageItemData
                {
                    itemID = kvp.Key,
                    quantity = kvp.Value
                });
            }

            chestStates.Add(chestData);
        }

        return chestStates;
    }

    private void LoadChestStates(List<SaveData.ChestStorageData> chestStates)
    {
        if (chestStates == null) return;

        foreach (var chest in chests)
        {
            SaveData.ChestStorageData chestData = chestStates.FirstOrDefault(c => c.chestID == chest.chestID);

            if (chestData != null)
            {
                Dictionary<int, int> loadedItems = new Dictionary<int, int>();

                foreach (var itemData in chestData.items)
                {
                    loadedItems[itemData.itemID] = itemData.quantity;
                }

                chest.SetStorageData(loadedItems);
            }
        }
    }

    private List<SaveData.ReparableSaveData> GetReparableStates()
    {
        var result = new List<SaveData.ReparableSaveData>();
        foreach (var r in reparables)
        {
            if (string.IsNullOrEmpty(r.ReparableID)) continue;
            result.Add(new SaveData.ReparableSaveData
            {
                reparableID = r.ReparableID,
                isRepaired = r.IsRepaired,
                isUnlocked = r.IsRepairUnlocked
            });
        }
        return result;
    }

    private void LoadReparableStates(List<SaveData.ReparableSaveData> states)
    {
        if (states == null) return;
        foreach (var r in reparables)
        {
            if (string.IsNullOrEmpty(r.ReparableID)) continue;
            SaveData.ReparableSaveData data = states.FirstOrDefault(s => s.reparableID == r.ReparableID);
            if (data != null)
                r.LoadRepairState(data.isRepaired, data.isUnlocked);
        }
    }
}
