using Unity.Burst;
using Unity.Mathematics;

namespace Game.Terrain
{
    [BurstCompile]
    public static class BurstPerlin
    {
        private static readonly float2[] gradients = new float2[]
        {
        new(1,0), new(-1,0),
        new(0,1), new(0,-1),
        new(0.70710678f,0.70710678f), new(-0.70710678f,0.70710678f),
        new(0.70710678f,-0.70710678f), new(-0.70710678f,-0.70710678f)
        };

        private static float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);
        private static float Lerp(float a, float b, float t) => a + t * (b - a);

        private static int Hash(int x, int y)
        {
            uint h = (uint)(x * 374761393 + y * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;

            return (int)(h & 7u);
        }

        private static float Grad(int hash, float x, float y)
        {
            float2 g = gradients[hash];
            return g.x * x + g.y * y;
        }

        [BurstCompile]
        public static float Perlin(float x, float y)
        {
            int xi = (int)math.floor(x);
            int yi = (int)math.floor(y);

            float xf = x - xi;
            float yf = y - yi;

            float u = Fade(xf);
            float v = Fade(yf);

            int aa = Hash(xi, yi);
            int ba = Hash(xi + 1, yi);
            int ab = Hash(xi, yi + 1);
            int bb = Hash(xi + 1, yi + 1);

            float x1 = Lerp(Grad(aa, xf, yf), Grad(ba, xf - 1f, yf), u);
            float x2 = Lerp(Grad(ab, xf, yf - 1f), Grad(bb, xf - 1f, yf - 1f), u);

            float result = Lerp(x1, x2, v);
            return (result + 1f) * 0.5f; // normalize to [0,1]
        }
    }
}
