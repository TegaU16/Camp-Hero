using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public abstract class MultiObjectPool<T> : MonoBehaviour where T : Component
    {
        public static MultiObjectPool<T> Instance;

        [SerializeField] protected Vector3 poolGraveyardPosition = new(0, -1000, 0);

        protected readonly Dictionary<GameObject, Queue<T>> pools = new();
        protected readonly Dictionary<GameObject, HashSet<T>> activeObjects = new();

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        protected abstract T CreatePooledObject(GameObject prefab);

        protected virtual void OnGetObject(T obj) => obj.gameObject.SetActive(true);

        protected virtual void OnReturnObject(T obj)
        {
            obj.gameObject.SetActive(false);
            obj.transform.position = poolGraveyardPosition;
        }

        public T Get(GameObject prefab, bool isPoolStatic = true)
        {
            if (!pools.ContainsKey(prefab))
                pools[prefab] = new Queue<T>();

            Queue<T> queue = pools[prefab];
            if (queue.Count == 0 && isPoolStatic) return null;

            T obj = queue.Count > 0 ? queue.Dequeue() : CreatePooledObject(prefab);

            if (!activeObjects.ContainsKey(prefab))
                activeObjects[prefab] = new HashSet<T>();

            activeObjects[prefab].Add(obj);

            OnGetObject(obj);
            return obj;
        }

        public void Return(T obj, GameObject prefab)
        {
            OnReturnObject(obj);

            if (!pools.ContainsKey(prefab))
                pools[prefab] = new Queue<T>();

            pools[prefab].Enqueue(obj);

            if (activeObjects.ContainsKey(prefab))
                activeObjects[prefab].Remove(obj);
        }
    }
}
