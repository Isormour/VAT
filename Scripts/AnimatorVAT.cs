using UnityEngine;

public class AnimatorVAT
{
    public delegate void StateChange(VATState newState);
    public event StateChange OnStateChange;
    public bool inTransition { private set; get; } = false;
    public AnimationVAT CurrentVAT { private set; get; }
    public VATState currentState { private set; get; }
    public TransitionVAT currentTransition { private set; get; } = null;
    public float animationTime { private set; get; } = 0;
    public int eventIndex { private set; get; } = 0;
    public AnimatorControllerVAT animatorController { private set; get; }
    public MeshRenderer renderer { private set; get; }
    public float SpeedMultiplier { private set; get; }  = 1;
    public delegate void AnimationVATEvent(string clipName, string eventName);
    public event AnimationVATEvent OnVATEvent;
    public MaterialPropertyBlock materialBlock { private set; get; }

    public AnimatorVAT(MaterialPropertyBlock matBlock, MeshRenderer renderer, AnimatorControllerVAT animatorController)
    {
        materialBlock = matBlock;
        SetRenderer(renderer);
        this.animatorController = animatorController;
        SetState(animatorController.States[0]);
        ApplyPropertyBlock(materialBlock);
    }
    protected virtual void SetRenderer(MeshRenderer rend)
    {
        Bounds temp = this.renderer.localBounds;
        temp.extents = animatorController.BoundsScale * this.renderer.localBounds.extents;
        this.renderer = rend;
        this.renderer.localBounds = temp;
    }

    public VATState GetState(string name)
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
        OnStateChange?.Invoke(state);
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
        ApplyPropertyBlock(materialBlock);
    }
    public virtual void Update(float deltaTime)
    {
        UpdateTime(deltaTime);
        CheckAnimationEvents();
    }
    public void Play(string name)
    {

        if (currentTransition != null) return;
        // get state
        VATState nextState = GetState(name);
        if (nextState == null || currentState == nextState) return;

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
    protected void UpdateTime(float deltaTime)
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
                    string nextState = currentTransition.To.StateName;
                    float nextAnimTime = currentTransition.ToTransitionTime;
                    currentTransition = null;
                    Play(nextState);
                    animationTime = nextAnimTime;
                    inTransition = false;
                }
            }
            else
            {
                float transitionTime = animationTime - currentTransition.FromTransitionStart;
                if (Mathf.Abs(transitionTime) < 0.01f)
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
        ApplyPropertyBlock(materialBlock);
    }
    protected virtual void ApplyPropertyBlock(MaterialPropertyBlock block)
    {
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