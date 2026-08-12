using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Game.Terrain
{
    [System.Serializable]
    public class OreDeposit
    {
        public OreType oreType;
        public Vector3 center;
        public float radius;
        public int seed;
        public List<Vector3> orePositions;

        public OreDeposit(
            OreType oreType,
            Vector3 center,
            float radius,
            int seed,
            List<Vector3> orePositions)
        {
            this.oreType = oreType;
            this.center = center;
            this.radius = radius;
            this.seed = seed;
            this.orePositions = orePositions;
        }
    }
}
