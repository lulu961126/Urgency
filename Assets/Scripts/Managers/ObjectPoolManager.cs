using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 簡單的物件池管理器 - 減少 Instantiate 與 Destroy 的開銷
/// </summary>
public class ObjectPoolManager : MonoBehaviour
{
    private static ObjectPoolManager instance;
    public static ObjectPoolManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Object.FindFirstObjectByType<ObjectPoolManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("_ObjectPoolManager");
                    instance = go.AddComponent<ObjectPoolManager>();
                    // DontDestroyOnLoad(go); // 如果需要跨場景則開啟
                }
            }
            return instance;
        }
    }

    private Dictionary<int, Queue<GameObject>> pools = new Dictionary<int, Queue<GameObject>>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 從池中取得物件，如果池空則生成新的
    /// </summary>
    public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        int key = prefab.GetInstanceID();

        if (!pools.ContainsKey(key))
        {
            pools[key] = new Queue<GameObject>();
        }

        GameObject obj;
        if (pools[key].Count > 0)
        {
            obj = pools[key].Dequeue();
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.SetActive(true);
        }
        else
        {
            obj = Instantiate(prefab, position, rotation);
            // 給予識別碼，以便回收時知道屬於哪個池
            var poolItem = obj.GetComponent<PoolItem>() ?? obj.AddComponent<PoolItem>();
            poolItem.prefabKey = key;
        }

        return obj;
    }

    /// <summary>
    /// 將物件回收至池中
    /// </summary>
    public void Recycle(GameObject obj)
    {
        if (obj == null) return;

        var poolItem = obj.GetComponent<PoolItem>();
        if (poolItem != null && pools.ContainsKey(poolItem.prefabKey))
        {
            obj.SetActive(false);
            // 確保不會重複加入佇列
            if (!pools[poolItem.prefabKey].Contains(obj))
            {
                pools[poolItem.prefabKey].Enqueue(obj);
            }
        }
        else
        {
            // 如果不是從池中生成的，則直接銷毀
            Destroy(obj);
        }
    }

    /// <summary>
    /// 靜態便捷方法：回收物件
    /// </summary>
    public static void Return(GameObject obj)
    {
        if (Instance != null)
        {
            Instance.Recycle(obj);
        }
        else
        {
            Destroy(obj);
        }
    }
}

/// <summary>
/// 用於標記物件池物品的輔助組件
/// </summary>
public class PoolItem : MonoBehaviour
{
    public int prefabKey;
}
