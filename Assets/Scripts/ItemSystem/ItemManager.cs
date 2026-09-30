using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    public static ItemManager Instance { get; private set; }

    private ItemListUI _itemListUI;
    private List<ItemBase> _playerItems;
    private WeaponSO _weapon;
    private WeaponUI _weaponUI;
    private WeaponSoundManager _weaponSound;

    public List<ItemBase> PlayerItems => _playerItems;

    private void OnEnable()
    {
        ItemBase.OnAddItem += HandleItemAdded;
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

    public bool HasItem(string itemName)
    {
        foreach (ItemBase item in _playerItems)
        {
            if (item.ItemData.ItemName == itemName) return true;
        }
        return false;
    }

    public void RemoveItemByName(string itemName)
    {
        foreach (ItemBase item in _playerItems)
        {
            if (item.ItemData.ItemName == itemName)
            {
                _playerItems.Remove(item);
                _itemListUI.DeleteItemFromList(item.ItemData.InventoryIcon);
                break;
            }
        }
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
