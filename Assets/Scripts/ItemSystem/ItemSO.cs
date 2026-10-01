using UnityEngine;

[CreateAssetMenu(
    fileName = "Item",
    menuName = "New Item/Item"
)]
public class ItemSO : ScriptableObject
{
    [Header("General")]
    public string ItemName;

    [TextArea]
    public string Description;

    public Sprite InventoryIcon;

    [Header("Stack")]
    [SerializeField] private bool stackable;

    [Min(1)]
    [SerializeField] private int maxStackSize = 1;

    public bool Stackable => stackable;

    public int MaxStackSize =>
        stackable
            ? Mathf.Max(1, maxStackSize)
            : 1;
}
    public bool IsHealing;
    public AudioClip PickUpSound;
    public AudioClip DropSound;
}
