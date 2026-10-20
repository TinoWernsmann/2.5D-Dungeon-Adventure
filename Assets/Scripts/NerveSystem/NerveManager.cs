using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class NerveManager : MonoBehaviour, ISaveable
{
    [SaveActiveState]
    [SerializeField] private GameObject _bossObject;

    [SaveActiveState]
    [SerializeField] private GameObject _bossDoorObject;

    [SerializeField] private Transform[] _spawnLocations;
    [SerializeField] private Nerve _nervePrefab;
    [SerializeField] private int LEVEL_NERVES;

    private List<Nerve> _levelNerves;
    private List<Transform> _chosenSpawnPos;
    private int _cutNerves;

    private void Awake()
    {
        _levelNerves = new List<Nerve>();
        _chosenSpawnPos = new List<Transform>();
        _cutNerves = 0;

        if (_spawnLocations == null) return;

        int count = 0;
        _chosenSpawnPos.AddRange(_spawnLocations);

        foreach (Transform pos in _spawnLocations)
        {
            if (count == LEVEL_NERVES) break;

            SpawnNerve(GetValidSpawnPos().position);
            count++;
        }
    }

    private Transform GetValidSpawnPos()
    {
        Transform removedTrans = _chosenSpawnPos[Random.Range(0, _chosenSpawnPos.Count)];
        _chosenSpawnPos.Remove(removedTrans);
        return removedTrans;
    }

    private Nerve SpawnNerve(Vector3 position)
    {
        Nerve nerve = Instantiate(_nervePrefab, position, Quaternion.identity);
        _levelNerves.Add(nerve);
        return nerve;
    }

    private void OnEnable()
    {
        if (_levelNerves != null && _levelNerves.Count > 0)
        {
            foreach (Nerve nerve in _levelNerves)
            {
                nerve.OnNerveCut += HandleNerveCut;
            }
        }
    }

    private void OnDisable()
    {
        if (_levelNerves != null && _levelNerves.Count > 0)
        {
            foreach (Nerve nerve in _levelNerves)
            {
                if (nerve != null) nerve.OnNerveCut -= HandleNerveCut;
            }
        }
    }

    private void HandleNerveCut()
    {
        _cutNerves++;

        if (_cutNerves >= LEVEL_NERVES)
        {
            AllNervesFound();
        }
    }

    private void AllNervesFound()
    {
        Debug.Log("Nerves Destroyed!");
        if (_bossObject != null) _bossObject.SetActive(true);
        if (_bossDoorObject != null) _bossDoorObject.SetActive(false);
    }

    #region Save System

    // Die Nerven werden zufällig verteilt – deshalb merken wir uns,
    // welche noch übrig sind und wo sie stehen.
    [Serializable]
    private struct SaveState
    {
        public int cutNerves;
        public List<Vector3> remainingNervePositions;
    }

    public string CaptureState()
    {
        SaveState state = new()
        {
            cutNerves = _cutNerves,
            remainingNervePositions = new List<Vector3>()
        };

        foreach (Nerve nerve in _levelNerves)
        {
            if (IsAlive(nerve))
            {
                state.remainingNervePositions.Add(nerve.transform.position);
            }
        }

        return JsonUtility.ToJson(state);
    }

    public void RestoreState(string state)
    {
        SaveState saveState = JsonUtility.FromJson<SaveState>(state);

        RemoveAllNerves();

        if (saveState.remainingNervePositions != null)
        {
            foreach (Vector3 position in saveState.remainingNervePositions)
            {
                SpawnNerve(position).OnNerveCut += HandleNerveCut;
            }
        }

        _cutNerves = saveState.cutNerves;

        if (_cutNerves >= LEVEL_NERVES)
        {
            AllNervesFound();
        }
    }

    private void RemoveAllNerves()
    {
        foreach (Nerve nerve in _levelNerves)
        {
            if (nerve == null) continue;

            nerve.OnNerveCut -= HandleNerveCut;
            Destroy(nerve.gameObject);
        }

        _levelNerves.Clear();
    }

    private static bool IsAlive(Nerve nerve)
    {
        return nerve != null &&
               nerve.TryGetComponent(out Health health) &&
               !health.IsDead;
    }

    #endregion
}
