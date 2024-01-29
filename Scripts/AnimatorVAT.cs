using UnityEngine;

public class AnimatorVAT
{
    public delegate void StateChange(VATState newState);
    public event StateChange OnStateChange;
    public bool inTransition { private set; get; } = false;
    public AnimationVAT CurrentVAT { private set; get; }
    public VATState currentState { private set; get; }
    public TransitionVAT currentTransition { private set; get; } = null;
    public float textureTime { private set; get; } = 0;
    public int eventIndex { private set; get; } = 0;

    public AnimatorControllerVAT animatorController { private set; get; }

    MaterialPropertyBlock materialBlock;

    public MeshRenderer renderer;
    public float SpeedMultiplier = 1;
    public delegate void AnimationVATEvent(string clipName, string eventName);
    public event AnimationVATEvent OnVATEvent;

    public AnimatorVAT(MaterialPropertyBlock matBlock, MeshRenderer renderer, AnimatorControllerVAT animatorController)
    {
        materialBlock = matBlock;
        this.renderer = renderer;
        Bounds temp = this.renderer.localBounds;
        temp.extents  = animatorController.BoundsScale * this.renderer.localBounds.extents;
        this.renderer.localBounds = temp;
        this.animatorController = animatorController;

        materialBlock.SetTexture("_VATAnimationTexture", animatorController.VATPosition);
        materialBlock.SetTexture("_VATNormalTexture", animatorController.VATNormal);
        materialBlock.SetTexture("_VATTangentTexture", animatorController.VATTangent);

        SetState(animatorController.States[0]);
        renderer.SetPropertyBlock(materialBlock);
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
        textureTime = 0;
        eventIndex = 0;
        CurrentVAT = VAT;
    }
    public void Update(float deltaTime)
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
    void UpdateTime(float deltaTime)
    {
        float animationSpeed = currentState.VAT.AnimationSpeed;
        textureTime += deltaTime * SpeedMultiplier * animationSpeed;

        if (currentTransition != null)
        {
            if (inTransition)
            {
                float TransitionEndTime = currentTransition.Transition.TextureLength;
                //exit transition
                if (textureTime >= TransitionEndTime)
                {
                    string nextState = currentTransition.To.StateName;
                    float nextAnimTime = currentTransition.Transition.TextureLength;
                    currentTransition = null;
                    Play(nextState);
                    textureTime = nextAnimTime;
                    inTransition = false;
                }
            }
            else
            {
              
                float transitionEnterTextureTime = currentTransition.Transition.TextureLength*currentTransition.ExitTime;
                float trasitionWindow = currentState.VAT.TextureLength - transitionEnterTextureTime - textureTime;
                //enter transition
                
                if (Mathf.Abs(trasitionWindow) < currentTransition.Transition.AnimDelta/2)
                {
                    inTransition = true;
                    SetVAT(currentTransition.Transition);
                    // dirty haxor, in some cases after setting new transition animation system uses last frame from previous anim on texture
                    textureTime += 0.001f;
                }
            }
        }
        UpdateCurrentState();
    }
    void UpdateCurrentState()
    {
        //TODO move 0.025f TimeDelta 
        float animEnd = currentState.VAT.TextureEndTime;
        float currentTime = currentState.VAT.TextureStartTime + textureTime;
        if (!inTransition && currentState.VAT.IsLooped && currentTime >= animEnd)
        {
            textureTime = 0;
            eventIndex = 0;
        }
        if (!inTransition)
        {
            materialBlock.SetFloat("_VATAnimationTime", currentState.VAT.TextureStartTime + textureTime);
        }
        else
        {
            materialBlock.SetFloat("_VATAnimationTime", currentTransition.Transition.TextureStartTime + textureTime);
        }
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
        if (currentState.VAT.Events[eventIndex].Time < currentState.VAT.TextureStartTime + textureTime)
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
    public VATState(string stateName, AnimationVAT vAT, int transitionNum)
    {
        StateName = stateName;
        VAT = vAT;
        Transitions = new TransitionVAT[transitionNum];
    }
}
