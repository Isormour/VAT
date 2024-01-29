using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransitionVAT : ScriptableObject
{
    public VATState From;
    public VATState To;
    public AnimationVAT Transition;
    public float FrameStart;
    public float TransitionDuration;
    public float TransitionOffset;
    public float ExitTime;
    public float ToTransitionTime {get { return TransitionOffset+TransitionDuration; }}
}
