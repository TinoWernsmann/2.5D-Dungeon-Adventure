using UnityEngine;
using UnityEngine.UI;

public class WeaponUI : MonoBehaviour
{
    [SerializeField] private Image _equipImage;

    private void Start()
    {
        UpdateEquipWeaponVisibilty();
    }

    public void UpdateEquipSprite(Sprite sprite)
    {
        _equipImage.sprite = sprite;
        UpdateEquipWeaponVisibilty();
    }

    private void UpdateEquipWeaponVisibilty()
    {
        _equipImage.gameObject.SetActive(_equipImage.sprite != null);
    }
}
