using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Door : MonoBehaviour, IInteractable
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

    [Header("Audio")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [SerializeField] private AudioClip lockedSound;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    private bool isOpen;
    private bool isAnimating;

    private void Awake()
    {
        if (itemManager == null)
        {
            itemManager = FindAnyObjectByType<ItemManager>();
        }

        closedRotation = doorPivot.localRotation;
        openRotation = closedRotation * Quaternion.Euler(0f, openAngle, 0f);

        SetDoorBlocking(true);
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
            navMeshObstacle.enabled = blocked;
        }
    }

    private void PlaySound(AudioClip sound)
    {
        if (audioSource == null || sound == null)
        {
            return;
        }

        audioSource.PlayOneShot(sound);
    }
}