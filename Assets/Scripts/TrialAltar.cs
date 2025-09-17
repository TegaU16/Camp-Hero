using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrialAltar : MonoBehaviour, IInteractable
{
    public string altarID;  // Unique ID for saving/loading (can auto-generate)
    public GameObject[] enemyWavePrefabs;
    public Transform[] spawnPoints;
    public int[] enemiesPerWave = { 3, 5, 7 };
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

    private GameObject barrierRoot;

    private void Start()
    {
        LoadAltarState();
        SetBarrierActive(false);
    }

    public void Interact()
    {
        if (keyAvailable)
        {
            CollectKey();
            return;
        }

        if (trialCompleted || waveInProgress)
            return;

        TrialMenu.Instance.Open(this);
    }

    public void StartTrialWave()
    {
        if (trialCompleted || waveInProgress)
            return;

        waveFail = false;
        StartCoroutine(StartWaveRoutine());
    }

    private IEnumerator StartWaveRoutine()
    {
        waveInProgress = true;
        SetBarrierActive(true);

        GameObject enemyPrefab = enemyWavePrefabs[currentWave];
        int count = enemiesPerWave.Length > currentWave ? enemiesPerWave[currentWave] : 3;

        yield return StartCoroutine(SpawnWave(enemyPrefab, count));

        yield return new WaitUntil(() => AreAllTrialEnemiesDead());

        waveInProgress = false;

        if (!waveFail) currentWave++;

        SaveAltarState();
        SetBarrierActive(false);

        if (currentWave >= enemyWavePrefabs.Length)
        {
            GrantKey();
        }
    }

    private IEnumerator SpawnWave(GameObject enemyPrefab, int count, float delayBetweenSpawns = 1.5f)
    {
        if (enemyPrefab == null)
        {
            Debug.LogError($"TrialAltar '{altarID}': enemyPrefab is null for wave {currentWave}!");
            yield break;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError($"TrialAltar '{altarID}': No spawn points assigned!");
            yield break;
        }

        if (TrialEnemyPool.Instance == null)
        {
            Debug.LogError($"TrialAltar '{altarID}': TrialEnemyPool.Instance is null!");
            yield break;
        }

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
            {
                finalPos = hit.point;
            }

            TrialEnemyPool.Instance.GetEnemy(enemyPrefab, finalPos);

            yield return new WaitForSeconds(delayBetweenSpawns); // Delay between spawns
        }
    }

    private bool AreAllTrialEnemiesDead()
    {
        return FindObjectsByType<TrialEnemyMarker>(FindObjectsSortMode.None).Length == 0;
    }

    private void GrantKey()
    {
        trialCompleted = true;
        keyAvailable = true;
        SetBarrierActive(false);
        Debug.Log("Trial completed! Key is ready for collection.");

        SaveAltarState();
    }

    private void CollectKey()
    {
        if (key == null) return;

        if (InventoryManager.Instance.IsInventoryFullForItem(key))
        {
            return;
        }

        InventoryManager.Instance.AddItem(key);

        keyAvailable = false;
        Debug.Log("Key collected!");

        SaveAltarState();
    }

    public string GetInteractText()
    {
        if (keyAvailable)
            return "Collect Key";

        if (trialCompleted)
            return "Trial Completed";

        if (waveInProgress)
            return "Wave in Progress";

        return $"Start Wave {currentWave + 1}";
    }

    public Transform GetTransform()
    {
        return transform;
    }

    public Enemy GetEnemyForWave(int waveIndex = -1)
    {
        // If waveIndex not specified, use current wave
        if (waveIndex < 0)
            waveIndex = currentWave;

        if (enemyWavePrefabs == null || enemyWavePrefabs.Length == 0)
        {
            Debug.LogError($"TrialAltar '{altarID}': No enemy prefabs assigned!");
            return null;
        }

        if (waveIndex < 0 || waveIndex >= enemyWavePrefabs.Length)
        {
            Debug.LogError($"TrialAltar '{altarID}': Invalid wave index {waveIndex}!");
            return null;
        }

        GameObject prefab = enemyWavePrefabs[waveIndex];
        if (prefab == null)
        {
            Debug.LogError($"TrialAltar '{altarID}': Enemy prefab for wave {waveIndex} is null!");
            return null;
        }

        if (!prefab.TryGetComponent(out Enemy enemy))
        {
            Debug.LogError($"TrialAltar '{altarID}': Enemy prefab for wave {waveIndex} has no Enemy component!");
            return null;
        }

        return enemy;
    }

    private void EnsureBarrierBuilt()
    {
        if (!useBarrier || barrierRoot != null) return;

        barrierRoot = new GameObject($"TrialBarrier_{altarID}");
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
        mr.material = BarrierMaterial.Instance.Get();

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
        {
            TrialEnemyPool.Instance.ReturnEnemyToPool(trialEnemy);
        }

        waveFail = true;
    }

    // Save / Load
    public void SaveAltarState()
    {
        TrialAltarSaveData data = new()
        {
            currentWave = currentWave,
            trialCompleted = trialCompleted,
            keyAvailable = keyAvailable
        };

        SaveSystem.SaveTrialAltarState(GameManager.Instance.currentWorldName, altarID, data);
    }

    private void LoadAltarState()
    {
        TrialAltarSaveData data = SaveSystem.LoadTrialAltarState(GameManager.Instance.currentWorldName, altarID);
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
