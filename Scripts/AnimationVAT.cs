using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationVAT : ScriptableObject
{
    public Texture2D VATTexture;
    public Material Mat;
    public float Duration;
    public VATEvent[] Events;
    public bool IsLooped = false;

    [System.Serializable]
    public struct VATEvent {
        public float Time;
        public string Name;
    }
}
