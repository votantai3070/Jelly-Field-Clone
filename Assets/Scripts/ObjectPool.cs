using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPoolable
{
    void OnSpawned();
    void OnDespawned();
}

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance { get; private set; }

    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int initialSize = 10;
        public Transform defaultParent;
    }

    [SerializeField] private List<Pool> pools = new();

    private readonly Dictionary<string, Queue<GameObject>> poolDictionary = new();
    private readonly Dictionary<string, GameObject> prefabDictionary = new();
    private readonly Dictionary<string, Transform> defaultParentDictionary = new();
    private readonly Dictionary<GameObject, string> spawnedObjects = new();
    private readonly Dictionary<GameObject, Coroutine> despawnRoutines = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        foreach (Pool pool in pools)
        {
            if (string.IsNullOrEmpty(pool.tag) || pool.prefab == null)
                continue;

            if (poolDictionary.ContainsKey(pool.tag))
            {
                Debug.LogWarning($"Duplicate pool tag: {pool.tag}");
                continue;
            }

            Queue<GameObject> queue = new();
            poolDictionary.Add(pool.tag, queue);
            prefabDictionary.Add(pool.tag, pool.prefab);
            defaultParentDictionary.Add(pool.tag, pool.defaultParent != null ? pool.defaultParent : transform);

            for (int i = 0; i < pool.initialSize; i++)
            {
                GameObject obj = CreateNewObject(pool.prefab, pool.tag, defaultParentDictionary[pool.tag]);
                queue.Enqueue(obj);
            }
        }
    }

    public GameObject Spawn(string tag, Transform parent = null)
    {
        return Spawn(tag, Vector3.zero, Quaternion.identity, parent, false);
    }

    public GameObject Spawn(string tag, Vector3 position, Quaternion rotation, Transform parent, bool useWorldSpace = true)
    {
        if (!poolDictionary.TryGetValue(tag, out Queue<GameObject> queue))
        {
            Debug.LogWarning($"Pool '{tag}' does not exist!");
            return null;
        }

        if (queue.Count == 0)
        {
            GameObject newObj = CreateNewObject(prefabDictionary[tag], tag, defaultParentDictionary[tag]);
            queue.Enqueue(newObj);
        }

        GameObject obj = queue.Dequeue();

        if (despawnRoutines.TryGetValue(obj, out Coroutine routine))
        {
            StopCoroutine(routine);
            despawnRoutines.Remove(obj);
        }

        Transform targetParent = parent != null ? parent : defaultParentDictionary[tag];
        obj.transform.SetParent(targetParent, false);

        if (useWorldSpace)
        {
            obj.transform.SetPositionAndRotation(position, rotation);
        }
        else
        {
            obj.transform.localPosition = position;
            obj.transform.localRotation = rotation;
        }

        obj.transform.localScale = Vector3.one;
        obj.SetActive(true);

        if (obj.TryGetComponent(out RectTransform rect))
        {
            rect.anchoredPosition3D = Vector3.zero;
            rect.localScale = Vector3.one;
        }

        if (obj.TryGetComponent(out IPoolable poolable))
        {
            poolable.OnSpawned();
        }

        spawnedObjects[obj] = tag;
        return obj;
    }

    public T Spawn<T>(string tag, Transform parent = null) where T : Component
    {
        GameObject obj = Spawn(tag, parent);
        return obj != null ? obj.GetComponent<T>() : null;
    }

    public void Despawn(GameObject obj, float delay = 0f)
    {
        if (obj == null) return;

        if (!spawnedObjects.TryGetValue(obj, out string tag))
        {
            Debug.LogWarning($"Object '{obj.name}' does not belong to any pool!");
            return;
        }

        if (despawnRoutines.TryGetValue(obj, out Coroutine runningRoutine))
        {
            StopCoroutine(runningRoutine);
            despawnRoutines.Remove(obj);
        }

        if (delay <= 0f)
        {
            ReturnToPool(tag, obj);
        }
        else
        {
            Coroutine routine = StartCoroutine(DespawnRoutine(tag, obj, delay));
            despawnRoutines[obj] = routine;
        }
    }

    private IEnumerator DespawnRoutine(string tag, GameObject obj, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (obj != null && obj.activeInHierarchy && spawnedObjects.ContainsKey(obj))
        {
            ReturnToPool(tag, obj);
        }

        despawnRoutines.Remove(obj);
    }

    private void ReturnToPool(string tag, GameObject obj)
    {
        if (obj.TryGetComponent(out IPoolable poolable))
        {
            poolable.OnDespawned();
        }

        obj.SetActive(false);
        obj.transform.SetParent(defaultParentDictionary[tag], false);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;

        spawnedObjects.Remove(obj);
        poolDictionary[tag].Enqueue(obj);
    }

    private GameObject CreateNewObject(GameObject prefab, string tag, Transform parent)
    {
        GameObject obj = Instantiate(prefab, parent);
        obj.name = $"{tag}_Pooled";
        obj.SetActive(false);
        return obj;
    }
}