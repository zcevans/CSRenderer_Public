using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraConfig : MonoBehaviour
{
    private float aspect;
    private Matrix4x4 view2ClipMatrix;
    private Matrix4x4 testMatrix;

    public float nearPlane;
    public float farPlane;
    public float fieldOfView;

    public Vector3 cameraPos;
    public Vector3 cameraGazeDir;
    public Vector3 cameraUpDir;

    private Vector3 camOrthoBoundMin;
    private Vector3 camOrthoBoundMax;

    public Vector3 CameraGazeDirNormalized { get { return cameraGazeDir.normalized; } }
    public Vector3 CameraUpDirNormalized { get { return cameraUpDir.normalized; } }
    public Matrix4x4 View2ClipMatrix { get { return view2ClipMatrix; } }
    public Matrix4x4 TestMatrix { get { return testMatrix; } }
    public void SetCameraAspect(float width, float height)
    {
        aspect = width / height;
    }

    public void CalCameraOrthoBoundingBox()
    {
        float left, right, top, bottom, near, far;

        //Top & Button
        top =  nearPlane * Mathf.Tan(fieldOfView / 2 * Mathf.PI / 180);
        bottom = -top;

        //Left & Right
        left = -aspect * top;
        right = -left;

        //Near & Far
        near = nearPlane;
        far = farPlane;

        camOrthoBoundMin = new Vector3(left, bottom, near);
        camOrthoBoundMax = new Vector3(right, top, far);

        //camOrthoBoundMin += cameraPos;
        //camOrthoBoundMax += cameraPos;  //todo：相机移动？
    }

    public Matrix4x4 CalCameraTransMatrix()
    {
        //Moves Camera to Origin
        Matrix4x4 transMatrix = new Matrix4x4(
            new Vector4(1, 0, 0, 0),
            new Vector4(0, 1, 0, 0),
            new Vector4(0, 0, 1, 0),
            new Vector4(-cameraPos.x, -cameraPos.y, -cameraPos.z, 1));

        //由于是相机/观察空间是右手坐标系，因此适用右手叉乘定则
        Vector3 gazeCrossUp = PipelineTool.CalCrossProduct(CameraGazeDirNormalized, CameraUpDirNormalized);

        //CameraTransMatrix矩阵基于的是右手坐标系，而VertexBuffer中的值是基于的是左手坐标系，需要对z做一次翻转
        Matrix4x4 negativeZMatrix = new Matrix4x4
            (new Vector4(1, 0, 0, 0),
            new Vector4(0, 1, 0, 0),
            new Vector4(0, 0, -1, 0),
            new Vector4(0, 0, 0, 1));
         
        Matrix4x4 rotMatrix = new Matrix4x4(
            new Vector4(gazeCrossUp.x, CameraUpDirNormalized.x, -CameraGazeDirNormalized.x, 0),
            new Vector4(gazeCrossUp.y, CameraUpDirNormalized.y, -CameraGazeDirNormalized.y, 0),
            new Vector4(gazeCrossUp.z, CameraUpDirNormalized.z, -CameraGazeDirNormalized.z, 0),
            new Vector4(0, 0, 0, 1));

        return negativeZMatrix * rotMatrix * transMatrix;
    }

    public void CalPerspectiveMatrix()
    {
        Matrix4x4 zVerseMatrix = new Matrix4x4(
            new Vector4(1, 0, 0, 0),
            new Vector4(0, 1, 0, 0),
            new Vector4(0, 0, -1, 0),
            new Vector4(0, 0, 0, 1));

        Matrix4x4 perspMatrix = new Matrix4x4(
            new Vector4(nearPlane, 0, 0, 0),
            new Vector4(0, nearPlane, 0, 0),
            new Vector4(0, 0, nearPlane + farPlane, 1),
            new Vector4(0, 0, -nearPlane * farPlane, 0));

        //将“包围盒”移动到origin
        Matrix4x4 orthoTransMatrix = new Matrix4x4(
            new Vector4(1, 0, 0, 0),
            new Vector4(0, 1, 0, 0),
            new Vector4(0, 0, 1, 0),
            new Vector4(-(camOrthoBoundMin.x + camOrthoBoundMax.x) / 2, -(camOrthoBoundMin.y + camOrthoBoundMax.y) / 2, -(camOrthoBoundMin.z + camOrthoBoundMax.z) / 2, 1)
            );

        //包围盒规范化
        Matrix4x4 orthoScaleMatrix = new Matrix4x4(
            new Vector4(2 / (camOrthoBoundMax.x - camOrthoBoundMin.x), 0, 0, 0),
            new Vector4(0, 2 / (camOrthoBoundMax.y - camOrthoBoundMin.y), 0, 0),
            new Vector4(0, 0, 2 / (camOrthoBoundMax.z - camOrthoBoundMin.z), 0),
            new Vector4(0, 0, 0, 1)
            );

        Matrix4x4 orthoMatrix = orthoScaleMatrix * orthoTransMatrix;
        testMatrix = orthoMatrix;

        //view2ClipMatrix = orthoMatrix * perspMatrix * zVerseMatrix;

        view2ClipMatrix = perspMatrix * zVerseMatrix;
    }
}
