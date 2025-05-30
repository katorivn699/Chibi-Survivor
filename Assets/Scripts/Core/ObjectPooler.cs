using System.Collections.Generic;
using UnityEngine;

public class ObjectPooler : MonoBehaviour
{
    [System.Serializable]
    public class Pool
    {
        public string tag;
        public GameObject prefab;
        public int size;
    }

    public static ObjectPooler Instance;

    public List<Pool> pools;
    public Dictionary<string, Queue<GameObject>> poolDictionary;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab);
                obj.SetActive(false);
                objectPool.Enqueue(obj);
            }

            poolDictionary.Add(pool.tag, objectPool);
        }
    }

    public GameObject SpawnFromPool(string tag, Vector3 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning("Pool with tag " + tag + " doesn't exist.");
            return null;
        }

        Queue<GameObject> objectQueue = poolDictionary[tag];

        if (objectQueue.Count == 0)
        {
            // Mở rộng pool bằng cách tạo thêm một object mới
            Pool pool = pools.Find(p => p.tag == tag);
            if (pool == null)
            {
                Debug.LogWarning("Pool not found for tag: " + tag);
                return null;
            }

            GameObject newObj = Instantiate(pool.prefab);
            newObj.SetActive(false);
            objectQueue.Enqueue(newObj);
        }

        // Lúc này chắc chắn có ít nhất 1 object
        GameObject objectToSpawn = objectQueue.Dequeue();

        objectToSpawn.SetActive(true);
        objectToSpawn.transform.position = position;
        objectToSpawn.transform.rotation = rotation;

        // Nếu có IPooledObject, gọi OnObjectSpawn()
        IPooledObject pooledObj = objectToSpawn.GetComponent<IPooledObject>();
        if (pooledObj != null)
        {
            pooledObj.OnObjectSpawn();
        }

        return objectToSpawn;
    }

    public interface IPooledObject
    {
        void OnObjectSpawn();
    }


}
