using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ItemBase : MonoBehaviour
{
    public static event Action<ItemBase> OnAddItem;

    [SerializeField] private ItemSO _itemData;

    private const float FLOAT_SPEED = 3f;
    private const float FLOAT_HEIGHT = 0.1f;
    private Rigidbody _rb;
    private Vector3 _startPos;

    public ItemSO ItemData => _itemData;

    private void Start()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb == null) Debug.LogError("Error Loading Item Body!");

        _startPos = transform.position;
    }

    private void Update()
    {
        FloatItem();
    }

    private void FloatItem()
    {
        float newY = _startPos.y + Mathf.Sin(Time.time * FLOAT_SPEED) * FLOAT_HEIGHT;
        transform.position = new Vector3(_startPos.x, newY, _startPos.z);
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
