using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ItemBase : MonoBehaviour
{
    private const float FloatSpeed = 3f;
    private const float FloatHeight = 0.1f;

    [Header("Item")]
    [SerializeField] private ItemSO itemData;

    [Header("Pickup")]
    [Min(1)]
    [SerializeField] private int pickupAmount = 1;

    private Rigidbody itemRigidbody;
    private Vector3 startPosition;

    public ItemSO ItemData => itemData;

    public int PickupAmount =>
        Mathf.Max(1, pickupAmount);

    private void Awake()
    {
        itemRigidbody =
            GetComponent<Rigidbody>();

        ValidateReferences();
    }

    private void Start()
    {
        startPosition =
            transform.position;
    }

    private void Update()
    {
        UpdateFloatingMovement();
    }

    private void OnTriggerEnter(
        Collider other)
    {
        ItemCollector collector =
            other.GetComponentInParent<
                ItemCollector>();

        if (collector == null)
        {
            return;
        }

        TryCollect(collector);
    }

    public void DropAt(
        Vector3 position)
    {
        transform.position =
            position;

        startPosition =
            position;

        gameObject.SetActive(true);
    }

    public void SetPickupAmount(
        int amount)
    {
        pickupAmount =
            Mathf.Max(1, amount);
    }

    private void TryCollect(
        ItemCollector collector)
    {
        bool wasCollected =
            collector.TryCollect(this);

        if (!wasCollected)
        {
            return;
        }

        gameObject.SetActive(false);
    }

    private void UpdateFloatingMovement()
    {
        float verticalOffset =
            Mathf.Sin(
                Time.time * FloatSpeed
            ) * FloatHeight;

        transform.position =
            new Vector3(
                startPosition.x,
                startPosition.y +
                verticalOffset,
                startPosition.z
            );
    }

    private void ValidateReferences()
    {
        if (itemData == null)
        {
            Debug.LogError(
                "ItemBase: ItemData reference " +
                "is missing.",
                this
            );
        }

        if (itemRigidbody == null)
        {
            Debug.LogError(
                "ItemBase: Rigidbody is missing.",
                this
            );
        }
    }
}