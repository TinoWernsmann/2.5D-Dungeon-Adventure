using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Speichern, Laden und Löschen von Spielständen (feste Anzahl an Slots).
///
/// Erzeugt sich beim Spielstart automatisch und bleibt über Szenenwechsel bestehen –
/// er muss in keine Szene gelegt werden.
///
/// Speichern:  sammelt den Zustand aller <see cref="SaveableEntity"/>-Objekte, zerstörte Objekte
///             und zur Laufzeit entstandene Items (Loot, gedroppte Items).
/// Laden:      lädt die gespeicherte Szene neu, wartet bis alle Start()-Methoden gelaufen sind,
///             und stellt dann den gespeicherten Zustand her.
/// </summary>
public class SaveManager : MonoBehaviour
{
    public const int SlotCount = 3;

    private readonly HashSet<string> destroyedEntityIds = new();

    private SaveGameData pendingLoad;
    private float playTimeSeconds;
    private bool isGameplayScene;

    public static SaveManager Instance { get; private set; }

    public static bool IsQuitting { get; private set; }

    /// <summary>Ein Spielstand wird gerade geladen bzw. wiederhergestellt.</summary>
    public bool IsLoadingGame => pendingLoad != null;

    /// <summary>Das zuletzt gespielte Level (z.B. für "Restart Level" im Todes-Bildschirm).</summary>
    public string LastGameplayScene { get; private set; }

    /// <summary>Ein Slot wurde gespeichert oder gelöscht (für die UI).</summary>
    public event Action SlotsChanged;

    /// <summary>Ein Spielstand wurde vollständig geladen.</summary>
    public event Action GameLoaded;

