using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 通用的物件池管理器。透過回收重複利用物件（如子彈、特效）來降低頻繁生成與銷毀帶來的效能開銷。
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
                }
            }
            return instance;
        }
    }

    private Dictionary<int, Queue<GameObject>> pools = new Dictionary<int, Queue<GameObject>>();

    private void Awake()
    {
        if (instance == null) instance = this;
        else if (instance != this) Destroy(gameObject);
    }

    /// <summary>
    /// 從池中取得一個物件實例。若池中無可用物件，則會根據 Prefab 實例化一個新的。
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
            // 標記組件，以便 Recycle 時識別其所屬資料池
            var poolItem = obj.GetComponent<PoolItem>() ?? obj.AddComponent<PoolItem>();
            poolItem.prefabKey = key;
        }

        return obj;
    }

    /// <summary>
    /// 將使用完畢的物件回收至其對應的資料池中。
    /// </summary>
    public void Recycle(GameObject obj)
    {
        if (obj == null) return;

        var poolItem = obj.GetComponent<PoolItem>();
        if (poolItem != null && pools.ContainsKey(poolItem.prefabKey))
        {
            obj.SetActive(false);
            if (!pools[poolItem.prefabKey].Contains(obj))
            {
                pools[poolItem.prefabKey].Enqueue(obj);
            }
        }
        else
        {
            Destroy(obj);
        }
    }

    /// <summary>
    /// 靜態便捷方法，用於快速回收物件。
    /// </summary>
    public static void Return(GameObject obj)
    {
        if (Instance != null) Instance.Recycle(obj);
        else Destroy(obj);
    }
}

/// <summary>
/// 標記組件，存放物件池物品的識別碼。
/// </summary>
public class PoolItem : MonoBehaviour
{
    public int prefabKey;
}
