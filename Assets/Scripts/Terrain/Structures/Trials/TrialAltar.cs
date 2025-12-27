using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Inventory;
using Game.Quests;
using Game.Registries;
using Game.Saving;
using UnityEngine;
using Worlds;

namespace Game.Terrain.Structures.Trials
{
    [System.Serializable]
    public class WaveEnemy
    {
        public GameObject enemyPrefab;
        public int count = 1;
    }

    [System.Serializable]
    public class TrialWave
    {
        public List<WaveEnemy> enemies = new();
    }

    public class TrialAltar : MonoBehaviour, IInteractable, ISaveableObject
    {
        public Transform[] spawnPoints;
        private List<TrialWave> waves = new();
        public Item key;

        [HideInInspector] public int currentWave = 0;
        [HideInInspector] public bool trialCompleted = false;
        [HideInInspector] public bool keyAvailable = false;
        private bool waveInProgress = false;
        private bool waveFail = false;

        [Header("Barrier Settings")]
        public bool useBarrier = true;
        public float barrierHalfSize = 15f;
        public float barrierHeight = 10f;
        public float barrierThickness = 0.5f;
        public string barrierLayerName = "TrialBarrier";
        public bool oneWayAllowInsideToExitOnly = false;

        [Header("Wave Settings")]
        [SerializeField] private int minWaves = 2;
        [SerializeField] private int maxWaves = 5;

        [SerializeField] private int minEnemiesPerWave = 2;
        [SerializeField] private int maxEnemiesPerWave = 6;

        [SerializeField] private int minCountPerEnemy = 1;
        [SerializeField] private int maxCountPerEnemy = 4;

        private GameObject barrierRoot;

        private void Start()
        {
            GenerateWaves();
            SetBarrierActive(false);
        }

        private void GenerateWaves()
        {
            if (waves != null && waves.Count > 0) return;

            waves = new List<TrialWave>();

            // Combine world seed + altar position for uniqueness
            int altarSeed = Utility.ConsistentHash(WorldSession.CurrentSeed) ^ transform.position.GetHashCode();
            System.Random rng = new(altarSeed);

            List<GameObject> enemyPrefabs =
                PrefabRegistry.GetPrefabsInCategory("Enemies");

            if (enemyPrefabs.Count == 0)
            {
                Debug.LogWarning("No enemy prefabs found for trial generation.");
                return;
            }

            int waveCount = rng.Next(minWaves, maxWaves + 1);

            for (int w = 0; w < waveCount; w++)
            {
                TrialWave wave = new();
                int enemiesInWave = rng.Next(minEnemiesPerWave, maxEnemiesPerWave + 1);

                for (int i = 0; i < enemiesInWave; i++)
                {
                    GameObject enemy = enemyPrefabs[rng.Next(enemyPrefabs.Count)];
                    int count = rng.Next(minCountPerEnemy, maxCountPerEnemy + 1);

                    wave.enemies.Add(new WaveEnemy
                    {
                        enemyPrefab = enemy,
                        count = count
                    });
                }

                waves.Add(wave);
            }
        }

        public void Interact()
        {
            if (keyAvailable)
            {
                CollectKey();
                return;
            }

            if (trialCompleted || waveInProgress) return;

            TrialMenu.Instance.Open(this);
        }

        public void StartTrialWave()
        {
            if (trialCompleted || waveInProgress) return;

            waveFail = false;
            StartCoroutine(StartWaveRoutine());
        }

        private IEnumerator StartWaveRoutine()
        {
            if (waveFail) yield break;

            waveInProgress = true;
            SetBarrierActive(true);

            TrialWave wave = waves[currentWave];

            foreach (WaveEnemy waveEnemy in wave.enemies)
            {
                if (waveEnemy.enemyPrefab == null || waveEnemy.count <= 0) continue;

                yield return StartCoroutine(SpawnWave(waveEnemy.enemyPrefab, waveEnemy.count));
            }

            yield return new WaitUntil(() => AreAllTrialEnemiesDead());

            waveInProgress = false;

            if (!waveFail)
                currentWave++;

            if (currentWave >= waves.Count)
            {
                GrantKey();
                yield break;
            }

            SetBarrierActive(false);
        }

