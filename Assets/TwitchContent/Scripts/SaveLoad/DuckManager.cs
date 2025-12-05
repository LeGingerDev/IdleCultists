using System.Collections;
using System.Collections.Generic;
using Audio.Core;
using Audio.Managers;
using LGD.Core.Singleton;
using LGD.Gameplay.Polish;
using LGD.Utilities.Extensions;
using TMPro;
using UnityEngine;

public class DuckManager : MonoSingleton<DuckManager>
{
    public List<DuckRuntimeData> ducks = new List<DuckRuntimeData>();
    public Transform leftSpawnPoint;
    public Transform rightSpawnPoint;

    private SaveLoadProviderBase<DuckRuntimeData> _saveProvider;
    private DuckRegistry _duckRegistry;

    public void Start()
    {
        StartCoroutine(LoadDucks());
    }

    private IEnumerator LoadDucks()
    {
        _saveProvider = SaveLoadProviderManager.Instance.GetProvider<DuckRuntimeData>();
        _duckRegistry = RegistryManager.Instance.GetRegistry<DuckBlueprint>() as DuckRegistry;

        yield return _saveProvider.Load();

        if (!_saveProvider.HasData())
            yield break;

        LoadDucksActive();
    }

    public void LoadDucksActive()
    {
        List<DuckRuntimeData> savedDucks = _saveProvider.GetData();
        foreach (DuckRuntimeData runtimeData in savedDucks)
        {
            DuckBlueprint blueprint = _duckRegistry.GetItemById(runtimeData.blueprintId);
            LoadDuckFromSave(blueprint, runtimeData);
        }
    }

    public void LoadDuckFromSave(DuckBlueprint blueprint, DuckRuntimeData runtimeData)
    {
        Vector3 spawnPoint = Vector3.Lerp(leftSpawnPoint.position, rightSpawnPoint.position, Random.value); ;
        RubberDuckController duckToSpawn = _duckRegistry.GetAllItems().Random().duckPrefab;
        GameObject duck = Instantiate(duckToSpawn.gameObject, spawnPoint, Quaternion.identity);

        AudioManager.Instance.PlaySFX(AudioConstIds.DUCK_QUACKS, true);
        duck.GetComponentInChildren<TextMeshProUGUI>().text = runtimeData.duckOwner;

        RandomHsvShift hsvShift = duck.GetComponentInChildren<RandomHsvShift>();
        hsvShift.SetHSVValue(runtimeData.duckHSVValue);
        runtimeData.objRef = duck;
        ducks.Add(runtimeData);
    }

    public IEnumerator GenerateNewDuck(string owner)
    {
        Vector3 spawnPoint = Vector3.Lerp(leftSpawnPoint.position, rightSpawnPoint.position, Random.value); ;
        DuckBlueprint blueprint = _duckRegistry.GetAllItems().Random();
        RubberDuckController duckToSpawn = blueprint.duckPrefab;
        GameObject duck = Instantiate(duckToSpawn.gameObject, spawnPoint, Quaternion.identity);

        AudioManager.Instance.PlaySFX(AudioConstIds.DUCK_QUACKS, true);
        duck.GetComponentInChildren<TextMeshProUGUI>().text = owner;

        yield return new WaitForEndOfFrame();

        RandomHsvShift hsvShift = duck.GetComponentInChildren<RandomHsvShift>();
        float assignedHsvValue = hsvShift.GetAssignedValue();

        DuckRuntimeData runtimeData = new DuckRuntimeData
        {
            blueprintId = blueprint.id,
            duckOwner = owner,
            duckHSVValue = assignedHsvValue,
            objRef = duck
        };

        ducks.Add(runtimeData);
        yield return _saveProvider.SetAndSave(ducks);
    }

    public int GetNumberOfDucks()
    {
        return ducks.Count;
    }

    public bool CanSpawnMoreDucks()
    {
        return ducks.Count < 20;
    }

    public void RemoveEarliestDuckIfNeeded()
    {
        if (ducks.Count >= 20)
        {
            RemoveEarliestDuck();
        }
    }

    public void RemoveEarliestDuck()
    {
        if (ducks.Count == 0) return;

        GameObject duck = ducks[0].objRef;
        ducks.RemoveAt(0);
        Destroy(duck);
    }

    public void RemoveDuck(GameObject duck)
    {
        DuckRuntimeData runtimeData = ducks.Find(d => d.objRef == duck);
        if (runtimeData != null)
        {
            ducks.Remove(runtimeData);
            Destroy(duck);
        }
    }

}

