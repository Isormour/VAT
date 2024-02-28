using System;
using UnityEngine;

public class AnimatorVATIndirect : AnimatorVAT
{
    public Mesh mesh { private set; get; }
    public Material mat { private set; get; }
    public Transform owner { private set; get; }
    public bool enabled { private set; get; }
    public void SetEnabled(bool v)
    {
        enabled = v;
    }
    public AnimatorVATIndirect(MaterialPropertyBlock matProperties,Transform owner, AnimatorControllerVATIndirect animatorController): base(matProperties,null,animatorController) 
    {
        this.mesh = animatorController.Mesh;
        this.mat = animatorController.Mat;
        this.owner = owner;
        VATIndirectRenderer.Instance.AddObjectToRender<VATGroupRenderer.BasicInstancedParams>(this);
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
        float currentTime = currentState.VAT.TextureStartTime + animationTime;
        if (!inTransition && currentState.VAT.IsLooped && currentTime >= animEnd)
        {
            animationTime = 0;
            eventIndex = 0;
        }
    }
}