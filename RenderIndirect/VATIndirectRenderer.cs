using System.Collections.Generic;
using UnityEngine;

public class VATIndirectRenderer : MonoBehaviour
{
    Dictionary<Tuple<Mesh, Material>, VATGroupRenderer<TransformParam>> renderGroups;
    public static VATIndirectRenderer Instance;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddObjectToRender<T>(AnimatorVATIndirect vat)
    {
        Tuple<Mesh, Material> pair = new Tuple<Mesh, Material>(vat.mesh, vat.mat);
        if (!renderGroups.ContainsKey(pair))
        {
         VATGroupRenderer<TransformParam> groupRenderer = new VATGroupRenderer<TransformParam>(pair);
            renderGroups.Add(pair, groupRenderer);
            groupRenderer.SetParams = (animator) => 
            {
                TransformParam temp = new TransformParam();

                temp.tranformMatrix = Matrix4x4.TRS(animator.owner.localPosition, animator.owner.localRotation, animator.owner.localScale);

                return temp;
            };


        }
        renderGroups[pair].AddAnimator(vat);
    }
    private void Update()
    {
        List<Tuple<Mesh,Material>> renderKeys = new List<Tuple<Mesh,Material>>(renderGroups.Keys);
        for (int i = 0; i < renderKeys.Count; i++)
        {
            renderGroups[renderKeys[i]].DrawGroup();
        }
    }
    public struct TransformParam
    {
        public Matrix4x4 tranformMatrix;
    }

    class VATGroupRenderer<T>
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
            int size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(T));
            this.paramsBuffer = new ComputeBuffer(instances, size);
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

            for (int i = 0; i < instances; i++)
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
        }
        T[] CreateParamsArray()
        {
            T[] shaderParams = new T[instances];
            GameObject TempParent = new GameObject("Parent");
            for (int i = 0; i < instances; i++)
            {
                GameObject Temp = new GameObject("object " + i);
                shaderParams[i] = SetParams(objectsToRender[i]);
            }
            return shaderParams;
        }
    }
    public struct Tuple<T1, T2>
    {
        public readonly T1 Item1;
        public readonly T2 Item2;
        public Tuple(T1 item1, T2 item2) { Item1 = item1; Item2 = item2; }
    }

    public static class Tuple
    { // for type-inference goodness.
        public static Tuple<T1, T2> Create<T1, T2>(T1 item1, T2 item2)
        {
            return new Tuple<T1, T2>(item1, item2);
        }
    }
}
