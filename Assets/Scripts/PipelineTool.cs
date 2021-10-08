using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PipelineTool : MonoBehaviour
{
    public static int Threads = 16;

    public static a2v[] CreateMesh(float3[] vertexPosArray, float3[] colorArray, float3[] normalArray)
    {
        //数据检查
        if (vertexPosArray.Length <= 0 && colorArray.Length <= 0 && normalArray.Length <= 0)
            return new a2v[0];

        if (vertexPosArray.Length != colorArray.Length && vertexPosArray.Length != normalArray.Length)
            return new a2v[0];

        //结构体赋值
        a2v[] a2VArray = new a2v[vertexPosArray.Length];

        for(int i = 0; i < vertexPosArray.Length; i++)
        {
            a2VArray[i].vertex = vertexPosArray[i];
            a2VArray[i].color = colorArray[i];
            a2VArray[i].normal = normalArray[i];
        }
        return a2VArray;
    }

    public static int ComputeThreadGroupsNum(int particleNum, int threadNumPerGroup)
    {
        int groups = particleNum / threadNumPerGroup;
        if (particleNum % threadNumPerGroup != 0) groups++;

        return groups;
    }

    public static float Angle2Radian(float angle)
    {
        return Mathf.PI / 180 * angle;
    }

    public static Vector3 CalCrossProduct(Vector3 a, Vector3 b)
    {
        return new Vector3(a.y * b.z - b.y * a.z, a.z * b.x - b.z * a.x, a.x * b.y - b.x * a.y);
    }
}

public struct a2v
{
    public float3 vertex;
    public float3 color;
    public float3 normal;
}

public struct float3
{
    public float x;
    public float y;
    public float z;

    public static float3 operator +(float3 left, float3 right)
    {
        float3 result = new float3();
        result.x = left.x + right.x;
        result.y = left.y + right.y;
        result.z = left.z + right.z;
        return result;
    }

    public float3(float v1, float v2, float v3) : this()
    {
        this.x = v1;
        this.y = v2;
        this.z = v3;
    }
}