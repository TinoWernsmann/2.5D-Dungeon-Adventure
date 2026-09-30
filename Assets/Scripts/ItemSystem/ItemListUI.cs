using UnityEngine;
using UnityEngine.UI;

public class ItemListUI : MonoBehaviour
{
    [SerializeField] private Image[] _itemIcons;
    [SerializeField] private Image _weaponIcon;

    private void Start()
    {
        UpdateItemListIcons();
        UpdateWeaponIcon();
    }

    public void AddItemToList(Sprite itemSprite)
    {
        foreach (Image icon in _itemIcons)
        {
            if (icon.sprite != null) continue;
            icon.sprite = itemSprite;
            break;
        }

        UpdateItemListIcons();
    }

    public void ChangeEquippedWeapon(Sprite weaponSprite)
    {
        _weaponIcon.sprite = weaponSprite;
        UpdateWeaponIcon();
    }

    public void DeleteItemFromList(Sprite itemSprite)
    {
        foreach (Image icon in _itemIcons)
        {
            if (icon.sprite == null) continue;
            if (icon.sprite == itemSprite)
            {
                icon.sprite = null;
                break;
            }
        }

        UpdateItemListIcons();
    }

    private void UpdateItemListIcons()
    {
        foreach (Image icon in _itemIcons)
        {
            icon.gameObject.SetActive(icon.sprite != null);
        }
    }

    private void UpdateWeaponIcon()
    {
        _weaponIcon.gameObject.SetActive(_weaponIcon.sprite != null);
    }
}
