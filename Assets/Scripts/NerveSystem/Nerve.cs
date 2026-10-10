using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class Nerve : MonoBehaviour
{
    public event Action OnNerveCut;

    [SerializeField] private Health _health;
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _cutClip;
    [SerializeField] private GameObject _visual;

    [Header("Quest")]
    [Tooltip("Muss der Target Id der Destroy-Quest entsprechen")]
    [SerializeField] private string _questTargetId = "Nerve";

    private void OnEnable()
    {
        _health.Died += HandleNerveDeath;
    }

    private void OnDisable()
    {
        _health.Died -= HandleNerveDeath;
    }

    private void HandleNerveDeath()
    {
        StartCoroutine(PlayDieSoundThenDespawn());
        Debug.Log($"Nerve zerstört: {name}, Quest-ID: {_questTargetId}");
    }

    private IEnumerator PlayDieSoundThenDespawn()
    {
        _audioSource.PlayOneShot(_cutClip);
        _visual.SetActive(false);
        OnNerveCut?.Invoke();
        QuestManager.Report(QuestType.Destroy, _questTargetId);

        yield return new WaitForSeconds(_cutClip.length);

        Destroy(gameObject);
    }
}