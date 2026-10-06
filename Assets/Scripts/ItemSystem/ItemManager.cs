using System;
using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    [Serializable]
    private class InventorySlot
    {
        public ItemBase ItemInstance;
        public int Amount;

        public ItemSO ItemData =>
            ItemInstance != null
                ? ItemInstance.ItemData
                : null;

        public InventorySlot(
            ItemBase itemInstance,
            int amount)
        {
            ItemInstance =
                itemInstance;

            Amount =
                Mathf.Max(1, amount);
        }
    }

    public const int MaxItems = 4;

    [Header("References")]
    [SerializeField] private Transform dropPoint;
    [SerializeField] private AudioSource itemSound;

    private readonly List<InventorySlot>
    inventorySlots = new(MaxItems);

    private Transform playerTransform;
    private int selectedSlot = -1;

    public event Action InventoryChanged;
    public event Action<ItemSO> SelectedItemChanged;
    public event Action OnHealingUsed;

    public int ItemCount =>
        inventorySlots.Count;

    public int SelectedSlot =>
        selectedSlot;

    public ItemSO SelectedItem => GetSelectedItem();

    public WeaponSO SelectedWeapon =>
        SelectedItem as WeaponSO;

    public bool IsFull =>
        inventorySlots.Count >= MaxItems;

    public void ConfigurePlayer(
        Transform player)
    {
        playerTransform = player;
    }

    public bool TryAddItem(
        ItemBase item)
    {
        if (item == null ||
            item.ItemData == null)
        {
            return false;
        }

        ItemSO itemData =
            item.ItemData;

        int amount =
            item.PickupAmount;

        if (item.ItemData.PickUpSound != null)
        {
            itemSound.PlayOneShot(item.ItemData.PickUpSound);
        }
        if (itemData.Stackable)
        {
            return TryAddStackableItem(
                item,
                itemData,
                amount
            );
        }

        return TryAddNonStackableItem(
            item
        );
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool HasItem(
        string itemName)
    {
        if (string.IsNullOrWhiteSpace(
                itemName))
        {
            return false;
        }

        return inventorySlots.Exists(
            slot =>
                slot.ItemData != null &&
                slot.ItemData.ItemName ==
                itemName &&
                slot.Amount > 0
        );
    }

    public bool HasItem(
        ItemSO itemData,
        int requiredAmount = 1)
    {
        if (itemData == null ||
            requiredAmount <= 0)
        {
            return false;
        }

        return GetItemAmount(itemData)
               >= requiredAmount;
    }

    public int GetItemAmount(
        ItemSO itemData)
    {
        if (itemData == null)
        {
            return 0;
        }

        int totalAmount = 0;

        foreach (
            InventorySlot slot
            in inventorySlots)
        {
            if (slot.ItemData != itemData)
            {
                continue;
            }

            totalAmount += slot.Amount;
        }

        return totalAmount;
    }

    public int GetItemAmountAt(
        int slotIndex)
    {
        if (!IsValidSlot(slotIndex))
        {
            return 0;
        }

        return inventorySlots[
            slotIndex
        ].Amount;
    }

    public bool TryConsumeItem(
        ItemSO itemData,
        int amount = 1)
    {
        if (itemData == null ||
            amount <= 0)
        {
            return false;
        }

        if (!HasItem(
                itemData,
                amount))
        {
            return false;
        }

        int remainingToRemove =
            amount;

        for (
            int index =
                inventorySlots.Count - 1;
            index >= 0 &&
            remainingToRemove > 0;
            index--)
        {
            InventorySlot slot =
                inventorySlots[index];

            if (slot.ItemData !=
                itemData)
            {
                continue;
            }

            int removeAmount =
                Mathf.Min(
                    slot.Amount,
                    remainingToRemove
                );

            slot.Amount -=
                removeAmount;

            remainingToRemove -=
                removeAmount;

            if (slot.Amount <= 0)
            {
                RemoveSlot(index);
            }
        }

        NotifyInventoryChanged();
        NotifySelectedItemChanged();

        return true;
    }

    public bool RemoveItemByName(
        string itemName)
    {
        int itemIndex =
            inventorySlots.FindIndex(
                slot =>
                    slot.ItemData != null &&
                    slot.ItemData.ItemName ==
                    itemName
            );

        if (itemIndex < 0)
        {
            return false;
        }

        RemoveSlot(itemIndex);

        NotifyInventoryChanged();
        NotifySelectedItemChanged();

        return true;
    }

    public void SelectSlot(
        int slotIndex)
    {
        if (!IsValidSlot(slotIndex))
        {
            return;
        }

        if (selectedSlot ==
            slotIndex)
        {
            return;
        }

        selectedSlot =
            slotIndex;

        ItemSO item = GetItemAt(selectedSlot);
        if (item.PickUpSound != null)
        {
            itemSound.PlayOneShot(item.PickUpSound);
        }

        NotifyInventoryChanged();
        NotifySelectedItemChanged();
    }

    public void TryUseHealing()
    {
        if (!HasItem("Healing")) return;
        ItemSO item = null;

        OnHealingUsed?.Invoke();
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            item = GetItemAt(i);
            if (item.ItemName == "Healing") break;
        }

        if (item != null)
        {
            TryConsumeItem(item);
        }
    }

    public void DropSelectedItem()
    {
        if (!HasSelectedItem())
        {
            return;
        }

        InventorySlot slot =
            inventorySlots[
                selectedSlot
            ];

        if (slot.ItemData.DropSound != null)
        {
            itemSound.PlayOneShot(slot.ItemData.DropSound);
        }

        ItemBase itemToDrop =
            slot.ItemInstance;

        bool removeSlot =
            slot.Amount <= 1;

        slot.Amount--;

        if (removeSlot)
        {
            inventorySlots.RemoveAt(selectedSlot);
            UpdateSelectedSlotAfterRemoval();
        }

        if (itemToDrop != null)
        {
            ItemBase droppedItem =
                removeSlot
                    ? itemToDrop
                    : Instantiate(itemToDrop);

            droppedItem.SetPickupAmount(1);
            droppedItem.DropAt(GetDropPosition());
        }

        NotifyInventoryChanged();
        NotifySelectedItemChanged();
    }

    public ItemSO GetItemAt(
        int slotIndex)
    {
        if (!IsValidSlot(slotIndex))
        {
            return null;
        }

        return inventorySlots[
            slotIndex
        ].ItemData;
    }

    public ItemSO GetSelectedItem()
    {
        return GetItemAt(
            selectedSlot
        );
    }

    public WeaponSO GetSelectedWeapon()
    {
        return GetSelectedItem()
            as WeaponSO;
    }

    private bool TryAddStackableItem(
        ItemBase item,
        ItemSO itemData,
        int amount)
    {
        InventorySlot existingSlot =
            FindStackableSlot(itemData);

        if (existingSlot != null)
        {
            int availableSpace =
                itemData.MaxStackSize -
                existingSlot.Amount;

            if (availableSpace <
                amount)
            {
                return false;
            }

            existingSlot.Amount +=
                amount;

            NotifyInventoryChanged();

            return true;
        }

        if (IsFull)
        {
            return false;
        }

        if (amount >
            itemData.MaxStackSize)
        {
            return false;
        }

        InventorySlot newSlot =
            new InventorySlot(
                item,
                amount
            );

        inventorySlots.Add(
            newSlot
        );

        SelectFirstItemIfNeeded();

        NotifyInventoryChanged();
        NotifySelectedItemChanged();

        return true;
    }

    private bool TryAddNonStackableItem(
        ItemBase item)
    {
        if (IsFull)
        {
            return false;
        }

        InventorySlot newSlot =
            new InventorySlot(
                item,
                1
            );

        inventorySlots.Add(
            newSlot
        );

        SelectFirstItemIfNeeded();

        NotifyInventoryChanged();
        NotifySelectedItemChanged();

        return true;
    }

    private InventorySlot FindStackableSlot(
        ItemSO itemData)
    {
        return inventorySlots.Find(
            slot =>
                slot.ItemData ==
                itemData &&
                slot.Amount <
                itemData.MaxStackSize
        );
    }

    private void RemoveSlot(
        int index)
    {
        if (index < 0 ||
            index >=
            inventorySlots.Count)
        {
            return;
        }

        inventorySlots.RemoveAt(
            index
        );

        if (selectedSlot == index)
        {
            UpdateSelectedSlotAfterRemoval();
        }
        else if (
            selectedSlot > index)
        {
            selectedSlot--;
        }
    }

    private bool HasSelectedItem()
    {
        return IsValidSlot(
            selectedSlot
        );
    }

    private bool IsValidSlot(
        int slotIndex)
    {
        return slotIndex >= 0 &&
               slotIndex <
               inventorySlots.Count;
    }

    private void
        SelectFirstItemIfNeeded()
    {
        if (selectedSlot >= 0)
        {
            return;
        }

        selectedSlot = 0;
    }

    private void
        UpdateSelectedSlotAfterRemoval()
    {
        if (inventorySlots.Count == 0)
        {
            selectedSlot = -1;
            return;
        }

        selectedSlot =
            Mathf.Clamp(
                selectedSlot,
                0,
                inventorySlots.Count - 1
            );
    }

    private void NotifyInventoryChanged()
    {
        InventoryChanged?.Invoke();
    }

    private void NotifySelectedItemChanged()
    {
        SelectedItemChanged?.Invoke(
            SelectedItem
        );
    }

    public void RemoveSelectedItem()
    {
        if (!HasSelectedItem()) return;

        RemoveSlot(selectedSlot);
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
            return
                playerTransform.position +
                playerTransform.forward *
                1.5f +
                Vector3.up * 0.25f;
        }

        return
            transform.position +
            transform.forward * 1.5f +
            Vector3.up * 0.25f;
    }
}