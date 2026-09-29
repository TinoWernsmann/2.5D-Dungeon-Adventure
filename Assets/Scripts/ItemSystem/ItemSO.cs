using UnityEngine;

[CreateAssetMenu(fileName = "Item", menuName = "New Item/Item")]
public class ItemSO : ScriptableObject
{
    public string ItemName;
    public string Description;
    public Sprite InventoryIcon;
}
