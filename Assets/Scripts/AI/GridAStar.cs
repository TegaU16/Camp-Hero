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
                hCost = Heuristic(start, end)
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
                    int heightDiff = neighbor.y - current.position.y;

                    if (closedSet.Contains(neighbor)) continue;

                    if (isOccupied(neighbor)) continue;

                    int tentativeG = current.gCost + 1;

                    if (!openMap.TryGetValue(neighbor, out Node neighborNode))
                    {
                        neighborNode = new Node(neighbor)
                        {
                            gCost = tentativeG,
                            hCost = Heuristic(neighbor, end),
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

        private int Heuristic(Vector3Int a, Vector3Int b) => (int)Vector3Int.Distance(a, b);

        private IEnumerable<Vector3Int> GetNeighbors(Vector3Int pos)
        {
            foreach (Vector3Int dir in directions)
            {
                int nx = pos.x + dir.x;
                int nz = pos.z + dir.z;

                float surfaceY = getHeight(nx, 0, nz);
                int ny = Mathf.RoundToInt(surfaceY);

                int heightDiff = ny - pos.y;
                if (Mathf.Abs(heightDiff) > maxStepHeight) continue;

                Vector3Int neighbor = new(nx, ny, nz);

                Vector3 mid = ScaleVector3(pos + neighbor, 0.5f);
                Vector3Int midInt = Vector3Int.RoundToInt(mid);
                if (isOccupied(midInt)) continue;

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

        private Vector3 ScaleVector3(Vector3 vector, float scale)
        {
            Vector3 scaledVector = new(
                vector.x * scale,
                vector.y * scale,
                vector.z * scale);

            return scaledVector;
        }
    }
}
