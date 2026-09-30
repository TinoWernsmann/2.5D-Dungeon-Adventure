using UnityEngine;
using UnityEngine.UI;

public class ItemListUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ItemManager itemManager;

    [Header("Inventory")]
    [SerializeField] private Image[] itemIcons;

    [Header("Selected Item")]
    [SerializeField] private Image selectedItemIcon;

    private readonly Color selectedColor = Color.white;

    private readonly Color unselectedColor =
        new(0.65f, 0.65f, 0.65f, 1f);

    private void OnEnable()
    {
        if (itemManager == null)
        {
            return;
        }

        itemManager.InventoryChanged += Refresh;
        itemManager.SelectedItemChanged += HandleSelectedItemChanged;
    }

    private void Start()
    {
        Refresh();
    }

    private void OnDisable()
    {
        if (itemManager == null)
        {
            return;
        }

        itemManager.InventoryChanged -= Refresh;
        itemManager.SelectedItemChanged -= HandleSelectedItemChanged;
    }

    private void Refresh()
    {
        if (itemManager == null)
        {
            return;
        }

        RefreshInventorySlots();
        RefreshSelectedItem();
    }

    private void RefreshInventorySlots()
    {
        for (int index = 0; index < itemIcons.Length; index++)
        {
            ItemSO item = itemManager.GetItemAt(index);

            bool hasItem = item != null;

            itemIcons[index].sprite =
                hasItem ? item.InventoryIcon : null;

            itemIcons[index].gameObject.SetActive(hasItem);

            itemIcons[index].color =
                index == itemManager.SelectedSlot
                    ? selectedColor
                    : unselectedColor;
        }
    }

    private void RefreshSelectedItem()
    {
        SetSelectedItemIcon(itemManager.SelectedItem);
    }

    private void HandleSelectedItemChanged(ItemSO item)
    {
        SetSelectedItemIcon(item);
    }

    private void SetSelectedItemIcon(ItemSO item)
    {
        if (selectedItemIcon == null)
        {
            return;
        }

        Sprite icon = item != null
            ? item.InventoryIcon
            : null;

        selectedItemIcon.sprite = icon;
        selectedItemIcon.gameObject.SetActive(icon != null);
    }
}