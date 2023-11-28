using UnityEngine;
using System;
using System.IO;

public class MapGenerator : MonoBehaviour
{
    [Header("Generate terrain mesh")]
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    [Header("Terrain Properties")]
    public int terrainSize;
    public float noiseScale;
    public int octaves = 4;
    [Range(0, 1)] public float persistence = 0.5f;
    public float lacunarity = 2f;
    public int seed;
    public Vector2 offset;
    public float heightMultiplier = 1.0f;

    private void OnValidate()
    {
        octaves = Mathf.Max(0, octaves);    
    }

    // Draw map data in editor
    public void DrawMapInEditor() {
        MapData mapData = GenerateMap(Vector2.zero);

        // Geenrate a mesh based on data
        // Save generated texture into local folder
        Texture2D generatedTexture = TextureFromColorMap(mapData.colorMap, mapData.noiseMap);
        DrawMesh(MeshGenerator.GenerateMeshData(mapData.noiseMap, heightMultiplier), generatedTexture);

        // Save texture
        byte[] bytes = generatedTexture.EncodeToPNG();
        File.WriteAllBytes(Application.dataPath + "/Textures/terrainTex.png", bytes);
    }

    // Generate noise map first then save height data into a color info
    MapData GenerateMap(Vector2 centre) {
        float[,] noiseMap = Noise.GenerateNoiseMap(terrainSize, terrainSize, seed, noiseScale, octaves, persistence, lacunarity, centre + offset);

        // Generate color map
        Color[] colorMap = new Color[terrainSize * terrainSize];
        for (int y = 0; y < terrainSize; y++)
        {
            for (int x = 0; x < terrainSize; x++)
            {
                float height = noiseMap[x, y];
                height = Mathf.Clamp01(height);
                colorMap[y * terrainSize + x] = new Color(height, height, height);
            }
        }

        return new MapData(noiseMap, colorMap);
    }

    private Texture2D TextureFromColorMap(Color[] colorMap, float[,] heightMap)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        // Create a texutre
        Texture2D texture = new Texture2D(width, height);

        // Better visualization
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        texture.SetPixels(colorMap);
        texture.Apply();

        return texture;
    }

    public void DrawMesh(MeshData meshData, Texture2D texture)
    {
        // Set mesh and texture
        meshFilter.sharedMesh = meshData.CreateMesh();
        meshRenderer.sharedMaterial.mainTexture = texture;
    }
}
public class MapData
{
    public float[,] noiseMap;
    public Color[] colorMap;

    public MapData(float[,] noiseMap, Color[] colorMap)
    {
        this.noiseMap = noiseMap;
        this.colorMap = colorMap;
    }
}
