using System;
using System.Collections.Generic;
using UnityEngine;

public class NerveManager : MonoBehaviour
{
    [SerializeField] private GameObject _bossEntryObject;
    [SerializeField] private Transform[] _spawnLocations;
    [SerializeField] private GameObject _nervePrefab;
    [SerializeField] private int LEVEL_NERVES;

    private List<Nerve> _levelNerves;
    private List<Transform> _chosenSpawnPos;
    private int _levelNervesAmount;
    private int _cutNerves;

    private void Awake() 
    {
        _levelNerves = new List<Nerve>();
        _chosenSpawnPos = new List<Transform>(); 
        _cutNerves = 0;
    }

    private void Start()
    {
        if (_spawnLocations == null) return;

        int count = 0;
        _chosenSpawnPos.AddRange(_spawnLocations);

        foreach (Transform pos in _spawnLocations)
        {
            if (count == LEVEL_NERVES) break;

            GameObject nerve = Instantiate(_nervePrefab);
            nerve.transform.position = GetValidSpawnPos().position;
            nerve.transform.rotation = Quaternion.identity;
            count++;
        }
    }

    private Transform GetValidSpawnPos()
    {
        Transform removedTrans = _chosenSpawnPos[UnityEngine.Random.Range(0, _chosenSpawnPos.Count)];
        _chosenSpawnPos.Remove(removedTrans);
        return removedTrans;
    }

    private void OnEnable()
    {
        if (_levelNerves != null &&  _levelNerves.Count > 0)
        {
            foreach (Nerve nerves in _levelNerves)
            {
                nerves.OnNerveCut += HandleNerveCut;
            }
        }      
    }

    private void OnDisable()
    {
        if (_levelNerves != null &&  _levelNerves.Count > 0)
        {
            foreach (Nerve nerves in _levelNerves)
            {
                nerves.OnNerveCut -= HandleNerveCut;
            }
        }      
    }

    private void HandleNerveCut() 
    {
        _cutNerves++;

        if (_cutNerves >= _levelNervesAmount)
        {
            AllNervesFound();
        }
    }

    private void AllNervesFound()
    {
        if (_bossEntryObject == null) return;
        _bossEntryObject?.SetActive(false);
    }
}
