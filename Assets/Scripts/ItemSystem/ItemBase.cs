using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ItemBase : MonoBehaviour
{
    public static event Action<ItemBase> OnAddItem;

    [SerializeField] private ItemSO _itemData;

    private Rigidbody _rb;

    public ItemSO ItemData => _itemData;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb == null) Debug.LogError("Error Loading Item Body!");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        OnAddItem?.Invoke(this);
        Destroy(gameObject);
    }

    public void ItemDebugCollect()
    {
        OnAddItem?.Invoke(this);
        Destroy(gameObject);
    }
}