        private IEnumerator SpawnWave(GameObject enemyPrefab, int count, float delayBetweenSpawns = 1.5f)
        {
            if (waveFail) yield break;
            if (enemyPrefab == null) yield break;
            if (spawnPoints == null || spawnPoints.Length == 0) yield break;
            if (TrialEnemyPool.Instance == null) yield break;

            List<Transform> shuffledSpawns = new(spawnPoints);
            for (int i = 0; i < shuffledSpawns.Count; i++)
            {
                int rand = Random.Range(i, shuffledSpawns.Count);
                (shuffledSpawns[i], shuffledSpawns[rand]) = (shuffledSpawns[rand], shuffledSpawns[i]);
            }

            for (int i = 0; i < count; i++)
            {
                Transform spawn;

                if (i < shuffledSpawns.Count)
                    spawn = shuffledSpawns[i];
                else
                    spawn = shuffledSpawns[Random.Range(0, shuffledSpawns.Count)];

                Vector3 finalPos = spawn.position;

                if (Physics.Raycast(spawn.position + Vector3.up * 50, Vector3.down, out RaycastHit hit, 100f, LayerMask.GetMask("Ground")))
                    finalPos = hit.point;

                TrialEnemyPool.Instance.GetEnemy(enemyPrefab, finalPos);

                yield return new WaitForSeconds(delayBetweenSpawns); // Delay between spawns
            }
        }

        private bool AreAllTrialEnemiesDead() => FindObjectsByType<TrialEnemyMarker>(FindObjectsSortMode.None).Length == 0;

        private void GrantKey()
        {
            trialCompleted = true;
            keyAvailable = true;
            SetBarrierActive(false);
            Debug.Log("Trial completed! Key is ready for collection.");
        }

        private void CollectKey()
        {
            if (key == null) return;
            if (InventoryManager.Instance.IsInventoryFullForItem(key)) return;

            InventoryManager.Instance.AddItem(key);

            keyAvailable = false;
            QuestManager.Instance.trialQuest.AddProgress();
        }

        public string GetInteractText()
        {
            if (keyAvailable) return "Collect Key";

            if (trialCompleted) return "Trial Completed";

            if (waveInProgress) return "Wave in Progress";

            return $"Start Wave {currentWave + 1}";
        }

        public Transform GetTransform() => transform;

        public List<WaveEnemy> GetEnemiesForWave(int waveIndex = -1)
        {
            // If waveIndex not specified, use current wave
            if (waveIndex < 0)
                waveIndex = currentWave;

            if (waves == null || waves.Count == 0 || waveIndex < 0 || waveIndex >= waves.Count) return null;

            TrialWave trialWave = waves[waveIndex];
            return trialWave.enemies;
        }

        public int GetNumberOfWaves() => waves.Count;

        private void EnsureBarrierBuilt()
        {
            if (!useBarrier || barrierRoot != null) return;

            barrierRoot = new GameObject($"TrialBarrier");
            barrierRoot.transform.SetParent(transform, worldPositionStays: false);
            barrierRoot.transform.localPosition = Vector3.zero;

            int barrierLayer = LayerMask.NameToLayer(barrierLayerName);
            if (barrierLayer < 0)
            {
                Debug.LogWarning($"Layer '{barrierLayerName}' not found. Using default layer for barrier.");
                barrierLayer = 0;
            }
            barrierRoot.layer = barrierLayer;

            // Create 4 walls (positive X, negative X, positive Z, negative Z)
            // Normal points inward (toward altar) so the OneWay script can know "which side is outside".
            CreateWall("Wall+X", new Vector3(barrierHalfSize, barrierHeight * 0.5f - 2f, 0f),
                       new Vector3(barrierThickness, barrierHeight, barrierHalfSize * 2f),
                       Vector3.left, barrierLayer);

            CreateWall("Wall-X", new Vector3(-barrierHalfSize, barrierHeight * 0.5f - 2f, 0f),
                       new Vector3(barrierThickness, barrierHeight, barrierHalfSize * 2f),
                       Vector3.right, barrierLayer);

            CreateWall("Wall+Z", new Vector3(0f, barrierHeight * 0.5f - 2f, barrierHalfSize),
                       new Vector3(barrierHalfSize * 2f, barrierHeight, barrierThickness),
                       Vector3.back, barrierLayer);

            CreateWall("Wall-Z", new Vector3(0f, barrierHeight * 0.5f - 2f, -barrierHalfSize),
                       new Vector3(barrierHalfSize * 2f, barrierHeight, barrierThickness),
                       Vector3.forward, barrierLayer);
        }

