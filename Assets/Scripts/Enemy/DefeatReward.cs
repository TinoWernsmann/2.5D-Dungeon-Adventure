using UnityEngine;

/// <summary>
/// Belohnung, wenn dieser Gegner stirbt: Kill-Quest melden und ein Item fallen lassen (z.B. einen Schlüssel).
/// Das fallengelassene Item speichert der SaveManager als "Dropped Item".
/// </summary>
[RequireComponent(typeof(Health))]
public class DefeatReward : MonoBehaviour
{
    [Header("Quest")]
    [Tooltip("Ziel-ID der Kill-Quest (z.B. KillBoss2). Leer = keine Meldung.")]
    [SerializeField] private string questTargetId;

    [Header("Loot")]
    [SerializeField] private ItemBase lootPrefab;

    [Min(0f)]
    [SerializeField] private float upwardForce = 3f;

    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        health.Died += HandleDied;
    }

    private void OnDisable()
    {
        health.Died -= HandleDied;
    }

    private void HandleDied()
    {
        QuestManager.Report(QuestType.Kill, questTargetId);

        if (lootPrefab == null)
        {
            return;
        }

        ItemBase loot = Instantiate(lootPrefab, transform.position + Vector3.up, Quaternion.identity);

        if (loot.TryGetComponent(out Rigidbody body))
        {
            body.AddForce(Vector3.up * upwardForce, ForceMode.Impulse);
        }
    }
}
