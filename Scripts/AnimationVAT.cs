using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationVAT : ScriptableObject
{
    public float AnimDelta = 0.025f;
    public float TextureStartTime;
    public VATEvent[] Events;
    public bool IsLooped = false;
    public float AnimationSpeed =1;
    public int Frames;
    public float TextureEndTime;
    public float Duration { get { return Frames * AnimDelta; } }
    public float TextureLength { get { return TextureEndTime - TextureStartTime; } }
    [System.Serializable]
    public struct VATEvent {
        public float Time;
        public string Name;
    }
}
