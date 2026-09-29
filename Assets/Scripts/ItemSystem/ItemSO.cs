using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "New Item/Item")]
public class ItemSO : ScriptableObject
{
    public ItemType ItemType;
    public string Name;
    public string Description;
    public Sprite ItemSprite;
}

public enum ItemType
{
    Key,
    Weapon
}
