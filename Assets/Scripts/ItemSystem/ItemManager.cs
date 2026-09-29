using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    private ItemListUI _itemListUI;
    private List<ItemBase> _playerItems;
    private ItemBase _weapon;
    private WeaponUI _weaponUI;

    private void OnEnable()
    {
        ItemBase.OnAddItem += HandleItemAdded;
    }

    private void Start()
    {
        _playerItems = new List<ItemBase>();
        _itemListUI = GetComponent<ItemListUI>();
        _weaponUI = GetComponent<WeaponUI>();
        if (_itemListUI == null) Debug.LogError("Error Loading Item List UI!");
    }

    private void HandleItemAdded(ItemBase item)
    {
        if (item is WeaponBase weapon && item.ItemData is WeaponSO weaponData)
        {
            _weapon = item;
            weapon.EquipWeapon(weaponData);
            _weaponUI.UpdateEquipSprite(weapon.CurrentSprite);
            _itemListUI.ChangeEquippedWeapon(weapon.ItemData.InventoryIcon);
        }
        else
        {
            _playerItems.Add(item);
            _itemListUI.AddItemToList(item.ItemData.InventoryIcon);
        }
    }

    private void OnDisable()
    {
        ItemBase.OnAddItem -= HandleItemAdded;
    }
}
