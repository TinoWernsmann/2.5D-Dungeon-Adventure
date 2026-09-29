using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    private ItemListUI _itemListUI;
    private List<ItemBase> _playerItems;
    private WeaponSO _weapon;
    private WeaponUI _weaponUI;
    private WeaponSoundManager _weaponSound;

    private void OnEnable()
    {
        ItemBase.OnAddItem += HandleItemAdded;
    }

    private void Start()
    {
        _playerItems = new List<ItemBase>();
        _itemListUI = GetComponent<ItemListUI>();
        _weaponUI = GetComponent<WeaponUI>();
        _weaponSound = GetComponent<WeaponSoundManager>();
        if (_itemListUI == null) Debug.LogError("Error Loading Item List UI!");
        if (_weaponSound == null) Debug.LogError("Error Loading Weapon Sound!");
    }

    public void UseWeapon()
    {
        if (_weapon == null)
        {
            Debug.Log("No Weapon!");
            return;
        }
        _weaponUI.Attack(_weapon, _weaponSound);
    }

    private void HandleItemAdded(ItemBase item)
    {
        if (item.ItemData is WeaponSO weapon)
        {
            _weapon = weapon;
            _weaponUI.UpdateEquipSprite(weapon.IdleSprite);
            _itemListUI.ChangeEquippedWeapon(weapon.InventoryIcon);
            _weaponSound.GetSoundEffects(weapon.AttackSounds);
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
