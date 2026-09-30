using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    public const int MaxItems = 4;

    [Header("References")]
    [SerializeField] private Transform dropPoint;

    private readonly List<ItemBase> playerItems = new(MaxItems);

    private Transform playerTransform;
    private int selectedSlot = -1;

    public event Action InventoryChanged;
    public event Action<ItemSO> SelectedItemChanged;

    public int ItemCount => playerItems.Count;

    public int SelectedSlot => selectedSlot;

    public ItemSO SelectedItem => GetSelectedItem();

    public WeaponSO SelectedWeapon => SelectedItem as WeaponSO;

    public bool IsFull => playerItems.Count >= MaxItems;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void ConfigurePlayer(Transform player)
    {
        playerTransform = player;
    }

    public bool TryAddItem(ItemBase item)
    {
        if (!CanAddItem(item))
        {
            return false;
        }

        playerItems.Add(item);

        SelectFirstItemIfNeeded();

        NotifyInventoryChanged();
        NotifySelectedItemChanged();

        return true;
    }

    public void SelectSlot(int slotIndex)
    {
        if (!IsValidSlot(slotIndex))
        {
            return;
        }

        if (selectedSlot == slotIndex)
        {
            return;
        }

        selectedSlot = slotIndex;

        NotifyInventoryChanged();
        NotifySelectedItemChanged();
    }

    public void DropSelectedItem()
    {
        if (!HasSelectedItem())
        {
            return;
        }

        ItemBase itemToDrop = playerItems[selectedSlot];

        playerItems.RemoveAt(selectedSlot);

        UpdateSelectedSlotAfterRemoval();

        itemToDrop.DropAt(GetDropPosition());

        NotifyInventoryChanged();
        NotifySelectedItemChanged();
    }

    public ItemSO GetItemAt(int slotIndex)
    {
        if (!IsValidSlot(slotIndex))
        {
            return null;
        }

        return playerItems[slotIndex].ItemData;
    }

    public ItemSO GetSelectedItem()
    {
        return GetItemAt(selectedSlot);
    }

    public WeaponSO GetSelectedWeapon()
    {
        return GetSelectedItem() as WeaponSO;
    }

    private bool CanAddItem(ItemBase item)
    {
        return item != null &&
               item.ItemData != null &&
               !IsFull;
    }

    private bool HasSelectedItem()
    {
        return IsValidSlot(selectedSlot);
    }

    private bool IsValidSlot(int slotIndex)
    {
        return slotIndex >= 0 &&
               slotIndex < playerItems.Count;
    }

    private void SelectFirstItemIfNeeded()
    {
        if (selectedSlot >= 0)
        {
            return;
        }

        selectedSlot = 0;
    }

    private void UpdateSelectedSlotAfterRemoval()
    {
        if (playerItems.Count == 0)
        {
            selectedSlot = -1;
            return;
        }

        selectedSlot = Mathf.Min(
            selectedSlot,
            playerItems.Count - 1
        );
    }

    private void NotifyInventoryChanged()
    {
        InventoryChanged?.Invoke();
    }

    private void NotifySelectedItemChanged()
    {
        SelectedItemChanged?.Invoke(SelectedItem);
    }

    public void RemoveSelectedItem()
    {
        playerItems.RemoveAt(selectedSlot);
        UpdateSelectedSlotAfterRemoval();
        NotifyInventoryChanged();
        NotifySelectedItemChanged();
    }

    private Vector3 GetDropPosition()
    {
        if (dropPoint != null)
        {
            return dropPoint.position;
        }

        if (playerTransform != null)
        {
            return playerTransform.position +
                   playerTransform.forward * 1.5f +
                   Vector3.up * 0.25f;
        }

        return transform.position +
               transform.forward * 1.5f +
               Vector3.up * 0.25f;
    }
}