using UnityEditor;
using UnityEngine;

public class VATViewer : EditorWindow
{

    AnimatorVAT Animator;
    [MenuItem("DB/VATViewer")]
    public static void CreateWindow()
    {
        GetWindow<VATViewer>("VATViewer").InitWindow();
    }
    public void InitWindow()
    {
       

    }
    private void Update()
    {
        Repaint();
    }
    public void OnGUI()
    {
        if (Selection.activeGameObject == null)
        {
            GUI.contentColor = Color.red;
            GUILayout.Label("Select GameObject");
            GUI.contentColor = Color.black;
            return;
        }
        GameObject obj = Selection.activeGameObject;
        IAnimatorVat animatorVatTest = obj.GetComponent<IAnimatorVat>();
        if (animatorVatTest == null)
        {
            GUI.contentColor = Color.red;
            GUILayout.Label("Select GameObject with VATTester");
            GUI.contentColor = Color.black;
            return;
        }
        if (!EditorApplication.isPlaying)
        {
            GUI.contentColor = Color.red;
            GUILayout.Label("Editor not playing");
            GUI.contentColor = Color.black;
            return;
        }
        AnimatorVAT animVat = animatorVatTest.GetAnimatorVat();
        GUILayout.Label("Currnet state " + animVat.currentState.StateName);
        GUILayout.Label("Currnet time " + animVat.animationTime);
        GUILayout.Label("Is InTransition " + animVat.inTransition);
        GUILayout.Label("Currnet transition " + animVat.currentTransition == null? animVat.currentTransition.name : "null");
        if(animVat.currentTransition !=null)
        {
            GUILayout.Label("Transition in start = " + animVat.currentTransition.FromTransitionStart);
            GUILayout.Label("Transition length = " + animVat.currentTransition.Length);
            GUILayout.Label("Transition out start = " + animVat.currentTransition.ToTransitionTime);
        }
        
    }
}
