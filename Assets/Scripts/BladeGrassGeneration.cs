using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BladeGrassGeneration : MonoBehaviour
{
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
    private Vector3[] positionBufferData;
    private ComputeBuffer positionBuffer;

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
        computeShader.Dispatch(0, Mathf.CeilToInt(dimension / 8), Mathf.CeilToInt(dimension / 8), 1);
    }

    private void LateUpdate()
    {
        mat.SetBuffer("_Positions", positionBuffer);

        mat.SetFloat("_Height", height);
        mat.SetFloat("_Offset", curveOffset);
        mat.SetFloat("_SideOffsetAmount", sideOffsetAmount);
        mat.SetFloat("_ShadingOffset", shadingOffset);
        mat.SetFloat("_ShadingParameter", shadingParameter);
        Graphics.DrawMeshInstancedProcedural(mesh, 0, mat, new Bounds(Vector3.zero, new Vector3(100, 100, 100)), instanceCount);
    }

    private void InitializeComputeShader()
    {
        // Initialize data
        positionBufferData = new Vector3[instanceCount];

        // Initialize buffers
        positionBuffer = new ComputeBuffer(instanceCount, sizeof(float) * 3);

        // Set data to buffers
        positionBuffer.SetData(positionBufferData);

        // Set compute buffers to compute shader
        computeShader.SetBuffer(0, "_Positions", positionBuffer);

        // Run simulation step
        computeShader.SetInt("_Dimension", dimension);
        computeShader.SetVector("_PlacementOffset", offset);

        // Create new material for gpu instancing
        mat = new Material(shader);
        mat.SetBuffer("_Positions", positionBuffer);
    }

    private void OnDestroy()
    {
        positionBuffer.Release();
    }
}
