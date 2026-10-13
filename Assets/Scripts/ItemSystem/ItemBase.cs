using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ItemBase : MonoBehaviour, ISaveable
{
    private const float FloatSpeed = 3f;
    private const float FloatHeight = 0.1f;
    private bool _questReported;

    [Header("Item")]
    [SerializeField] private ItemSO itemData;

    [Header("Pickup")]
    [Min(1)]
    [SerializeField] private int pickupAmount = 1;

    private Rigidbody itemRigidbody;
    private Vector3 startPosition;
    private bool hasStarted;

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

        hasStarted = true;
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

    public void ReportPickupToQuest()
    {
        // Nur einmal pro Item, sonst zählt "Droppen und wieder aufheben" doppelt
        if (_questReported || itemData == null) return;
        if (QuestManager.Instance == null) return;

        _questReported = true;
        QuestManager.Instance.ReportProgress(
            QuestType.Collect,
            itemData.ItemName,
            PickupAmount
        );
    }

    #region Save System

    // Ob das Item aufgehoben wurde (= inaktiv), speichert die SaveableEntity.
    [Serializable]
    private struct SaveState
    {
        public Vector3 position;
    }

    public string CaptureState()
    {
        // startPosition gibt es erst nach Start() – vorher (nie aktiv gewesen) die echte Position.
        Vector3 position = hasStarted ? startPosition : transform.position;

        return JsonUtility.ToJson(new SaveState { position = position });
    }

    public void RestoreState(string state)
    {
        SaveState saveState = JsonUtility.FromJson<SaveState>(state);

        transform.position = saveState.position;
        startPosition = saveState.position;
    }

    #endregion

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