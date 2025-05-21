using System.Collections.Generic;
using UnityEngine;

public class ColliderPool : MonoBehaviour
{
    private readonly Queue<GameObject> pool = new();

    public GameObject GetColliderObject(Transform parent)
    {
        GameObject obj;
        if (pool.Count > 0)
        {
            obj = pool.Dequeue();
            obj.SetActive(true);
        }
        else
        {
            obj = new GameObject("PooledCollider");
            obj.AddComponent<BoxCollider>();
        }
        obj.transform.parent = parent;
        return obj;
    }

    public void ReturnColliderObject(GameObject obj)
    {
        obj.SetActive(false);
        obj.transform.parent = null;
        pool.Enqueue(obj);
    }
}
