using UnityEngine;


public class AnimatorVAT
{
    public StateToVAT[] states;
    public MeshRenderer renderer;
    MaterialPropertyBlock materialBlock;
    float animationTime = 0;
    int currentStateIndex = 0;
    int eventIndex = 0;
    public float SpeedMultiplier = 1;
    public delegate void AnimationVATEvent(string clipName, string eventName);
    public event AnimationVATEvent OnVATEvent;
    public AnimatorVAT(MaterialPropertyBlock matBlock, MeshRenderer renderer, StateToVAT[] states)
    {
        materialBlock = matBlock;
        this.renderer = renderer;
        this.states = states;
    }

    public void Update(float deltaTime)
    {
        UpdateTime(deltaTime);
        CheckAnimationEvents();
    }
    public void Play(string name)
    {
        if (states[currentStateIndex].StateName == name)
        {
            return;
        }
        for (int i = 0; i < states.Length; i++)
        {
            if (states[i].StateName == name)
            {
                animationTime = 0;
                eventIndex = 0;
                currentStateIndex = i;
                materialBlock.SetTexture("_VATAnimationTexture", states[i].VAT.VATTexture);
                materialBlock.SetTexture("_VATNormalTexture", states[i].VAT.VATNormal);
                materialBlock.SetTexture("_VATTangentTexture", states[i].VAT.VATTangent);
                renderer.SetPropertyBlock(materialBlock);
                break;
            }
        }
    }
    void UpdateTime(float deltaTime)
    {
        float animationSpeed = states[currentStateIndex].VAT.AnimationSpeed;
        animationTime += deltaTime * SpeedMultiplier * animationSpeed;
        if (states[currentStateIndex].VAT.IsLooped)
            if (animationTime >= states[currentStateIndex].VAT.Duration)
            {
                animationTime = 0;
                eventIndex = 0;
            }
        materialBlock.SetFloat("_VATAnimationTime", animationTime);
        renderer.SetPropertyBlock(materialBlock);
    }
    void CheckAnimationEvents()
    {
        if (states[currentStateIndex].VAT.Events.Length <= 0)
        {
            return;
        }
        if (eventIndex >= states[currentStateIndex].VAT.Events.Length)
        {
            return;
        }
        if (states[currentStateIndex].VAT.Events[eventIndex].Time < animationTime)
        {
            OnVATEvent?.Invoke(states[currentStateIndex].StateName, states[currentStateIndex].VAT.Events[eventIndex].Name);
            eventIndex++;
        }

    }
}
[System.Serializable]
public struct StateToVAT
{
    public string StateName;
    public AnimationVAT VAT;
}
