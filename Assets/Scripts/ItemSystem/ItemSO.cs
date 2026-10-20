using UnityEngine;
using UnityEngine.Serialization;

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

    [Header("Held")]
    [Tooltip("Sprite, das der Player in der Hand hält, wenn das Item ausgewählt ist. Leer = nichts in der Hand.")]
    [FormerlySerializedAs("IdleSprite")]
    public Sprite HeldSprite;

    [Header("Stack")]
    [SerializeField] private bool stackable;

    [Min(1)]
    [SerializeField] private int maxStackSize = 1;

    public bool Stackable => stackable;

    public int MaxStackSize =>
        stackable
            ? Mathf.Max(1, maxStackSize)
            : 1;

    [Header("Use")]
    [Tooltip("Heiltrank: wird mit H oder Linksklick (wenn ausgewählt) getrunken.")]
    public bool IsHealing;

    [Header("Tutorial")]
    [Tooltip("Wird beim ersten Aufheben dieses Items angezeigt. Leer = kein Tutorial.")]
    public TutorialSO PickupTutorial;

    [Header("Audio")]
    public AudioClip PickUpSound;
    public AudioClip DropSound;
}
