using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gibt einem Objekt in der Szene eine feste, eindeutige ID, damit es im Spielstand
/// wiedergefunden wird. Speichert:
///  - ob das Objekt aktiv ist,
///  - ob es zerstört wurde,
///  - den Zustand aller <see cref="ISaveable"/>-Komponenten auf dem Objekt.
///
/// Wird per "Tools → Save System → Setup Open Scene" automatisch hinzugefügt.
/// </summary>
[DisallowMultipleComponent]
public class SaveableEntity : MonoBehaviour
{
    private static readonly Dictionary<string, SaveableEntity> liveEntities = new();

    [Tooltip("Wird automatisch vergeben. Nicht von Hand ändern, sonst passen alte Spielstände nicht mehr.")]
    [SerializeField] private string id;

    public string Id => id;

    public bool HasId => !string.IsNullOrEmpty(id);

    private void Awake()
    {
        if (!HasId)
        {
            return;
        }

        // Instantiate() kopiert auch die ID. Eine Kopie ist kein Szenenobjekt
        // und wird wie ein zur Laufzeit erzeugtes Objekt behandelt.
        if (liveEntities.TryGetValue(id, out SaveableEntity original) &&
            original != null &&
            original != this)
        {
            id = string.Empty;
            return;
        }

        liveEntities[id] = this;
    }

    private void OnDestroy()
    {
        if (!HasId)
        {
            return;
        }

        if (liveEntities.TryGetValue(id, out SaveableEntity registered) && registered == this)
        {
            liveEntities.Remove(id);
        }

        // Beim Szenenwechsel / Beenden werden alle Objekte zerstört – das zählt nicht.
        bool isDestroyedDuringGameplay = gameObject.scene.isLoaded && !SaveManager.IsQuitting;

        if (isDestroyedDuringGameplay)
        {
            SaveManager.NotifyEntityDestroyed(id);
        }
    }

    public EntitySaveData Capture()
    {
        EntitySaveData data = new()
        {
            id = id,
            isActive = gameObject.activeSelf
        };

        foreach (ISaveable saveable in GetComponents<ISaveable>())
        {
            data.components.Add(new ComponentSaveData
            {
                type = saveable.GetType().FullName,
                state = saveable.CaptureState()
            });
        }

        return data;
    }

    public void Restore(EntitySaveData data)
    {
        // Erst aktivieren (damit Awake gelaufen ist), dann Zustände setzen.
        if (data.isActive)
        {
            gameObject.SetActive(true);
        }

        foreach (ISaveable saveable in GetComponents<ISaveable>())
        {
            string typeName = saveable.GetType().FullName;
            ComponentSaveData componentData = data.components.Find(c => c.type == typeName);

            if (componentData != null)
            {
                saveable.RestoreState(componentData.state);
            }
        }

        if (!data.isActive)
        {
            gameObject.SetActive(false);
        }
    }

#if UNITY_EDITOR
    /// <summary>Vergibt eine neue ID, falls keine vorhanden ist oder sie doppelt ist (z.B. nach Strg+D).</summary>
    public bool EnsureUniqueId(ICollection<string> usedIds)
    {
        if (HasId && !usedIds.Contains(id))
        {
            usedIds.Add(id);
            return false;
        }

        id = System.Guid.NewGuid().ToString("N");
        usedIds.Add(id);

        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);

        return true;
    }

    private void OnValidate()
    {
        if (Application.isPlaying || !gameObject.scene.IsValid())
        {
            return;
        }

        // Prefab-Assets (auch im Prefab-Modus) bekommen keine ID – nur Objekte in Szenen.
        if (UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(gameObject) != null)
        {
            if (HasId)
            {
                id = string.Empty;
                UnityEditor.EditorUtility.SetDirty(this);
            }

            return;
        }

        if (!HasId)
        {
            id = System.Guid.NewGuid().ToString("N");
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
        }
    }
#endif
}
