using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class PoolManager : MonoBehaviour
{
    [SerializeField] private bool dontDestroyOnLoadFlag = false;
    private GameObject _empthyHolder;

    private static GameObject _scrapyardEmpty;
    private static GameObject _forestEmpty;

    private static Dictionary<GameObject, ObjectPool<GameObject>> _objectPools;
    private static Dictionary<GameObject, GameObject> _cloneToPrefabEmpty;
    public enum PoolType
    {
        Forest,
        Scrapyard,
    }
    public static PoolType poolType;
    void Awake()
    {
        _objectPools = new Dictionary<GameObject, ObjectPool<GameObject>>();
        _cloneToPrefabEmpty = new Dictionary<GameObject, GameObject>();
        SetupEmpties();
    }
    private void SetupEmpties()
    {
        _empthyHolder = new GameObject("Object Pools");
        _forestEmpty = new GameObject("Forest Empty");
        _scrapyardEmpty = new GameObject("Scrapyard Empty");
        _forestEmpty.transform.SetParent(_empthyHolder.transform);
        _scrapyardEmpty.transform.SetParent(_empthyHolder.transform);
        if (dontDestroyOnLoadFlag)
        {
            DontDestroyOnLoad(_forestEmpty.transform.root);
        }
    }
    private static void CreatePool(GameObject prefab, Vector3 pos, Quaternion rot, PoolType poolType = PoolType.Forest)
    {
        ObjectPool<GameObject> pool = new ObjectPool<GameObject>(createFunc: () => CreateObject(prefab, pos, rot, 10, poolType),
            actionOnGet: OnGetObject,
            actionOnRelease: OnReleaseObject,
            actionOnDestroy: OnDestroyObject
            );

        _objectPools.Add(prefab, pool);
    }
    private static GameObject CreateObject(GameObject prefab, Vector3 pos, Quaternion rot, int initialSize, PoolType poolType = PoolType.Forest)
    {
        prefab.SetActive(false);
        GameObject obj = Instantiate(prefab, pos, rot);
        prefab.SetActive(true);
        GameObject parentObject = SetParentObject(poolType);
        obj.transform.SetParent(parentObject.transform);
        return obj;
    }
    private static void OnGetObject(GameObject obj)
    {

    }
    private static void OnReleaseObject(GameObject obj)
    {
        obj.SetActive(false);
    }
    private static void OnDestroyObject(GameObject obj)
    {
        if (_cloneToPrefabEmpty.ContainsKey(obj))
        {
            _cloneToPrefabEmpty.Remove(obj);
        }
    }

    private static GameObject SetParentObject(PoolType poolType)
    {
        switch (poolType)
        {
            case PoolType.Forest:
                return _forestEmpty;
            case PoolType.Scrapyard:
                return _scrapyardEmpty;
            default:
                return null;
        }
    }

    private static T SpawnObject<T>(GameObject objectToSpawn, Vector3 spawnPos, Quaternion spawnRot, PoolType poolType = PoolType.Forest) where T : Object
    {
        if (!_objectPools.ContainsKey(objectToSpawn))
        {
            CreatePool(objectToSpawn, spawnPos, spawnRot, poolType);
        }
        GameObject obj = _objectPools[objectToSpawn].Get();
        if (obj != null)
        {
            if (!_cloneToPrefabEmpty.ContainsKey(obj))
            {
                _cloneToPrefabEmpty.Add(obj, objectToSpawn);
            }
            obj.transform.position = spawnPos;
            obj.transform.rotation = spawnRot;
            obj.SetActive(true);
            if (typeof(T) == typeof(GameObject))
            {
                return obj as T;
            }
            T component = obj.GetComponent<T>();
            if (component == null)
            {
                Debug.LogError($"Object of type {objectToSpawn.name} doesnt have {typeof(T)}.");
                return null;
            }
            return component;
        }
        return null;
    }

    public static T SpawnObject<T>(T typePrefab, Vector3 spawnPos, Quaternion spawnRot, PoolType poolType = PoolType.Forest) where T : Component
    {
        return SpawnObject<T>(typePrefab.gameObject, spawnPos, spawnRot, poolType);
    }
    public static GameObject SpawnObject(GameObject objectToSpawn, Vector3 spawnPos, Quaternion spawnRot, PoolType poolType = PoolType.Forest)
    {
        return SpawnObject<GameObject>(objectToSpawn, spawnPos, spawnRot, poolType);
    }
    public static void ReturnObjectToPool(GameObject obj, PoolType poolType = PoolType.Forest)
    {
        if (_cloneToPrefabEmpty.TryGetValue(obj, out GameObject prefab))
        {
            GameObject parentObject = SetParentObject(poolType);
            if (obj.transform.parent != parentObject.transform)
            {
                obj.transform.SetParent(parentObject.transform);
            }
            if (_objectPools.TryGetValue(prefab, out ObjectPool<GameObject> pool))
            {
                pool.Release(obj);
            }

        }
        else
        {
            Debug.LogWarning($"Object {obj.name} does not belong to any pool.");
        }
    }
    
}
