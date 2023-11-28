using UnityEngine;

public static class Noise
{
    // Generate a noise map using perlin noise
    // based on input properties
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistence, float lacunarity, Vector2 offset) {
        // Create a new 2d array to hold noise value
        float[,] noiseMap = new float[mapWidth, mapHeight];

        float maxPossibleNoiseHeight = 0;
        float frequency = 1.0f;
        float amplitude = 1.0f;

        // Generate a random number based on seed we choose
        // We introduce octaves to change the wave shape at each point
        System.Random prng = new System.Random(seed);
        Vector2[] octavesOffset = new Vector2[octaves];
        for (int i = 0; i < octaves; i++) {
            // For each octave, we create a random offset point based on prng and input offset
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) - offset.y;
            octavesOffset[i] = new Vector2(offsetX, offsetY);

            maxPossibleNoiseHeight += amplitude;
            amplitude *= persistence;
        }

        // Make sure we have a valid scale
        if (scale <= 0) {
            scale = 0.0001f;
        }

        // Use max and min to track the scope
        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        // Calculate the center point
        float halfWidth = mapWidth / 2;
        float halfHeight = mapHeight / 2;

        // For each point of the map
        for (int y = 0; y < mapHeight; y++) {
            for (int x = 0; x < mapWidth; x++) {
                // We use persistenc to change frequency and use lacunarity to change amplitude
                frequency = 1.0f;
                amplitude = 1.0f;
                float noiseHeight = 0.0f;

                // For each point we apply the number of octaves
                for (int i = 0; i < octaves; i++) {
                    float sampleX = (x - halfWidth + octavesOffset[i].x) / scale * frequency;
                    float sampleY = (y - halfHeight + octavesOffset[i].y) / scale * frequency;

                    // Then generate the noise map
                    float perlinNoise = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    //float perlinNoise = PerlinNoise.GetPerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinNoise * amplitude;

                    // For each octaves
                    // We want to increase the frequency and
                    // decrease the amplitude
                    amplitude *= persistence;
                    frequency *= lacunarity;
                }
                // Update min and max noiseHeight map
                maxNoiseHeight = noiseHeight > maxNoiseHeight ? noiseHeight : maxNoiseHeight;
                minNoiseHeight = noiseHeight < minNoiseHeight ? noiseHeight : minNoiseHeight;

                // Apply noise height to the noise map
                noiseMap[x, y] = noiseHeight;
            }
        }

        // Normalize noise map value
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                // If noiseMap[x,y] == minNoiseHeight then return 0
                // If noiseMap[x,y] == maxNoiseHeight then return 1
                // If halfway then return 0.5
                noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);    
            }
        }

        return noiseMap;
    }
}
