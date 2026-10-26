using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Tür, die der Player öffnen/schließen kann. Abgeschlossene Türen brauchen ein Item (Schlüssel).
///
/// Gegner: Unverschlossene Türen blockieren das NavMesh NICHT – Gegner planen ihren Weg
/// hindurch und öffnen sie, wenn sie davor stehen. Dafür erzeugt die Tür eine unsichtbare
/// Zone (<see cref="DoorProximityTrigger"/>), die Gegnern meldet, dass eine Tür in der Nähe ist.
/// Abgeschlossene Türen bleiben für Gegner unpassierbar.
/// </summary>
public class Door : MonoBehaviour, IInteractable, ISaveable
{
    [Header("References")]
    [SerializeField] private ItemManager itemManager;
    [SerializeField] private Transform doorPivot;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Collider visionBlocker;
    [SerializeField] private NavMeshObstacle navMeshObstacle;

    [Header("Door Settings")]
    [SerializeField] private float openAngle = -90f;
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private bool isLocked;
    [SerializeField] private string neededUnlockItem;

    [Tooltip("Wird nach dem Aufschließen geladen (z.B. CreditScene am Levelende). Leer = nichts.")]
    [SerializeField] private string sceneToLoadOnUnlock;

    [Header("Enemies")]
    [Tooltip("Gegner können diese Tür öffnen, solange sie nicht abgeschlossen ist.")]
    [SerializeField] private bool enemiesCanOpen = true;

    [Tooltip("In diesem Umkreis öffnen Gegner die Tür, wenn sie darauf zulaufen.")]
    [Min(0.5f)]
    [SerializeField] private float enemyOpenRadius = 2.2f;

    [Header("Audio")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [SerializeField] private AudioClip lockedSound;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    private bool isOpen;
    private bool isAnimating;

    public bool CanBeOpenedByEnemy =>
        enemiesCanOpen && !isLocked && !isOpen && !isAnimating;

    /// <summary>Mitte der Türöffnung (der Drehpunkt liegt am Rand).</summary>
    public Vector3 Center =>
        visionBlocker != null ? visionBlocker.bounds.center : transform.position;

    /// <summary>Ein Gegner läuft auf die geschlossene Tür zu und öffnet sie.</summary>
    public void OpenByEnemy()
    {
        if (CanBeOpenedByEnemy)
        {
            OpenDoor();
        }
    }

    private void Awake()
    {
        if (itemManager == null)
        {
            itemManager = FindAnyObjectByType<ItemManager>();
        }

        closedRotation = doorPivot.localRotation;
        openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);

        SetDoorBlocking(true);

        if (enemiesCanOpen)
        {
            DoorProximityTrigger.Create(this, Center, enemyOpenRadius);
        }
    }

    public virtual void Interact()
    {
        if (isAnimating)
        {
            return;
        }

        if (isLocked)
        {
            if (itemManager != null && itemManager.HasItem(neededUnlockItem))
            {
                itemManager.RemoveItemByName(neededUnlockItem);
                isLocked = false;
                OpenDoor();

                if (!string.IsNullOrEmpty(sceneToLoadOnUnlock))
                {
                    StartCoroutine(LoadSceneAfterOpening());
                }

                return;
            }
            else
            {
                PlaySound(lockedSound);
                return;
            }
        }

        if (isOpen)
        {
            CloseDoor();
            return;
        }

        OpenDoor();
    }

    private void OpenDoor()
    {
        SetDoorBlocking(false);
        PlaySound(openSound);

        StartCoroutine(
            RotateDoor(openRotation, true)
        );
    }

    /// <summary>Erst die Tür aufgehen lassen, dann die nächste Szene laden.</summary>
    private IEnumerator LoadSceneAfterOpening()
    {
        yield return new WaitForSeconds(animationDuration + 0.5f);

        SceneLoader.Load(sceneToLoadOnUnlock);
    }

    private void CloseDoor()
    {
        PlaySound(closeSound);

        StartCoroutine(
            RotateDoor(closedRotation, false)
        );
    }

    private IEnumerator RotateDoor(
        Quaternion targetRotation,
        bool targetOpenState)
    {
        isAnimating = true;

        Quaternion startRotation = doorPivot.localRotation;
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / animationDuration
            );

            doorPivot.localRotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                progress
            );

            yield return null;
        }

        doorPivot.localRotation = targetRotation;
        isOpen = targetOpenState;
        isAnimating = false;

        if (!isOpen)
        {
            SetDoorBlocking(true);
        }
    }

    private void SetDoorBlocking(bool blocked)
    {
        if (visionBlocker != null)
        {
            visionBlocker.enabled = blocked;
        }

        if (navMeshObstacle != null)
        {
            // Nur Türen, die Gegner nicht öffnen können, sperren den Weg im NavMesh.
            bool blocksEnemies = isLocked || !enemiesCanOpen;
            navMeshObstacle.enabled = blocked && blocksEnemies;
        }
    }

    #region Save System

    [Serializable]
    private struct SaveState
    {
        public bool isOpen;
        public bool isLocked;
    }

    public string CaptureState()
    {
        return JsonUtility.ToJson(new SaveState { isOpen = isOpen, isLocked = isLocked });
    }

    public void RestoreState(string state)
    {
        SaveState saveState = JsonUtility.FromJson<SaveState>(state);

        StopAllCoroutines();
        isAnimating = false;

        isOpen = saveState.isOpen;
        isLocked = saveState.isLocked;

        doorPivot.localRotation = isOpen ? openRotation : closedRotation;
        SetDoorBlocking(!isOpen);
    }

    #endregion

    private void PlaySound(AudioClip sound)
    {
        if (audioSource == null || sound == null)
        {
            return;
        }

        audioSource.PlayOneShot(sound);
    }
}