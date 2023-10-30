using UnityEngine;

public class AnimatorVAT
{
    public bool inTransition { private set; get; } = false;
    public AnimationVAT CurrentVAT { private set; get; }
    public VATState currentState { private set; get; }
    public TransitionVAT currentTransition { private set; get; } = null;
    public float animationTime { private set; get; } = 0;
    public int eventIndex { private set; get; } = 0;

    AnimatorControllerVAT animatorController;
    MaterialPropertyBlock materialBlock;

    public MeshRenderer renderer;
    public float SpeedMultiplier = 1;
    public delegate void AnimationVATEvent(string clipName, string eventName);
    public event AnimationVATEvent OnVATEvent;
    
    public AnimatorVAT(MaterialPropertyBlock matBlock, MeshRenderer renderer, AnimatorControllerVAT animatorController)
    {
        materialBlock = matBlock;
        this.renderer = renderer;
        this.animatorController = animatorController;

        SetState(animatorController.States[0]);
        renderer.SetPropertyBlock(materialBlock);
    }
    VATState GetState(string name)
    {
        for (int i = 0; i < this.animatorController.States.Length; i++)
        {
            if (this.animatorController.States[i].StateName == name)
            {
                return this.animatorController.States[i];
            }
        }
        Debug.LogError("State not found =" + name);
        return null;
    }
    void SetState(VATState state)
    {
        currentState = state;
        SetVAT(currentState.VAT);
    }
    void SetVAT(AnimationVAT VAT)
    {

        animationTime = 0;
        eventIndex = 0;
        materialBlock.SetTexture("_VATAnimationTexture", VAT.VATTexture);
        materialBlock.SetTexture("_VATNormalTexture", VAT.VATNormal);
        materialBlock.SetTexture("_VATTangentTexture", VAT.VATTangent);
        CurrentVAT = VAT;
        renderer.SetPropertyBlock(materialBlock);
    }
    public void Update(float deltaTime)
    {
        UpdateTime(deltaTime);
        CheckAnimationEvents();
    }
    public void Play(string name)
    {
        // get state
        VATState nextState = GetState(name);
        if (nextState == null) return;

        TransitionVAT transition = GetTransition(nextState);

        if (transition != null && !inTransition)
        {
            currentTransition = transition;
            return;
        }

        //set new state w/o transition
        if (nextState.StateName != currentState.StateName)
        {
            SetState(nextState);
        }
    }
    TransitionVAT GetTransition(VATState toState)
    {
        TransitionVAT transition = null;
        for (int i = 0; i < currentState.Transitions.Length; i++)
        {
            if (currentState.Transitions[i].To.StateName == toState.StateName)
            {
                return currentState.Transitions[i];
            }
        }
        return transition;
    }
    void UpdateTime(float deltaTime)
    {
        float animationSpeed = currentState.VAT.AnimationSpeed;
        animationTime += deltaTime * SpeedMultiplier * animationSpeed;

        if (currentTransition != null)
        {
            if (inTransition)
            {
                float transitionTime = animationTime;
                if (transitionTime >= currentTransition.Length)
                {

                    Play(currentTransition.To.StateName);
                    animationTime = currentTransition.ToTransitionTime;
                    currentTransition = null;
                    inTransition = false;
                }
            }
            else
            {
                float transitionTime = animationTime - currentTransition.FromTransitionStart;
                if (Mathf.Abs(transitionTime)<0.01f)
                {
                    inTransition = true;
                    SetVAT(currentTransition.Transition);
                }
            }
        }
        UpdateCurrentState(deltaTime);
    }
    void UpdateCurrentState(float deltaTime)
    {
        if (!inTransition && currentState.VAT.IsLooped && animationTime >= CurrentVAT.Duration)
        {
            animationTime = 0;
            eventIndex = 0;
        }
        materialBlock.SetFloat("_VATAnimationTime", animationTime / CurrentVAT.Duration);
        renderer.SetPropertyBlock(materialBlock);
    }
    void CheckAnimationEvents()
    {
        if (currentState.VAT.Events.Length <= 0)
        {
            return;
        }
        if (eventIndex >= currentState.VAT.Events.Length)
        {
            return;
        }
        if (currentState.VAT.Events[eventIndex].Time < animationTime * currentState.VAT.Duration)
        {
            OnVATEvent?.Invoke(currentState.StateName, currentState.VAT.Events[eventIndex].Name);
            eventIndex++;
        }

    }
}
[System.Serializable]
public class VATState
{
    public string StateName;
    public AnimationVAT VAT;
    public TransitionVAT[] Transitions;
}