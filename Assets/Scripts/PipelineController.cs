using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PipelineController : MonoBehaviour, IDisposable
{
    //General Setting
    public int pixelWidth = 1920;
    public int pixelHeight = 1080;  //todo：并入Config

    private GameObjectConfig gameObjectConfig;
    private CameraConfig cameraConfig;

    //Mesh Data
    List<a2v[]> meshDataList = new List<a2v[]>();

    //Temporary vertex data after geometric phase

    private Vector3[] vertices;
    private int[] triangles;

    #region Append Buffer
    public ComputeBuffer fragBuffer;
    public ComputeBuffer zBuffer;
    public ComputeBuffer frameBuffer;
    #endregion

    #region Compute Buffer

    //Application phase
    public ComputeBuffer vertexBuffer;      private int vertexBufferCount;
    public ComputeBuffer triangleBuffer;    private int triangleBufferCount;
    public ComputeBuffer screenPosBuffer;
    public ComputeBuffer vertexDepthBuffer; //投影矩阵计算时使用

    #endregion
    public ComputeBuffer countBuffer;
    #region

    #endregion

    #region Texture2D
    public RenderTexture frameTex;
    public RenderTexture depthTex;
    #endregion

    #region Compute Shader

    //Application phase
    public ComputeShader GpuCullingCS;

    public ComputeShader ClearBufferCS;

    //Geometry phase
    public ComputeShader Local2ClipCS;

    public ComputeShader NormalizedDeviceCoordCS;

    public ComputeShader ScreenMappingCS;

    //Rasterization phase
    public ComputeShader TriangleTraversalCS;

    //Fragment shader 
    public ComputeShader PerFragOperationCS;

    #endregion

    public ShowFinalFrameBuffer showFinalFrame;

    // Start is called before the first frame update
    void Start()
    {
        cameraConfig = this.GetComponent<CameraConfig>();
        cameraConfig.SetCameraAspect(pixelWidth, pixelHeight);
        cameraConfig.CalCameraOrthoBoundingBox();
        cameraConfig.CalPerspectiveMatrix();

        gameObjectConfig = this.GetComponent<GameObjectConfig>();

        //Debug.Log(PipelineTool.CalCrossProduct(new Vector3(1, 2, 3), new Vector3(4, 5, 6)));
        //1.Setup Camera's params

        //2.Init Mesh and Texture

        ////获取视锥体6个面  
        //Plane[] planes = new Plane[6];
        //planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);   //左、右、下、上、近、远, 储存法线和到原点的距离


        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Cube);

        //获取物体包围盒
        vertices = quad.GetComponent<MeshFilter>().mesh.vertices;
        vertexBufferCount = vertices.Length;
        //vertices[0] = new Vector3(0, 2, 4);

        triangles = quad.GetComponent<MeshFilter>().mesh.triangles;
        triangleBufferCount = triangles.Length;

        vertexDepthBuffer = new ComputeBuffer(vertexBufferCount, sizeof(float));

        screenPosBuffer = new ComputeBuffer(vertexBufferCount, sizeof(float) * 3);    //temp
        //vertexDepthBuffer = new ComputeBuffer(vertexBufferCount, sizeof(float));
         
        Destroy(quad);  //工具人使命完成

        frameTex = new RenderTexture(pixelWidth, pixelHeight, 24);
        frameTex.enableRandomWrite = true;
        frameTex.Create();

        depthTex = new RenderTexture(pixelWidth, pixelHeight, 24);
        depthTex.enableRandomWrite = true;
        depthTex.Create();
        //Bounds cubeBound = GeometryUtility.CalculateBounds(vertices, cube.transform.localToWorldMatrix);

        ////测试物体是否与视锥体相交
        //bool isCollide = GeometryUtility.TestPlanesAABB(planes, cubeBound);

        //InitMeshData();
    }

    // Update is called once per frame
    void Update()
    {
        gameObjectConfig.UpdateModel2WorldMatrix();

        vertexBuffer = new ComputeBuffer(vertexBufferCount, sizeof(float) * 3);
        vertexBuffer.SetData(vertices);

        triangleBuffer = new ComputeBuffer(triangleBufferCount, sizeof(int));
        triangleBuffer.SetData(triangles);

        if (ClearBufferCS)
        {
            int clearBufferKernel = ClearBufferCS.FindKernel("ClearBuffer");

            ClearBufferCS.SetTexture(clearBufferKernel, "frameTex", frameTex);
            ClearBufferCS.SetTexture(clearBufferKernel, "depthTex", depthTex);

            ClearBufferCS.Dispatch(clearBufferKernel, pixelWidth / PipelineTool.Threads, pixelWidth / PipelineTool.Threads, 1);
        }

        #region Geometry Phase
        //1.Culling 优先
        //1.1 Calculating Bound
        //1.2 Culling

        //int cullingKernel = GpuCullingCS.FindKernel("Culling");

        //GpuCullingCS.SetBuffer(cullingKernel, "meshBuffer", meshBufferList[i]);

        //int groups = PipelineTool.ComputeThreadGroupsNum(meshDataList[i].Length, PipelineTool.Threads);
        //GpuCullingCS.Dispatch(cullingKernel, groups, 1, 1);

        //2.Model Translation
        if (Local2ClipCS)
        {
            # region Local2World
            int local2WorldKernel = Local2ClipCS.FindKernel("Local2World");

            Local2ClipCS.SetInt("vertexBufferCount", vertexBufferCount);
            Local2ClipCS.SetMatrix("Model2WorldMatrix", gameObjectConfig.Model2WorldMatrix);

            Local2ClipCS.SetBuffer(local2WorldKernel, "VertexBuffer", vertexBuffer);

            int local2WorldGroup = PipelineTool.ComputeThreadGroupsNum(vertexBufferCount, PipelineTool.Threads);
            Local2ClipCS.Dispatch(local2WorldKernel, local2WorldGroup, 1, 1);
            #endregion

            Vector3[] temp1 = new Vector3[vertexBufferCount];
            vertexBuffer.GetData(temp1);

            #region World2View
            int world2ViewKernel = Local2ClipCS.FindKernel("World2View");

            Local2ClipCS.SetInt("vertexBufferCount", vertexBufferCount);
            Local2ClipCS.SetMatrix("CameraTransMatrix", cameraConfig.CalCameraTransMatrix());

            Local2ClipCS.SetBuffer(world2ViewKernel, "VertexBuffer", vertexBuffer);

            int world2ViewGroup = PipelineTool.ComputeThreadGroupsNum(vertexBufferCount, PipelineTool.Threads);
            Local2ClipCS.Dispatch(world2ViewKernel, local2WorldGroup, 1, 1);

            //Vector3[] temp2 = new Vector3[4];
            //vertexBuffer.GetData(temp2);
            #endregion

            #region View2Clip
            int view2ClipKernel = Local2ClipCS.FindKernel("View2Clip");

            Local2ClipCS.SetInt("vertexBufferCount", vertexBufferCount);
            Local2ClipCS.SetMatrix("View2ClipMatrix", cameraConfig.View2ClipMatrix);
            Local2ClipCS.SetMatrix("TestMatrix", cameraConfig.TestMatrix);

            Local2ClipCS.SetBuffer(view2ClipKernel, "VertexBuffer", vertexBuffer);
            Local2ClipCS.SetBuffer(view2ClipKernel, "VertexDepthBuffer", vertexDepthBuffer); 

            int view2ClipGroup = PipelineTool.ComputeThreadGroupsNum(vertexBufferCount, PipelineTool.Threads);
            Local2ClipCS.Dispatch(view2ClipKernel, view2ClipGroup, 1, 1);
            #endregion

            Vector3[] temp = new Vector3[vertexBufferCount];
            vertexBuffer.GetData(temp);

            float[] aaa = new float[vertexBufferCount];
            vertexDepthBuffer.GetData(aaa);
        }

        if (NormalizedDeviceCoordCS)
        {
            int NDCKernel = NormalizedDeviceCoordCS.FindKernel("NormalizedDeviceCoordinates");

            NormalizedDeviceCoordCS.SetInt("vertexBufferCount", vertexBufferCount);

            NormalizedDeviceCoordCS.SetBuffer(NDCKernel, "VertexBuffer", vertexBuffer);
            NormalizedDeviceCoordCS.SetBuffer(NDCKernel, "VertexDepthBuffer", vertexDepthBuffer);

            int NDCGroup = PipelineTool.ComputeThreadGroupsNum(vertexBufferCount, PipelineTool.Threads);
            NormalizedDeviceCoordCS.Dispatch(NDCKernel, NDCGroup, 1, 1);

            Vector3[] temp = new Vector3[vertexBufferCount];
            vertexBuffer.GetData(temp);
        }

        //5.Clipping

        //6.NDC

        //Screen Mapping
        if (ScreenMappingCS)
        {
            int mappingKernel = ScreenMappingCS.FindKernel("Mapping");
            ScreenMappingCS.SetInt("vertexCount", vertexBufferCount);
            ScreenMappingCS.SetInt("pixelWidth", this.pixelWidth);
            ScreenMappingCS.SetInt("pixelHeight", this.pixelHeight);

            ScreenMappingCS.SetBuffer(mappingKernel, "VertexBuffer", vertexBuffer);
            ScreenMappingCS.SetBuffer(mappingKernel, "VertexDepthBuffer", vertexDepthBuffer);
            ScreenMappingCS.SetBuffer(mappingKernel, "ScreenPosBuffer", screenPosBuffer);

            //Vector3[] temp1 = new Vector3[4];
            //vertexBuffer.GetData(temp1);


            int mappingGroup = PipelineTool.ComputeThreadGroupsNum(vertexBufferCount, PipelineTool.Threads);
            ScreenMappingCS.Dispatch(mappingKernel, mappingGroup, 1, 1);

            Vector3[] temp = new Vector3[vertexBufferCount];
            screenPosBuffer.GetData(temp);
            float[] temp2 = new float[vertexBufferCount];
            vertexDepthBuffer.GetData(temp2);

        }
        #endregion

        //Triangle Traversal
        if (TriangleTraversalCS)
        {
            int traversalKernel = TriangleTraversalCS.FindKernel("Traversal");

            TriangleTraversalCS.SetInt("vertexCount", vertexBufferCount);
            TriangleTraversalCS.SetInt("triangleBufferCount", triangleBufferCount);

            fragBuffer = new ComputeBuffer(102400, sizeof(float) * 3, ComputeBufferType.Append); //todo
            fragBuffer.SetCounterValue(0);

            zBuffer = new ComputeBuffer(102400, sizeof(float) * 3, ComputeBufferType.Append); //todo
            zBuffer.SetCounterValue(0);

            frameBuffer = new ComputeBuffer(102400, sizeof(float) * 4, ComputeBufferType.Append); //todo
            frameBuffer.SetCounterValue(0);

            TriangleTraversalCS.SetBuffer(traversalKernel, "zBuffer", zBuffer);
            TriangleTraversalCS.SetBuffer(traversalKernel, "frameBuffer", frameBuffer);
            TriangleTraversalCS.SetBuffer(traversalKernel, "ScreenPosBuffer", screenPosBuffer);
            TriangleTraversalCS.SetBuffer(traversalKernel, "TriangleBuffer", triangleBuffer);
            TriangleTraversalCS.SetBuffer(traversalKernel, "fragBuffer", fragBuffer);

            int traversalGroup = PipelineTool.ComputeThreadGroupsNum(triangleBufferCount / 3, PipelineTool.Threads);
            TriangleTraversalCS.Dispatch(traversalKernel, traversalGroup, 1, 1);
        }

        //var countBuffer1 = new ComputeBuffer(2, sizeof(int), ComputeBufferType.IndirectArguments);
        //ComputeBuffer.CopyCount(fragBuffer, countBuffer1, 0 * sizeof(int));  //0:fragBuffer length   1:zBuffer length
        //ComputeBuffer.CopyCount(zBuffer, countBuffer1, 1 * sizeof(int));

        //Vector4[] aaa = new Vector4[100];
        //colorBuffer.GetData(aaa);
        //Debug.Log(aaa[0]);

        //countBuffer1.Release();

        if (PerFragOperationCS)
        {
            //reference: https://zhuanlan.zhihu.com/p/113482286

            int fragKernel = PerFragOperationCS.FindKernel("Frag");

            PerFragOperationCS.SetTexture(fragKernel, "frameTex", frameTex);
            PerFragOperationCS.SetTexture(fragKernel, "depthTex", depthTex);

            PerFragOperationCS.SetBuffer(fragKernel, "fragBuffer", fragBuffer);
            PerFragOperationCS.SetBuffer(fragKernel, "zBuffer", zBuffer);
            PerFragOperationCS.SetBuffer(fragKernel, "frameBuffer", frameBuffer);

            countBuffer = new ComputeBuffer(2, sizeof(int), ComputeBufferType.IndirectArguments);
            ComputeBuffer.CopyCount(fragBuffer, countBuffer, 0 * sizeof(int));  //0:fragBuffer length   1:zBuffer length
            ComputeBuffer.CopyCount(zBuffer, countBuffer, 1 * sizeof(int));

            int[] aaa = new int[2];
            countBuffer.GetData(aaa);
            Debug.Log(aaa[1]);

            PerFragOperationCS.SetBuffer(fragKernel, "countBuffer", countBuffer);

            int fragGroup = PipelineTool.ComputeThreadGroupsNum(10000, PipelineTool.Threads);    //can not get the length to CPU except GetData API
            PerFragOperationCS.Dispatch(fragKernel, fragGroup, 1, 1);
        }

        //Draw https://blog.csdn.net/wodownload2/article/details/104406137

        showFinalFrame.OnUpdate(frameTex);

        //Dispose temp buffer data
        countBuffer.Release();
        vertexBuffer.Dispose();
        triangleBuffer.Dispose();
        fragBuffer.Dispose();
        zBuffer.Dispose();
        frameBuffer.Dispose();
    }


    public void Dispose()
    {
        throw new NotImplementedException();
    }

    void InitMeshData()
    {

        //Square (inside of view frustum)

        float3 squareOffset = new float3(0f, 0f, 0f);
        float3[] vertexArray = { new float3(-1, 1, 1), new float3(1, 1, 1), new float3(1, -1, 1),
                                 new float3(-1, 1, 1), new float3(1, -1, 1), new float3(-1, -1, 1) };

        float3[] colorArray = { new float3(1, 0, 0), new float3(1, 0, 0), new float3(1, 0, 0),
                                new float3(1, 0, 0), new float3(1, 0, 0), new float3(1, 0, 0) };

        float3[] normalArray = { new float3(0, 0, -1), new float3(0, 0, -1), new float3(0, 0, -1),
                                 new float3(0, 0, -1), new float3(0, 0, -1), new float3(0, 0, -1) };

        for(int i = 0; i < vertexArray.Length; i++)
        {
            vertexArray[i] += squareOffset;
            colorArray[i] += squareOffset;
            normalArray[i] += squareOffset;
        }

        a2v[] squareMesh = PipelineTool.CreateMesh(vertexArray, colorArray, normalArray);

        if (squareMesh.Length > 0)
            meshDataList.Add(squareMesh);
        else
            Debug.LogError("meshData is invalid");

        //ComputeBuffer cb = new ComputeBuffer(meshDataList[meshBufferList.Count + 1].Length, (sizeof(float) * 3) * 3);
        //cb.SetData(meshDataList[meshBufferList.Count + 1]);
        //meshBufferList.Add(cb);

        //Triangle (fully outside of view frustum)


        //Triangle (partly outside of view frustum)
    }

}
