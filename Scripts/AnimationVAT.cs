using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationVAT : ScriptableObject
{
    public Texture2D VATTexture;
    public float Duration;
    public VATEvent[] Events;
    public bool IsLooped = false;
    public float AnimationSpeed =1;

    [System.Serializable]
    public struct VATEvent {
        public float Time;
        public string Name;
    }
}
