using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public abstract class ObjectPool<T> : MonoBehaviour where T : Component
    {
        public static ObjectPool<T> Instance;

        [SerializeField] protected Vector3 poolGraveyardPosition = new(0f, -1000f, 0f);
        [SerializeField] protected int initialPoolSize = 10;

        protected readonly Queue<T> pool = new();
        protected readonly HashSet<T> activeObjects = new();

        protected virtual void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        protected virtual void Start()
        {
            for (int i = 0; i < initialPoolSize; i++)
            {
                T obj = CreatePooledObject();
                if (obj != null)
                    pool.Enqueue(obj);
            }
        }

        protected abstract T CreatePooledObject();

        protected virtual void OnGetObject(T obj)
        {
            obj.gameObject.SetActive(true);
            activeObjects.Add(obj);
        }

        protected virtual void OnReturnObject(T obj)
        {
            obj.gameObject.SetActive(false);
            obj.transform.position = poolGraveyardPosition;
            activeObjects.Remove(obj);
            pool.Enqueue(obj);
        }

        public T Get()
        {
            T obj = pool.Count > 0 ? pool.Dequeue() : CreatePooledObject();
            OnGetObject(obj);
            return obj;
        }

        public void Return(T obj) => OnReturnObject(obj);
    }
}