        private void CreateWall(string name, Vector3 localPos, Vector3 size, Vector3 inwardNormal, int layer)
        {
            GameObject wall = new(name)
            {
                layer = layer
            };
            wall.transform.SetParent(barrierRoot.transform, false);
            wall.transform.localPosition = localPos;

            BoxCollider col = wall.AddComponent<BoxCollider>();
            col.isTrigger = false;
            col.size = size;

            MeshFilter mf = wall.AddComponent<MeshFilter>();

            if (Mathf.Approximately(size.z, barrierThickness))
                mf.mesh = BuildQuadMesh(size.x, size.y, Vector3.forward);
            else
                mf.mesh = BuildQuadMesh(size.z, size.y, Vector3.right);

            MeshRenderer mr = wall.AddComponent<MeshRenderer>();
            mr.material = BarrierMaterial.Instance.GetMaterial();

            if (oneWayAllowInsideToExitOnly)
            {
                OneWayBarrier ow = wall.AddComponent<OneWayBarrier>();
                ow.inwardNormal = inwardNormal;
                ow.allowInsideToExitOnly = true;
                ow.solidCollider = col;
            }
        }

        private Mesh BuildQuadMesh(float width, float height, Vector3 normal)
        {
            Mesh mesh = new();

            Vector3 right, up;

            if (normal == Vector3.forward || normal == Vector3.back)
            {
                right = Vector3.right;
                up = Vector3.up;
            }
            else if (normal == Vector3.right || normal == Vector3.left)
            {
                right = Vector3.forward;
                up = Vector3.up;
            }
            else
            {
                right = Vector3.right;
                up = Vector3.up;
            }

            mesh.vertices = new Vector3[]
            {
                (-right * width/2) + (-up * height/2),
                ( right * width/2) + (-up * height/2),
                (-right * width/2) + ( up * height/2),
                ( right * width/2) + ( up * height/2)
            };

            mesh.uv = new Vector2[]
            {
                new(0,0), new(1,0),
                new(0,1), new(1,1)
            };

            mesh.triangles = new int[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateNormals();

            return mesh;
        }

        private void SetBarrierActive(bool active)
        {
            if (!useBarrier) return;

            EnsureBarrierBuilt();
            barrierRoot.SetActive(active);
        }

        public void FailTrial()
        {
            if (barrierRoot != null)
                barrierRoot.SetActive(false);

            foreach (GameObject trialEnemy in TrialEnemyPool.Instance.activeEnemies.ToList())
                TrialEnemyPool.Instance.ReturnTrialEnemy(trialEnemy);

            waveFail = true;
        }

        // Save / Load
        public string SaveState()
        {
            TrialAltarSaveData data = new()
            {
                currentWave = currentWave,
                trialCompleted = trialCompleted,
                keyAvailable = keyAvailable
            };

            return JsonUtility.ToJson(data);
        }

        public void LoadState(string json)
        {
            TrialAltarSaveData data = JsonUtility.FromJson<TrialAltarSaveData>(json);

            GenerateWaves();

            if (data != null)
            {
                currentWave = data.currentWave;
                trialCompleted = data.trialCompleted;
                keyAvailable = data.keyAvailable;
            }
        }

        // Expose state for menu
        public int GetCurrentWave() => currentWave;
        public bool IsCompleted() => trialCompleted;
        public bool IsWaveInProgress() => waveInProgress;
    }
}
