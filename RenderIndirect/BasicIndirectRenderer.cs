using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicIndirectRenderer : VATIndirectRenderer
{
    [SerializeField] RendererGroup basicGroup;
    protected override void Awake()
    {
        base.Awake();
        Type structType = typeof(VATGroupRenderer.BasicInstancedParams);
        AddParamStructGroup(structType,basicGroup.mesh, basicGroup.mat, BasicBufferSetter);
    }
    void BasicBufferSetter(ComputeBuffer buffer,List<AnimatorVATIndirect> objectsToRender,int instanceCount)
    {
        VATGroupRenderer.BasicInstancedParams[] objParams = new VATGroupRenderer.BasicInstancedParams[instanceCount];
        for (int i = 0; i < instanceCount; i++)
        {
            objParams[i] = new VATGroupRenderer.BasicInstancedParams();
            if (i >= objectsToRender.Count)
                continue;
            if (objectsToRender[i].enabled)
            {
                objParams[i].transformMatrix = objectsToRender[i].GetMatrixFromTransform();
                objParams[i].animationTime = objectsToRender[i].animationTime;
            }
        }
        buffer.SetData(objParams);
    
    }
    [System.Serializable]
    public struct RendererGroup
    {
        public Material mat;
        public Mesh mesh;
    }
}
