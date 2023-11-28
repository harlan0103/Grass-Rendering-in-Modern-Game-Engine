using System.Collections.Generic;
using UnityEngine;


struct TerrainChunk 
{
    public Vector3 centroid;
    public Vector3 bottomLeftCornorPt;
    public int width;

    public TerrainChunk(Vector3 _centroid, Vector3 _bottomLeftCorner, int _width)
    { 
        centroid = _centroid;
        bottomLeftCornorPt = _bottomLeftCorner;
        width = _width;
    }
}

[System.Serializable]
public struct Blade
{
    public Vector3 position;
    public float windOffset;
}

public class BladeGrassGeneration : MonoBehaviour
{
    public Camera mainCam;
    [Header("Terrain Properties")]
    public GameObject terrain;
    public Texture2D heightMap;
    public float heightMultiplier;
    public float heightMapScale;

    // For now terrainChunkWidth will be the same as the terrain size
    // It will be way faster in one compute shader calculation
    //public int terrainChunkWidth = 40;   // Single terrain chunk size
    private int terrainChunkWidth;

    private List<TerrainChunk> terrainList;

    [Header("Blade Grass Properties")]
    public int dimension;
    private Vector2 offset;
    public float height;
    public float curveOffset;
    public float sideOffsetAmount;
    public Texture windTex;

    [Header("Instancing")]
    public Mesh mesh;
    public Shader shader;
    public ComputeShader computeShader;
    private Material mat;
    public float shadingOffset = 1.2f;
    public float shadingParameter = 1.4f;
    public float windStrength = 1.2f;
    public float noiseOffset = -0.5f;
    public Vector3 windDirection = Vector3.one;

    private int instanceCount;
    private int[] bladeCntBufferData;
    private ComputeBuffer bladeBuffer;
    private ComputeBuffer bladeCntBuffer;
    private ComputeBuffer heightMapBuffer;

    [Header("Culling")]
    public float distanceCullingThreshold;
    public float frustumNearPlaneOffset;
    public float frustumEdgeOffset;

    [Header("Debugging")]
    public int numGrassRendered = 0;

    private Vector3 camPosInWorldSpace;

    // Start is called before the first frame update
    void Start()
    {
        // Terrain calculation
        Vector3 terrainBounds = terrain.GetComponent<MeshRenderer>().bounds.size;
        terrainChunkWidth = (int)terrainBounds.x;       // TEMP
        int numOfChunkOnEachSide = (int)terrainBounds.x / terrainChunkWidth;
        int terrainSideSize = numOfChunkOnEachSide * terrainChunkWidth;
        int totalChunksNumber = numOfChunkOnEachSide * numOfChunkOnEachSide;

        Vector3 initialCenterPos = Vector3.zero;
        Vector3 startBottomLeftCornerPos = initialCenterPos - new Vector3(terrainSideSize / 2, 0.0f, terrainSideSize / 2);

        // Initialize terrain list
        terrainList = new List<TerrainChunk>();

        // Add terrains into the list
        for (int i = 0; i < numOfChunkOnEachSide; i++)
        {
            for (int j = 0; j < numOfChunkOnEachSide; j++)
            {
                Vector3 leftCornerPos = startBottomLeftCornerPos + new Vector3(i * terrainChunkWidth, 0.0f, j * terrainChunkWidth);
                Vector3 centerPos = leftCornerPos + new Vector3(terrainChunkWidth / 2, 0.0f, terrainChunkWidth / 2);

                TerrainChunk chunk = new TerrainChunk(centerPos, leftCornerPos, terrainChunkWidth);
                terrainList.Add(chunk);
            }
        }

        // Calculate attributes for compute shader
        instanceCount = dimension * dimension * totalChunksNumber;
        //Vector3 bounds = GetComponent<MeshRenderer>().bounds.size;
        offset = new Vector2((float)dimension / terrainChunkWidth, (float)dimension / terrainChunkWidth);
        InitializeComputeShader();
    }

