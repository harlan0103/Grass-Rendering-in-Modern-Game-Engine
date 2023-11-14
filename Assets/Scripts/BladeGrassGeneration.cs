using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BladeGrassGeneration : MonoBehaviour
{
    public Mesh mesh;
    public Shader shader;
    public ComputeShader computeShader;
    private Material mat;

    public int dimension;

    private int instanceCount;
    private Vector3[] positionBufferData;
    private ComputeBuffer positionBuffer;

    // Start is called before the first frame update
    void Start()
    {
        instanceCount = dimension * dimension;

        const int boundSize = 10;
        Vector2 offset = new Vector2(dimension / boundSize, dimension / boundSize);

        positionBufferData = new Vector3[instanceCount];
        positionBuffer = new ComputeBuffer(instanceCount, sizeof(float) * 3);
        positionBuffer.SetData(positionBufferData);
        computeShader.SetBuffer(0, "_Positions", positionBuffer);
        computeShader.SetInt("_Dimension", dimension);
        computeShader.SetVector("_PlacementOffset", offset);

        mat = new Material(shader);
        mat.SetBuffer("_Positions", positionBuffer);
    }

    // Update is called once per frame
    void Update()
    {
        computeShader.Dispatch(0, Mathf.CeilToInt(dimension / 8), Mathf.CeilToInt(dimension / 8), 1);
    }

    private void LateUpdate()
    {
        mat.SetBuffer("_Positions", positionBuffer);
        Graphics.DrawMeshInstancedProcedural(mesh, 0, mat, new Bounds(Vector3.zero, new Vector3(100, 100, 100)), instanceCount);
    }

    private void OnDestroy()
    {
        positionBuffer.Release();
    }
}
