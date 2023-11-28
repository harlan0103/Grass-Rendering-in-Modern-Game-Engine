using UnityEngine;

public class MeshGenerator
{
    public static MeshData GenerateMeshData(float[,] heightMap, float heightMultiplier) {

        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        
        // Make the pivot point at the center
        float centerX = (width - 1) / -2f;
        float centerY = (height - 1) / -2f;

        int meshSimplificationIncrement = 1;
        int verticesPerLine = (width - 1) / meshSimplificationIncrement + 1;

        MeshData meshaData = new MeshData(verticesPerLine, verticesPerLine);
        int vertexIndex = 0;

        for (int y = 0; y < height; y += meshSimplificationIncrement) {
            for (int x = 0; x < width; x += meshSimplificationIncrement) {
                float vertexHeight = Mathf.Clamp01(heightMap[x, y]);
                meshaData.vertices[vertexIndex] = new Vector3(x + centerX, vertexHeight * heightMultiplier, y + centerY);
                meshaData.uvs[vertexIndex] = new Vector2(x / (float)width, y / (float)height);

                // Triangle range: (width - 1) * (height - 1)
                if (x < width - 1 && y < height - 1) {
                    meshaData.AddTriangles(vertexIndex + verticesPerLine, vertexIndex + verticesPerLine + 1, vertexIndex);
                    meshaData.AddTriangles(vertexIndex + 1, vertexIndex, vertexIndex + verticesPerLine + 1);
                }

                vertexIndex++;
            }
        }

        return meshaData;
    }
}

public class MeshData {
    // Need to have the mesh information
    public Vector3[] vertices;
    public Vector2[] uvs;
    public int[] triangles;

    int triangleIndex;

    public MeshData(int width, int height) {
        vertices = new Vector3[width * height];
        uvs = new Vector2[width * height];
        // We have (width - 1) * (height - 1) total points and each will have 2 triangles
        triangles = new int[(width - 1) * (height - 1) * 6];

        triangleIndex = 0;
    }

    // Add new triangle info
    public void AddTriangles(int a, int b, int c) {
        triangles[triangleIndex++] = a;
        triangles[triangleIndex++] = b;
        triangles[triangleIndex++] = c;
    }

    // Create a new Mesh
    public Mesh CreateMesh() {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();

        return mesh;
    }
}