    // Update is called once per frame
    void Update()
    {
        bladeBuffer.SetCounterValue(0);
        numGrassRendered = 0;

        // For now there will be only one chunk in the chunk list
        // Since calculating one chunk is faster
        foreach (TerrainChunk chunk in terrainList)
        {
            computeShader.SetVector("_InitialPos", chunk.bottomLeftCornorPt);

            RunSimulationStep();
        }
    }

    private void LateUpdate()
    {
        mat.SetBuffer("_BladeBuffer", bladeBuffer);

        mat.SetFloat("_Height", height);
        mat.SetFloat("_Offset", curveOffset);
        mat.SetFloat("_SideOffsetAmount", sideOffsetAmount);
        mat.SetFloat("_ShadingOffset", shadingOffset);
        mat.SetFloat("_ShadingParameter", shadingParameter);

        mat.SetFloat("_WindSpeed", windStrength);
        mat.SetVector("_WindDirection", windDirection);
        mat.SetTexture("_MainTex", windTex);
        mat.SetFloat("_NoiseOffset", noiseOffset);

        // Only render when there has grass in the scene
        // numGrassRendered = 0 will cause out of range error
        if (numGrassRendered > 0)
        {
            Graphics.DrawMeshInstancedProcedural(mesh, 0, mat, new Bounds(Vector3.zero, new Vector3(100, 100, 100)), numGrassRendered);
        }
    }

    private void InitializeComputeShader()
    {
        // Initialize buffers
        bladeBuffer = new ComputeBuffer(instanceCount, sizeof(float) * 4, ComputeBufferType.Append);
        bladeCntBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        bladeCntBufferData = new int[1];

        // Set compute buffers to compute shader
        computeShader.SetBuffer(0, "_BladeGrassBuffer", bladeBuffer);

        // Run simulation step
        computeShader.SetInt("_Dimension", dimension);
        computeShader.SetVector("_PlacementOffset", offset);

        // Create new material for gpu instancing
        mat = new Material(shader);
        mat.SetBuffer("_BladeBuffer", bladeBuffer);
    }

    private void RunSimulationStep()
    {
        // Get the updated camera position in world space
        camPosInWorldSpace = mainCam.transform.position;

        // Get camera GPU adjuested clip space
        Matrix4x4 projectionMatrix = GL.GetGPUProjectionMatrix(mainCam.projectionMatrix, false);
        Matrix4x4 adjuestedClippingMatrix = projectionMatrix * mainCam.worldToCameraMatrix;

        computeShader.SetVector("_CamPosInWorldSpace", camPosInWorldSpace);
        computeShader.SetMatrix("_CamClippingMatrix", adjuestedClippingMatrix);
        computeShader.SetFloat("_DistanceCullingThreshold", distanceCullingThreshold);
        computeShader.SetFloat("_NearPlaneOffset", frustumNearPlaneOffset);
        computeShader.SetFloat("_EdgeFrustumCullingOffset", frustumEdgeOffset);
        computeShader.SetFloat("_HeightMultiplier", heightMultiplier);
        computeShader.SetFloat("_HeightMapSize", heightMapScale);
        computeShader.SetTexture(0, "WindTex", windTex);
        computeShader.SetTexture(0, "HeightTex", heightMap);
        computeShader.SetVector("_Time", Shader.GetGlobalVector("_Time"));

        computeShader.Dispatch(0, Mathf.CeilToInt(dimension / 8), Mathf.CeilToInt(dimension / 8), 1);

        // Update count number
        ComputeBuffer.CopyCount(bladeBuffer, bladeCntBuffer, 0);
        bladeCntBuffer.GetData(bladeCntBufferData);
        numGrassRendered += bladeCntBufferData[0];
    }

    private void OnDestroy()
    {
        bladeBuffer.Release();
        bladeCntBuffer.Release();
    }
}
