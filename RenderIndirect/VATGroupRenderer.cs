using System;
using System.Collections.Generic;
using UnityEngine;

public class VATGroupRenderer 
{
    GraphicsBuffer commandBuf;
    GraphicsBuffer.IndirectDrawIndexedArgs[] commandData;
    ComputeBuffer paramsBuffer;
    protected int instances;
    Tuple<Mesh, Material> group;
    protected List<AnimatorVATIndirect> objectsToRender;
    const int RenderInstancesCount = 1024;
    public delegate void BufferSetter(ComputeBuffer buffer, List<AnimatorVATIndirect> objectsToRender,int instanceCount);
    BufferSetter bufferParamSetter;
    public struct BasicInstancedParams
    {
        public Matrix4x4 transformMatrix;
        public float animationTime;
    }
    public VATGroupRenderer(Tuple<Mesh, Material> group,Type paramType, BufferSetter bufferSetter)
    {
        this.group = group;
        this.instances = 0;
        this.commandBuf = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        this.commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
        objectsToRender = new List<AnimatorVATIndirect>();
        int paramStructSize = GetStructSize(paramType);
        this.paramsBuffer = new ComputeBuffer(RenderInstancesCount, paramStructSize);
        this.bufferParamSetter = bufferSetter;
    }
    int GetStructSize(Type paramType)
    {
        return System.Runtime.InteropServices.Marshal.SizeOf(paramType);
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
        bufferParamSetter(paramsBuffer,objectsToRender, instances);
        rp.matProps.SetBuffer("_ParamsBuffer", paramsBuffer);

        commandData[0].indexCountPerInstance = group.Item1.GetIndexCount(0);
        commandData[0].instanceCount = (uint)instances;
      
        commandBuf.SetData(commandData);
        Graphics.RenderMeshIndirect(rp, group.Item1, commandBuf, 1);
    }

    internal void AddAnimator(AnimatorVATIndirect vat)
    {
        objectsToRender.Add(vat);
        instances = objectsToRender.Count;
    }
}
