using System.Collections.Generic;
using UnityEngine;


public class VATIndirectRenderer : MonoBehaviour
{
    public Material material;
    public Mesh mesh;

    GraphicsBuffer commandBuf;
    GraphicsBuffer.IndirectDrawIndexedArgs[] commandData;
    ComputeBuffer paramsBuffer;
    int instances = 1024;
    public AnimatorVATTestIndirect[] owners;

    struct ShaderParams
    {
        public Matrix4x4 tranformMatrix;
        public float vATAnimationTime;
    }

    void Start()
    {
        instances = owners.Length;
        commandBuf = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, GraphicsBuffer.IndirectDrawIndexedArgs.size);
        commandData = new GraphicsBuffer.IndirectDrawIndexedArgs[1];
        int size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(ShaderParams));
        paramsBuffer = new ComputeBuffer(instances, size);
        ShaderParams[] shaderParams = new ShaderParams[owners.Length];
    }

    void OnDestroy()
    {
        commandBuf?.Release();
        commandBuf = null;
        paramsBuffer?.Release();
    }


    void Update()
    {
        ShaderParams[] shaderParams = new ShaderParams[instances];
        for (int i = 0; i < instances; i++)
        {
            shaderParams[i].tranformMatrix = Matrix4x4.TRS(owners[i].CashedTransform.localPosition, owners[i].CashedTransform.localRotation, owners[i].CashedTransform.localScale);
            shaderParams[i].vATAnimationTime = owners[i].VATTextureTime;
        }
        paramsBuffer.SetData(shaderParams);

        RenderParams rp = new RenderParams(material);

        rp.worldBounds = new Bounds(Vector3.zero, 10000 * Vector3.one); // use tighter bounds for better FOV culling
        rp.matProps = new MaterialPropertyBlock();

        rp.matProps.SetBuffer("_ParamsBuffer", paramsBuffer);

        for (int i = 0; i < 1; i++)
        {
            commandData[i].indexCountPerInstance = mesh.GetIndexCount(0);
            commandData[i].instanceCount = (uint)instances;
        }
        commandBuf.SetData(commandData);
        Graphics.RenderMeshIndirect(rp, mesh, commandBuf, 1);
    }
}
