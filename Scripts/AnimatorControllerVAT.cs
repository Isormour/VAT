using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorControllerVAT : ScriptableObject
{
    public VATState[] States;
    public float BoundsScale =1;
    public Material Mat;
    public VATState GetState(string stateName)
    {
        VATState state = null;
        for (int i = 0; i < States.Length; i++)
        {
            if (States[i].StateName == stateName)
            {
                return States[i];
            }
        }
        if (state == null) { Debug.LogError("state not found = " + stateName); }
        return state;
    }
}
