using System.Collections.Generic;
using UnityEngine;
using System;

namespace Game.AI
{
    public class GridAStar
    {
        public class Node : IHeapItem<Node>
        {
            public Vector3Int position;
            public int gCost;
            public int hCost;
            public Node parent;

            public int FCost => gCost + hCost;
            public int HeapIndex { get; set; }
            public Node(Vector3Int pos) => position = pos;
            public int CompareTo(Node other) => FCost.CompareTo(other.FCost);
        }

        private readonly Func<int, int, int, float> getHeight;
        private readonly Func<Vector3Int, bool> isOccupied;
        private readonly int maxStepHeight;

        private readonly Dictionary<Vector2Int, int> heightCache = new();
        private readonly Dictionary<Vector3Int, bool> occupancyCache = new();

        private static readonly Vector3Int[] directions = {
            new(1,0,0), new(-1,0,0),
            new(0,0,1), new(0,0,-1),
            new(1,0,1), new(-1,0,1),
            new(1,0,-1), new(-1,0,-1)
        };

        public GridAStar(Func<int, int, int, float> getHeight, Func<Vector3Int, bool> isOccupied, int maxStepHeight = 1)
        {
            this.getHeight = getHeight;
            this.isOccupied = isOccupied;
            this.maxStepHeight = maxStepHeight;
        }

        public List<Vector3Int> FindPath(Vector3Int start, Vector3Int end)
        {
            MinHeap<Node> openSet = new();
            Dictionary<Vector3Int, Node> openMap = new();
            HashSet<Vector3Int> closedSet = new();

            Node startNode = new(start)
            {
                gCost = 0,
                hCost = Utility.ManhattanDistance(start, end)
            };
            openSet.Add(startNode);
            openMap[start] = startNode;

            int iteration = 0;

            while (openSet.Count > 0)
            {
                iteration++;
                Node current = openSet.Pop();
                openMap.Remove(current.position);

                if (iteration > 10000)
                {
                    Debug.LogError($"[A* Debug] Iteration limit reached! Start: {start}, End: {end}");
                    return null;
                }

                if (current.position == end) return Retrace(current);

                closedSet.Add(current.position);

                foreach (Vector3Int neighbor in GetNeighbors(current.position))
                {
                    if (closedSet.Contains(neighbor)) continue;
                    if (IsOccupiedCached(neighbor)) continue;

                    int tentativeG = current.gCost + 1;

                    if (!openMap.TryGetValue(neighbor, out Node neighborNode))
                    {
                        neighborNode = new Node(neighbor)
                        {
                            gCost = tentativeG,
                            hCost = Utility.ManhattanDistance(neighbor, end),
                            parent = current
                        };
                        openSet.Add(neighborNode);
                        openMap[neighbor] = neighborNode;
                    }
                    else if (tentativeG < neighborNode.gCost)
                    {
                        neighborNode.gCost = tentativeG;
                        neighborNode.parent = current;
                        openSet.UpdateItem(neighborNode);
                    }
                }
            }

            return null;
        }

        private IEnumerable<Vector3Int> GetNeighbors(Vector3Int pos)
        {
            foreach (Vector3Int dir in directions)
            {
                int nx = pos.x + dir.x;
                int nz = pos.z + dir.z;

                int ny = GetCachedHeight(nx, nz);

                int heightDiff = ny - pos.y;
                if (Mathf.Abs(heightDiff) > maxStepHeight) continue;

                Vector3Int neighbor = new(nx, ny, nz);

                Vector3Int mid = new(
                    (pos.x + neighbor.x) / 2,
                    (pos.y + neighbor.y) / 2,
                    (pos.z + neighbor.z) / 2
                );

                if (IsOccupiedCached(mid)) continue;

                yield return neighbor;
            }
        }

        private List<Vector3Int> Retrace(Node endNode)
        {
            List<Vector3Int> path = new();
            Node current = endNode;
            while (current != null)
            {
                path.Add(current.position);
                current = current.parent;
            }

            path.Reverse();
            return path;
        }

        private int GetCachedHeight(int x, int z)
        {
            Vector2Int key = new(x, z);
            if (heightCache.TryGetValue(key, out int h)) return h;

            h = Mathf.RoundToInt(getHeight(x, 0, z));
            heightCache[key] = h;

            return h;
        }

        private bool IsOccupiedCached(Vector3Int pos)
        {
            if (occupancyCache.TryGetValue(pos, out bool occupied)) return occupied;

            occupied = isOccupied(pos);
            occupancyCache[pos] = occupied;
            return occupied;
        }
    }
}