    /// <summary>Speichern ist nur im laufenden Spiel möglich (nicht im Menü, nicht wenn tot).</summary>
    public bool CanSave
    {
        get
        {
            if (!isGameplayScene || pendingLoad != null)
            {
                return false;
            }

            PlayerMovement player = FindAnyObjectByType<PlayerMovement>();

            return player != null &&
                   player.TryGetComponent(out Health playerHealth) &&
                   !playerHealth.IsDead;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateInstance()
    {
        IsQuitting = false;

        GameObject managerObject = new("SaveManager");
        managerObject.AddComponent<SaveManager>();
        DontDestroyOnLoad(managerObject);
    }

    public static void NotifyEntityDestroyed(string entityId)
    {
        if (Instance != null && !string.IsNullOrEmpty(entityId))
        {
            Instance.destroyedEntityIds.Add(entityId);
        }
    }

    #region Unity

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void OnApplicationQuit()
    {
        IsQuitting = true;
    }

    private void Update()
    {
        if (isGameplayScene)
        {
            playTimeSeconds += Time.deltaTime;
        }
    }

    #endregion

    #region Slots

    public bool HasSave(int slot)
    {
        return SaveFileStorage.Exists(slot);
    }

    public bool HasAnySave()
    {
        return GetMostRecentSlot() >= 0;
    }

    /// <summary>Liest die Infos eines Slots (Datum, Spielzeit, Quest) für die Anzeige.</summary>
    public bool TryGetSlotInfo(int slot, out SaveGameData info)
    {
        return SaveFileStorage.TryRead(slot, out info);
    }

    /// <summary>Der zuletzt gespeicherte Slot, oder -1 wenn es keinen gibt.</summary>
    public int GetMostRecentSlot()
    {
        int mostRecentSlot = -1;
        DateTime mostRecentDate = DateTime.MinValue;

        for (int slot = 0; slot < SlotCount; slot++)
        {
            if (TryGetSlotInfo(slot, out SaveGameData info) && info.SavedAt > mostRecentDate)
            {
                mostRecentDate = info.SavedAt;
                mostRecentSlot = slot;
            }
        }

        return mostRecentSlot;
    }

    public void Delete(int slot)
    {
        SaveFileStorage.Delete(slot);
        SlotsChanged?.Invoke();
    }

    #endregion

    #region Save

    public bool Save(int slot)
    {
        if (!CanSave)
        {
            Debug.LogWarning("[Save] Speichern ist gerade nicht möglich.");
            return false;
        }

        SaveGameData data = CaptureGame();

        try
        {
            SaveFileStorage.Write(slot, data);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[Save] Slot {slot} konnte nicht gespeichert werden: {exception.Message}");
            return false;
        }

        Debug.Log($"[Save] Slot {slot + 1} gespeichert ({data.entities.Count} Objekte).");
        SlotsChanged?.Invoke();

        return true;
    }

    private SaveGameData CaptureGame()
    {
        SaveGameData data = new()
        {
            sceneName = SceneManager.GetActiveScene().name,
            playerName = GlobalVars.Instance != null ? GlobalVars.Instance.PlayerName : string.Empty,
            difficulty = DifficultySettings.Current,
            questTitle = GetTrackedQuestTitle(),
            savedAtIso = DateTime.Now.ToString("o"),
            playTimeSeconds = playTimeSeconds,
            destroyedEntityIds = new List<string>(destroyedEntityIds),
            droppedItems = CaptureDroppedItems()
        };

        foreach (SaveableEntity entity in FindAllEntities())
        {
            if (entity.HasId)
            {
                data.entities.Add(entity.Capture());
            }
        }

        return data;
    }

    private static string GetTrackedQuestTitle()
    {
        QuestProgress tracked = QuestManager.Instance != null ? QuestManager.Instance.Tracked : null;

        if (tracked == null)
        {
            return string.Empty;
        }

        return string.IsNullOrEmpty(tracked.Data.Title) ? tracked.GoalText : tracked.Data.Title;
    }

    /// <summary>Items, die nicht zur Szene gehören (Truhen-Loot, gedroppte Kopien) und gerade herumliegen.</summary>
    private static List<DroppedItemSaveData> CaptureDroppedItems()
    {
        List<DroppedItemSaveData> droppedItems = new();
        SaveRegistry registry = SaveRegistry.Instance;

        if (registry == null)
        {
            return droppedItems;
        }

        foreach (ItemBase item in FindObjectsByType<ItemBase>(FindObjectsInactive.Exclude))
        {
            bool isSceneItem = item.TryGetComponent(out SaveableEntity entity) && entity.HasId;
            string itemId = registry.GetItemId(item.ItemData);

            if (isSceneItem || string.IsNullOrEmpty(itemId))
            {
                continue;
            }

            droppedItems.Add(new DroppedItemSaveData
            {
                itemId = itemId,
                amount = item.PickupAmount,
                position = item.transform.position
            });
        }

        return droppedItems;
    }

    #endregion

    #region Load

    public bool Load(int slot)
    {
        if (!SaveFileStorage.TryRead(slot, out SaveGameData data))
        {
            Debug.LogWarning($"[Save] Slot {slot + 1} ist leer oder beschädigt.");
            return false;
        }

        if (data.version > SaveGameData.CurrentVersion)
        {
            Debug.LogWarning($"[Save] Slot {slot + 1} stammt aus einer neueren Spielversion.");
            return false;
        }

        pendingLoad = data;

        SceneLoader.Load(data.sceneName);

        return true;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isGameplayScene = FindAnyObjectByType<PlayerMovement>() != null;

        if (isGameplayScene)
        {
            LastGameplayScene = scene.name;
        }

        if (pendingLoad != null)
        {
            // Zwischendurch wird der Ladebildschirm geladen – erst in der Zielszene wiederherstellen.
            if (scene.name == pendingLoad.sceneName)
            {
                StartCoroutine(RestoreGame(pendingLoad));
            }

            return;
        }

        // Neues Spiel (oder Neustart nach dem Tod): Welt ist frisch.
        destroyedEntityIds.Clear();
        playTimeSeconds = 0f;
    }

    private IEnumerator RestoreGame(SaveGameData data)
    {
        // Warten, bis alle Start()-Methoden der Szene gelaufen sind,
        // damit sie den geladenen Zustand nicht wieder überschreiben.
        yield return null;

        destroyedEntityIds.Clear();
        destroyedEntityIds.UnionWith(data.destroyedEntityIds);
        playTimeSeconds = data.playTimeSeconds;

        if (GlobalVars.Instance != null && !string.IsNullOrEmpty(data.playerName))
        {
            GlobalVars.Instance.SetPlayerName(data.playerName);
        }

        DifficultySettings.Current = data.difficulty;

        Dictionary<string, SaveableEntity> entitiesById = MapEntitiesById();

        DestroyRemovedEntities(entitiesById);

        // Zerstören passiert erst am Ende des Frames – danach sind die Objekte wirklich weg.
        yield return null;

        RestoreEntities(data.entities, entitiesById);
        RestoreDroppedItems(data.droppedItems);
        NotifyLoadListeners();

        pendingLoad = null;

        Debug.Log($"[Save] Spielstand geladen ({data.entities.Count} Objekte).");
        GameLoaded?.Invoke();
    }

    private void DestroyRemovedEntities(Dictionary<string, SaveableEntity> entitiesById)
    {
        foreach (string destroyedId in destroyedEntityIds)
        {
            if (entitiesById.TryGetValue(destroyedId, out SaveableEntity entity))
            {
                entitiesById.Remove(destroyedId);
                Destroy(entity.gameObject);
            }
        }
    }

    private static void RestoreEntities(
        List<EntitySaveData> savedEntities,
        Dictionary<string, SaveableEntity> entitiesById)
    {
        foreach (EntitySaveData savedEntity in savedEntities)
        {
            if (entitiesById.TryGetValue(savedEntity.id, out SaveableEntity entity) && entity != null)
            {
                entity.Restore(savedEntity);
            }
        }
    }

    private static void RestoreDroppedItems(List<DroppedItemSaveData> droppedItems)
    {
        SaveRegistry registry = SaveRegistry.Instance;

        if (registry == null)
        {
            return;
        }

        foreach (DroppedItemSaveData droppedItem in droppedItems)
        {
            if (!registry.TryGetItem(droppedItem.itemId, out _, out ItemBase prefab) || prefab == null)
            {
                continue;
            }

            ItemBase item = Instantiate(prefab, droppedItem.position, Quaternion.identity);
            item.SetPickupAmount(droppedItem.amount);
        }
    }

    private static void NotifyLoadListeners()
    {
        MonoBehaviour[] behaviours =
            FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour is ISaveLoadListener listener)
            {
                listener.OnGameLoaded();
            }
        }
    }

    #endregion

    private static SaveableEntity[] FindAllEntities()
    {
        return FindObjectsByType<SaveableEntity>(FindObjectsInactive.Include);
    }

    private static Dictionary<string, SaveableEntity> MapEntitiesById()
    {
        Dictionary<string, SaveableEntity> entitiesById = new();

        foreach (SaveableEntity entity in FindAllEntities())
        {
            if (entity.HasId)
            {
                entitiesById.TryAdd(entity.Id, entity);
            }
        }

        return entitiesById;
    }
}
