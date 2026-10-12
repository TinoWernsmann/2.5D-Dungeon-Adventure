using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Verzeichnis aller Items und Quests mit fester ID, damit sie im Spielstand
/// als Text gespeichert und beim Laden wiedergefunden werden können.
///
/// Liegt unter Assets/Resources/SaveRegistry.asset und wird per
/// "Tools → Save System → Setup Open Scene" (oder Rechtsklick → Refresh) automatisch befüllt.
/// </summary>
[CreateAssetMenu(fileName = "SaveRegistry", menuName = "Save System/Save Registry")]
public class SaveRegistry : ScriptableObject
{
    public const string ResourcePath = "SaveRegistry";

    [Serializable]
    public class ItemEntry
    {
        public string id;
        public ItemSO item;

        [Tooltip("Prefab, aus dem das Item beim Laden neu erzeugt wird.")]
        public ItemBase prefab;
    }

    [Serializable]
    public class QuestEntry
    {
        public string id;
        public QuestData quest;
    }

    [SerializeField] private List<ItemEntry> items = new();
    [SerializeField] private List<QuestEntry> quests = new();

    private static SaveRegistry instance;

    public static SaveRegistry Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<SaveRegistry>(ResourcePath);

                if (instance == null)
                {
                    Debug.LogError(
                        "[Save] Keine SaveRegistry gefunden. " +
                        "Bitte 'Tools → Save System → Setup Open Scene' ausführen.");
                }
            }

            return instance;
        }
    }

    public string GetItemId(ItemSO item)
    {
        return items.Find(entry => entry.item == item)?.id;
    }

    public bool TryGetItem(string id, out ItemSO item, out ItemBase prefab)
    {
        ItemEntry entry = items.Find(e => e.id == id);

        item = entry?.item;
        prefab = entry?.prefab;

        return item != null;
    }

    public string GetQuestId(QuestData quest)
    {
        return quests.Find(entry => entry.quest == quest)?.id;
    }

    public QuestData GetQuest(string id)
    {
        return quests.Find(entry => entry.id == id)?.quest;
    }

#if UNITY_EDITOR
    /// <summary>Sucht alle Items, Item-Prefabs und Quests im Projekt zusammen.</summary>
    [ContextMenu("Refresh")]
    public void RefreshFromProject()
    {
        Dictionary<ItemSO, ItemBase> prefabsByItem = FindItemPrefabs();

        items.Clear();

        foreach (ItemSO item in FindAssets<ItemSO>())
        {
            prefabsByItem.TryGetValue(item, out ItemBase prefab);

            items.Add(new ItemEntry
            {
                id = GetAssetGuid(item),
                item = item,
                prefab = prefab
            });
        }

        quests.Clear();

        foreach (QuestData quest in FindAssets<QuestData>())
        {
            quests.Add(new QuestEntry
            {
                id = GetAssetGuid(quest),
                quest = quest
            });
        }

        UnityEditor.EditorUtility.SetDirty(this);
    }

    private static Dictionary<ItemSO, ItemBase> FindItemPrefabs()
    {
        Dictionary<ItemSO, ItemBase> result = new();

        foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:Prefab"))
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null ||
                !prefab.TryGetComponent(out ItemBase itemBase) ||
                itemBase.ItemData == null)
            {
                continue;
            }

            result.TryAdd(itemBase.ItemData, itemBase);
        }

        return result;
    }

    private static List<T> FindAssets<T>() where T : UnityEngine.Object
    {
        List<T> result = new();

        foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:" + typeof(T).Name))
        {
            T asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(
                UnityEditor.AssetDatabase.GUIDToAssetPath(guid));

            if (asset != null)
            {
                result.Add(asset);
            }
        }

        return result;
    }

    private static string GetAssetGuid(UnityEngine.Object asset)
    {
        return UnityEditor.AssetDatabase.AssetPathToGUID(
            UnityEditor.AssetDatabase.GetAssetPath(asset));
    }
#endif
}
