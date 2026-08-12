using System.Collections.Generic;
using Game.Saving;
using UnityEngine;

namespace Game.Terrain
{
    public class OreRockGroup : MonoBehaviour, ISaveableObject
    {
        [SerializeField] private List<GameObject> childRocks = new();

        private HashSet<int> removedIndices = new();

        private void Awake()
        {
            foreach (GameObject rock in childRocks)
            {
                if (!rock.activeSelf) continue;
                AdjustRocksToTerrain(rock);
            }
        }

        private void AdjustRocksToTerrain(GameObject rock)
        {
            Vector3 rockPos = rock.transform.position;

            int spawnPosX = Mathf.FloorToInt(rockPos.x);
            int spawnPosZ = Mathf.FloorToInt(rockPos.z);

            float height = Utility.GetHeightAt(spawnPosX, spawnPosZ);
            rockPos.y = height;
            rock.transform.position = rockPos;
        }

        public void RemoveRock(int index)
        {
            if (index < 0 || index >= childRocks.Count) return;

            childRocks[index].SetActive(false);
            removedIndices.Add(index);
        }

        public void ReleaseRocks()
        {
            foreach (GameObject rock in childRocks)
            {
                if (!rock.activeSelf) continue;
                rock.transform.SetParent(null);
            }
        }

        public string SaveState()
        {
            string json = JsonUtility.ToJson(new RockSaveData
            {
                removedRocks = new List<int>(removedIndices)
            });

            return json;
        }

        public void LoadState(string json)
        {
            Debug.Log($"LoadState called on ore");
            if (string.IsNullOrEmpty(json)) return;

            RockSaveData data = JsonUtility.FromJson<RockSaveData>(json);

            removedIndices = new HashSet<int>(data.removedRocks);

            foreach (int index in removedIndices)
            {
                if (index >= 0 && index < childRocks.Count)
                    childRocks[index].SetActive(false);
            }
        }
    }
}
