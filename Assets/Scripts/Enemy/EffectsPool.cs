using System.Collections.Generic;
using Game.Registries;
using UnityEngine;

namespace Game
{
    public class EffectsPool : MonoBehaviour
    {
        public static EffectsPool Instance;

        private readonly Vector3 poolGraveyardPosition = new(0f, -1000f, 0f);

        [SerializeField] private List<GameObject> effectPrefabs = new();
        [SerializeField] private int sizePerPool = 20;
        private readonly Dictionary<GameObject, Queue<ParticleSystem>> pools = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            foreach (GameObject effectPrefab in effectPrefabs)
            {
                Queue<ParticleSystem> particleSystems = new();

                for (int i = 0; i < sizePerPool; i++)
                {
                    ParticleSystem particleSystem = CreatePooledEffect(effectPrefab);
                    if (particleSystem == null) continue;

                    particleSystems.Enqueue(particleSystem);
                }

                pools[effectPrefab] = particleSystems;
            }
        }

        private ParticleSystem CreatePooledEffect(GameObject effect)
        {
            GameObject effectInstance = Instantiate(effect, poolGraveyardPosition, Quaternion.identity);
            effectInstance.SetActive(false);
            return effectInstance.GetComponent<ParticleSystem>();
        }

        public ParticleSystem GetParticleSystem(GameObject effect)
        {
            if (!pools.ContainsKey(effect))
            {
                Debug.LogWarning("No pool found for prefab: " + effect.name);
                return null;
            }

            Queue<ParticleSystem> particleSystems = pools[effect];

            if (particleSystems.Count == 0)
            {
                ParticleSystem newParticleSystem = CreatePooledEffect(effect);
                return newParticleSystem;
            }

            return particleSystems.Dequeue();
        }

        public void ReturnParticleSystem(ParticleSystem effect)
        {
            effect.Stop();
            effect.gameObject.SetActive(false);

            PrefabID id = effect.GetComponent<PrefabID>();
            GameObject prefab = PrefabRegistry.GetPrefabByKey(id.prefabKey);

            if (pools.TryGetValue(prefab, out Queue<ParticleSystem> pool))
                pool.Enqueue(effect);
        }
    }
}
