using Unity.Burst;
using Unity.Mathematics;

namespace Game.Terrain
{
    [BurstCompile]
    public struct BurstNoise
    {
        public enum NoiseType : byte { Perlin = 0 }
        public enum FractalType : byte { FBM = 0, Billow = 1, Rigid = 2 }

        public NoiseType noiseType;
        public FractalType fractalType;
        public int seed;
        public float frequency;
        public int octaves;
        public float lacunarity;
        public float gain; // persistence

        // Create default
        public static BurstNoise Default(int seed = 1337)
        {
            return new BurstNoise
            {
                noiseType = NoiseType.Perlin,
                fractalType = FractalType.FBM,
                seed = seed,
                frequency = 0.01f,
                octaves = 4,
                lacunarity = 2f,
                gain = 0.5f
            };
        }

        public void SetNoiseType(NoiseType t) { noiseType = t; }
        public void SetFractalType(FractalType t) { fractalType = t; }
        public void SetFractalOctaves(int o) { octaves = math.max(1, o); }
        public void SetFractalGain(float g) { gain = g; }
        public void SetLacunarity(float l) { lacunarity = l; }
        public void SetFrequency(float f) { frequency = f; }

        // Sample 2D point (returns roughly in 0..1, depends on fractal type)
        public readonly float Sample(float2 p)
        {
            // apply base frequency
            float amplitude = 1f;
            float freq = frequency;
            float sum = 0f;
            float maxAmp = 0f;

            // seed-based offsets per octave for determinism
            // simple approach: offset each octave by a small pseudo-random derived from seed
            uint baseSeed = (uint)seed;

            for (int i = 0; i < octaves; i++)
            {
                float2 wp = p * freq + OctaveOffset(baseSeed, i);
                float n = 0f;
                if (noiseType == NoiseType.Perlin)
                    n = BurstPerlin.Perlin(wp.x, wp.y);

                switch (fractalType)
                {
                    case FractalType.FBM:
                        sum += n * amplitude;
                        break;
                    case FractalType.Billow:
                        sum += (2f * n - 1f) * amplitude; // make sign
                        break;
                    case FractalType.Rigid:
                        float v = 1f - math.abs(2f * n - 1f);
                        sum += v * amplitude;
                        break;
                }

                maxAmp += amplitude;
                amplitude *= gain;
                freq *= lacunarity;
            }

            float outv = sum / maxAmp;
            // FBM and others may be roughly in -1..1 or 0..1 depending. Normalize to 0..1 for simplicity
            // For FBM with Perlin in 0..1 and amplitudes summing to maxAmp -> outv in 0..1
            return math.clamp(outv, 0f, 1f);
        }

        private static float2 OctaveOffset(uint baseSeed, int octave)
        {
            // Deterministic per-octave offset using integer hashing
            uint h = baseSeed + (uint)octave * 0x9E3779B1u;
            h ^= h >> 16;
            h *= 0x7feb352du;
            h ^= h >> 15;
            float ox = (h & 0xFFFF) / (float)65535f * 10000f;
            float oz = ((h >> 16) & 0xFFFF) / (float)65535f * 10000f;
            return new float2(ox, oz);
        }
    }

    public struct NoiseLayer
    {
        public BurstNoise noise;
        public float3 offset; // can use xz
        public float scale;
        public float weight;
    }
}
