using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;

public class AnimatorVAT : MonoBehaviour
{
    public StateToVAT[] states;
    public new MeshRenderer renderer;
    MaterialPropertyBlock materialBlock;
    float animationTime = 0;
    int currentStateIndex = 0;
    int eventIndex = 0;
    public delegate void AnimationVATEvent(string clipName, string eventName);
    public event AnimationVATEvent OnVATEvent;
    public void Start()
    {
        materialBlock = new MaterialPropertyBlock();
    }
    public void Update()
    {
        UpdateTime();
        CheckAnimationEvents();
    }
    public void Play(string name)
    {
        for (int i = 0; i < states.Length; i++)
        {
            if (states[i].StateName == name)
            {
                animationTime = 0;
                eventIndex = 0;
                currentStateIndex = i;
                materialBlock.SetTexture("VAT", states[i].VAT.VATTexture);
                renderer.SetPropertyBlock(materialBlock);
                break;
            }
        }
    }
    void UpdateTime()
    {
        animationTime += Time.deltaTime;
        if (states[currentStateIndex].VAT.IsLooped)
            if (animationTime >= states[currentStateIndex].VAT.Duration)
            {
                animationTime = 0;
                eventIndex = 0;
            }
        materialBlock.SetFloat("_AnimationTime", animationTime);
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
        if (states[currentStateIndex].VAT.Events[eventIndex].Time > animationTime)
        {
            OnVATEvent?.Invoke(states[currentStateIndex].StateName, states[currentStateIndex].VAT.Events[eventIndex].Name);
            Debug.Log("State "+ states[currentStateIndex].StateName+" event "+ states[currentStateIndex].VAT.Events[eventIndex].Name);
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
