using System.Collections;
using UnityEngine;

public class Chest : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] private Transform lidPivot;
    [SerializeField] private Transform dropPoint;
    [SerializeField] private AudioSource audioSource;

    [Header("Chest")]
    [SerializeField] private float openAngle = -100f;
    [SerializeField] private float animationDuration = 0.5f;

    [Header("Loot")]
    [SerializeField] private GameObject itemPrefab;

    [Header("Drop Physics")]
    [SerializeField] private float upwardForce = 3f;
    [SerializeField] private float forwardForce = 1.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip openSound;

    private Quaternion closedRotation;
    private Quaternion openRotation;

    private bool isOpen;
    private bool isAnimating;

    private void Awake()
    {
        if (lidPivot == null)
        {
            Debug.LogError(
                $"{nameof(Chest)} on {name} requires a Lid Pivot.",
                this
            );

            return;
        }

        closedRotation = lidPivot.localRotation;

        openRotation =
            closedRotation *
            Quaternion.Euler(0f, 0f, openAngle);
    }

    public void Interact()
    {
        if (isOpen || isAnimating || lidPivot == null)
        {
            return;
        }

        StartCoroutine(OpenChest());
    }

    private IEnumerator OpenChest()
    {
        isAnimating = true;

        PlayOpenSound();

        Quaternion startRotation = lidPivot.localRotation;
        float elapsedTime = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;

            float progress = Mathf.Clamp01(
                elapsedTime / animationDuration
            );

            lidPivot.localRotation = Quaternion.Slerp(
                startRotation,
                openRotation,
                progress
            );

            yield return null;
        }

        lidPivot.localRotation = openRotation;

        isOpen = true;
        isAnimating = false;

        SpawnLoot();
    }

    private void SpawnLoot()
    {
        if (itemPrefab == null)
        {
            return;
        }

        Vector3 spawnPosition = dropPoint != null
            ? dropPoint.position
            : transform.position + Vector3.up;

        GameObject item = Instantiate(
            itemPrefab,
            spawnPosition,
            Quaternion.identity
        );

        LaunchLoot(item);
    }

    private void LaunchLoot(GameObject item)
    {
        if (!item.TryGetComponent(out Rigidbody rigidbody))
        {
            return;
        }

        Vector3 launchDirection =
            Vector3.up * upwardForce +
            transform.forward * forwardForce;

        rigidbody.AddForce(
            launchDirection,
            ForceMode.Impulse
        );
    }

    private void PlayOpenSound()
    {
        if (audioSource == null || openSound == null)
        {
            return;
        }

        audioSource.PlayOneShot(openSound);
    }
}