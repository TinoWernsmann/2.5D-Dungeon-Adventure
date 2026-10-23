using UnityEngine;

public class ItemCollector : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private ItemManager itemManager;

    private void Awake()
    {
        if (itemManager == null)
        {
            itemManager =
                FindAnyObjectByType<
                    ItemManager>();
        }
    }

    public bool TryCollect(
        ItemBase item)
    {
        if (item == null ||
            itemManager == null)
        {
            return false;
        }

        bool collected = itemManager.TryAddItem(
            item
        );

        if (collected)
        {
            LevelStats.Current?.RecordItemCollected(item);
        }

        return collected;
    }
}