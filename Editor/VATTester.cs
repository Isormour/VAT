using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class VATTester : EditorWindow
{
   
    public MeshRenderer ModelRenderer;
    MaterialPropertyBlock RendererProperties;
    float animTime = 0;
    double LastTime = 0;

    [MenuItem("DB/VATTester")]
    public static void CreateWindow()
    {
        GetWindow<VATTester>("VATTester").InitWindow();
    }
    void InitWindow()
    {
        RendererProperties = new MaterialPropertyBlock();
        LastTime = EditorApplication.timeSinceStartup;
    }
    private void OnGUI()
    {
        ModelRenderer = (MeshRenderer)EditorGUILayout.ObjectField("ModelRenderer", ModelRenderer, typeof(MeshRenderer),true);

        if (ModelRenderer != null)
        {
            UpdateTime();
            UpdateRenderer();
        }
    }
    void UpdateTime()
    {
        double timeDelta = EditorApplication.timeSinceStartup - LastTime;
        LastTime = EditorApplication.timeSinceStartup;
        animTime += (float)timeDelta;

        if (animTime > 1)
        {
            animTime = 0;
        }
    }
    void UpdateRenderer()
    {
        RendererProperties.SetFloat("_VATAnimationTime", animTime);
        ModelRenderer.SetPropertyBlock(RendererProperties);
    }
}
