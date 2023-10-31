using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TransitionVAT : ScriptableObject
{
    public VATState From;
    public VATState To;
    public AnimationVAT Transition;
    public float Length;
    public float ToTransitionStart;
    public float FromTransitionStart;
    public float ToTransitionTime {get { return ToTransitionStart+Length; }}
}
