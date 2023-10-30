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
            if (GUILayout.Button("Toggle Anim"))
            {
                ToggleAnimation();
            }
        }
    }
    private void Update()
    {
        if (ModelRenderer != null)
        {
            UpdateTime();
            UpdateRenderer();
            if (AnimationMode.InAnimationMode())
                SceneView.RepaintAll();
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
    void ToggleAnimation()
    {
        if (AnimationMode.InAnimationMode())
            AnimationMode.StopAnimationMode();
        else
            AnimationMode.StartAnimationMode();
    }
}
