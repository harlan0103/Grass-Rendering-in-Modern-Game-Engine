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

public class BladeGrassGeneration : MonoBehaviour
{
    public Camera mainCam;
    [Header("Terrain")]

    // For now terrainChunkWidth will be the same as the terrain size
    // It will be way faster in one compute shader calculation
    //public int terrainChunkWidth = 40;   // Single terrain chunk size
    private int terrainChunkWidth;

    private List<TerrainChunk> terrainList;

    [Header("Properties")]
    public int dimension;
    private Vector2 offset;
    public float height;
    public float curveOffset;
    public float sideOffsetAmount;

    [Header("Instancing")]
    public Mesh mesh;
    public Shader shader;
    public ComputeShader computeShader;
    private Material mat;
    public float shadingOffset = 1.2f;
    public float shadingParameter = 1.4f;

    private int instanceCount;
    private int[] positionCntBufferData;
    private ComputeBuffer positionBuffer;
    private ComputeBuffer positionCntBuffer;

    [Header("Culling")]
    public float distanceCullingThreshold;
    public float frustumNearPlaneOffset;

    [Header("Debugging")]
    public int numGrassRendered = 0;

    private Vector3 camPosInWorldSpace;

    // Start is called before the first frame update
    void Start()
    {
        // Terrain calculation
        Vector3 terrainBounds = GetComponent<MeshRenderer>().bounds.size;
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
        Debug.Log(offset);
        InitializeComputeShader();
    }

    // Update is called once per frame
    void Update()
    {
        positionBuffer.SetCounterValue(0);
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
        mat.SetBuffer("_Positions", positionBuffer);

        mat.SetFloat("_Height", height);
        mat.SetFloat("_Offset", curveOffset);
        mat.SetFloat("_SideOffsetAmount", sideOffsetAmount);
        mat.SetFloat("_ShadingOffset", shadingOffset);
        mat.SetFloat("_ShadingParameter", shadingParameter);

        // Only render when there has grass in the scene
        // numGrassRendered = 0 will cause out of range error
        if (numGrassRendered > 0)
        {
            Graphics.DrawMeshInstancedProcedural(mesh, 0, mat, new Bounds(Vector3.zero, new Vector3(100, 100, 100)), numGrassRendered);
        }
    }

    private void OnDrawGizmos()
    {
        if (terrainList == null || terrainList.Count == 0)
        {
            return;
        }

        for (int i = 0; i < terrainList.Count; i++)
        {
            float hash1 = Mathf.PerlinNoise(i * 0.1f, 0);
            float hash2 = Mathf.PerlinNoise(0, i * 0.1f);
            float hash3 = Mathf.PerlinNoise(i * 0.1f, i * 0.1f);

            Gizmos.color = new Color(hash1, hash2, hash3);
            Gizmos.DrawWireCube(terrainList[i].centroid, new Vector3(terrainList[i].width, 0.5f, terrainList[i].width));
        }
    }

    private void InitializeComputeShader()
    {
        // Initialize buffers
        positionBuffer = new ComputeBuffer(instanceCount, sizeof(float) * 3, ComputeBufferType.Append);
        positionCntBuffer = new ComputeBuffer(1, sizeof(int), ComputeBufferType.Raw);
        positionCntBufferData = new int[1];

        // Set data to buffers
        //positionBuffer.SetData(positionBufferData);

        // Set compute buffers to compute shader
        computeShader.SetBuffer(0, "_Positions", positionBuffer);

        // Run simulation step
        computeShader.SetInt("_Dimension", dimension);
        computeShader.SetVector("_PlacementOffset", offset);

        // Create new material for gpu instancing
        mat = new Material(shader);
        mat.SetBuffer("_Positions", positionBuffer);
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

        computeShader.Dispatch(0, Mathf.CeilToInt(dimension / 8), Mathf.CeilToInt(dimension / 8), 1);

        // Update count number
        ComputeBuffer.CopyCount(positionBuffer, positionCntBuffer, 0);
        positionCntBuffer.GetData(positionCntBufferData);
        numGrassRendered += positionCntBufferData[0];
    }

    private void OnDestroy()
    {
        positionBuffer.Release();
        positionCntBuffer.Release();
    }
}
