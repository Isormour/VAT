using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AnimatorVAT))]
public class AnimatorVATEditor : Editor
{
    AnimatorVAT anim;
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    }
}
