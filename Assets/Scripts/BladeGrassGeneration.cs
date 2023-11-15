using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BladeGrassGeneration : MonoBehaviour
{
    public Camera mainCam;    

    [Header("Properties")]
    public int dimension;
    private Vector2 offset;
    public float height;
    public float curveOffset;
    public float sideOffsetAmount;
    public float distanceCullingThreshold;

    [Header("Instancing")]
    public Mesh mesh;
    public Shader shader;
    public ComputeShader computeShader;
    private Material mat;
    public float shadingOffset = 1.2f;
    public float shadingParameter = 1.4f;

    private int instanceCount;
    private Vector3[] positionBufferData;
    private int[] positionCntBufferData;
    private ComputeBuffer positionBuffer;
    private ComputeBuffer positionCntBuffer;
    
    [Header("Debugging")]
    public int numGrassRendered = 0;

    private Vector3 camPosInWorldSpace;

    // Start is called before the first frame update
    void Start()
    {
        instanceCount = dimension * dimension;
        Vector3 bounds = GetComponent<MeshRenderer>().bounds.size;
        offset = new Vector2(dimension / bounds.x, dimension / bounds.z);

        InitializeComputeShader();
    }

    // Update is called once per frame
    void Update()
    {
        RunSimulationStep();
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

    private void InitializeComputeShader()
    {
        // Initialize data
        positionBufferData = new Vector3[instanceCount];

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
        positionBuffer.SetCounterValue(0);

        // Get the updated camera position in world space
        camPosInWorldSpace = mainCam.transform.position;
        computeShader.SetVector("_CamPosInWorldSpace", camPosInWorldSpace);

        computeShader.SetFloat("_DistanceCullingThreshold", distanceCullingThreshold);

        computeShader.Dispatch(0, Mathf.CeilToInt(dimension / 8), Mathf.CeilToInt(dimension / 8), 1);

        // Update count number
        ComputeBuffer.CopyCount(positionBuffer, positionCntBuffer, 0);
        positionCntBuffer.GetData(positionCntBufferData);
        numGrassRendered = positionCntBufferData[0];
    }

    private void OnDestroy()
    {
        positionBuffer.Release();
        positionCntBuffer.Release();
    }
}
