using System;
using System.Collections.Generic;
using UnityEngine;

public class VATGroupRenderer : IRenderStruct
{
    GraphicsBuffer commandBuf;
    GraphicsBuffer.IndirectDrawIndexedArgs[] commandData;
    ComputeBuffer paramsBuffer;
    int instances;
    Tuple<Mesh, Material> group;
    List<AnimatorVATIndirect> objectsToRender;

   public struct BasicInstancedParams
    {
        public Matrix4x4 transformMatrix;
        public float animationTime;
    }
    public VATGroupRenderer(Tuple<Mesh, Material> group)
    {
        this.group = group;
        this.instances = 0;
        this.commandBuf = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        this.commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
        objectsToRender = new List<AnimatorVATIndirect>();
    }

    public void Deinitialize()
    {
        commandBuf?.Release();
        commandBuf = null;
        paramsBuffer?.Release();
        paramsBuffer = null;
    }
    public void DrawGroup()
    {
        RenderParams rp = new RenderParams(group.Item2);

        rp.worldBounds = new Bounds(Vector3.zero, 10000 * Vector3.one); // use tighter bounds for better FOV culling
        rp.matProps = new MaterialPropertyBlock();
        SetParamsBufferData(paramsBuffer);
        rp.matProps.SetBuffer("_ParamsBuffer", paramsBuffer);

        for (int i = 0; i < 1; i++)
        {
            commandData[i].indexCountPerInstance = group.Item1.GetIndexCount(0);
            commandData[i].instanceCount = (uint)instances;
        }
        commandBuf.SetData(commandData);
        Graphics.RenderMeshIndirect(rp, group.Item1, commandBuf, 1);
    }

    internal void AddAnimator(AnimatorVATIndirect vat)
    {
        this.paramsBuffer?.Release();
        objectsToRender.Add(vat);
        instances = objectsToRender.Count;
        int size = GetStructSize();
        this.paramsBuffer = new ComputeBuffer(instances, size);
    }

    public void SetParamsBufferData(UnityEngine.ComputeBuffer buffer)
    {
        BasicInstancedParams[] objParams = new BasicInstancedParams[instances];
        for (int i = 0; i < objParams.Length; i++)
        {
            objParams[i]= new BasicInstancedParams();
            objParams[i].animationTime = objectsToRender[i].textureTime;
            objParams[i].transformMatrix = GetMatrixFromTransform(objectsToRender[i].owner);
        }
        buffer.SetData(objParams);
    }
    public int GetStructSize()
    {
        return System.Runtime.InteropServices.Marshal.SizeOf(typeof(BasicInstancedParams));
    }
    Matrix4x4 GetMatrixFromTransform(Transform t)
    {
        return Matrix4x4.TRS(t.localPosition, t.rotation, t.localScale);
    }

}
