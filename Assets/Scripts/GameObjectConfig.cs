using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameObjectConfig : MonoBehaviour
{
    private Matrix4x4 model2WorldMatrix;
    //Object in Scene
    public Vector3 cubeWorldPos;
    public Vector3 cubeWorldRot;
    public Vector3 cubeWorldScale;

    public Matrix4x4 Model2WorldMatrix { get { return this.model2WorldMatrix; } }
    public void UpdateModel2WorldMatrix()
    {
        Matrix4x4 scaleMatrix = new Matrix4x4(
            new Vector4(cubeWorldScale.x, 0, 0, 0),
            new Vector4(0, cubeWorldScale.y, 0, 0),
            new Vector4(0, 0, cubeWorldScale.z, 0),
            new Vector4(0, 0, 0, 1));

        float thetaX = PipelineTool.Angle2Radian(cubeWorldRot.x);
        float thetaY = PipelineTool.Angle2Radian(cubeWorldRot.y);
        float thetaZ = PipelineTool.Angle2Radian(cubeWorldRot.z);

        Matrix4x4 xRotateMatrix = new Matrix4x4(
            new Vector4(1, 0, 0, 0),
            new Vector4(0, Mathf.Cos(thetaX), Mathf.Sin(thetaX), 0),
            new Vector4(0, -Mathf.Sin(thetaX), Mathf.Cos(thetaX), 0),
            new Vector4(0, 0, 0, 1));
        Matrix4x4 yRotateMatrix = new Matrix4x4(
            new Vector4(Mathf.Cos(thetaY), 0, -Mathf.Sin(thetaY), 0),
            new Vector4(0, 1, 0, 0),
            new Vector4(Mathf.Sin(thetaY), 0, Mathf.Cos(thetaY), 0),
            new Vector4(0, 0, 0, 1));
        Matrix4x4 zRotateMatrix = new Matrix4x4(
            new Vector4(Mathf.Cos(thetaZ), Mathf.Sin(thetaZ), 0, 0),
            new Vector4(-Mathf.Sin(thetaZ), Mathf.Cos(thetaZ), 0, 0),
            new Vector4(0, 0, 1, 0),
            new Vector4(0, 0, 0, 1));

        Matrix4x4 transMatrix = new Matrix4x4(
            new Vector4(1, 0, 0, 0),
            new Vector4(0, 1, 0, 0),
            new Vector4(0, 0, 1, 0),
            new Vector4(cubeWorldPos.x, cubeWorldPos.y, cubeWorldPos.z, 1));

        //Debug.Log(cubeWorldPos.z);

        //旋转先y后x再z，因此旋转矩阵最终是z*x*y
        https://blog.csdn.net/yhx956058885/article/details/108579270
        model2WorldMatrix = transMatrix * zRotateMatrix * xRotateMatrix * yRotateMatrix * scaleMatrix;
    }
}
