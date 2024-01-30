using System;
using UnityEngine;

public class AnimatorVATIndirect : AnimatorVAT
{
    public Mesh mesh { private set; get; }
    public Material mat { private set; get; }
    public Transform owner { private set; get; }
    public AnimatorVATIndirect(MaterialPropertyBlock matProperties,Transform owner,Material mat,Mesh mesh, AnimatorControllerVAT animatorController): base(matProperties,null,animatorController) 
    {
        this.mesh = mesh;
        this.mat = mat;
        this.owner = owner;
       // VATIndirectRenderer.Instance.AddObjectToRender<VATIndirectRenderer.TransformParam>(this);
    }
    protected override void SetRenderer(MeshRenderer rend)
    {

    }
    protected override void ApplyPropertyBlock()
    {
        
    }
    protected override void UpdateCurrentState()
    {
        float animEnd = currentState.VAT.TextureEndTime;
        float currentTime = currentState.VAT.TextureStartTime + textureTime;
        if (!inTransition && currentState.VAT.IsLooped && currentTime >= animEnd)
        {
            textureTime = 0;
            eventIndex = 0;
        }
    }
}