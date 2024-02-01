using System.Collections.Generic;
using UnityEngine;

public class VATGroupRenderer<T>
{
    GraphicsBuffer commandBuf;
    GraphicsBuffer.IndirectDrawIndexedArgs[] commandData;
    ComputeBuffer paramsBuffer;
    int instances;

    public delegate T Setter(AnimatorVATIndirect VATAnimator);
    public Setter SetParams = null;
    Tuple<Mesh, Material> group;
    List<AnimatorVATIndirect> objectsToRender;
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
    }
    public void DrawGroup()
    {
        RenderParams rp = new RenderParams(group.Item2);

        rp.worldBounds = new Bounds(Vector3.zero, 10000 * Vector3.one); // use tighter bounds for better FOV culling
        rp.matProps = new MaterialPropertyBlock();
        paramsBuffer.SetData(CreateParamsArray());
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
        objectsToRender.Add(vat);
        instances = objectsToRender.Count;
        this.paramsBuffer?.Release();
        int size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
        this.paramsBuffer = new ComputeBuffer(instances, size);
    }
    T[] CreateParamsArray()
    {
        T[] shaderParams = new T[instances];
        for (int i = 0; i < instances; i++)
        {
            shaderParams[i] = SetParams(objectsToRender[i]);
        }
        return shaderParams;
    }
}
