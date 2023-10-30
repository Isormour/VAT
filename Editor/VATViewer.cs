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
        AnimatorVATTest animatorVatTest = obj.GetComponent<AnimatorVATTest>();
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
        GUILayout.Label("Currnet state " + animatorVatTest.Vat.currentState.StateName);
        GUILayout.Label("Currnet time " + animatorVatTest.Vat.animationTime);
        GUILayout.Label("Is InTransition " + animatorVatTest.Vat.inTransition);
        GUILayout.Label("Currnet transition " + animatorVatTest.Vat.currentTransition == null? animatorVatTest.Vat.currentTransition.name : "null");
        if(animatorVatTest.Vat.currentTransition !=null)
        {
            GUILayout.Label("Transition in start = " + animatorVatTest.Vat.currentTransition.FromTransitionStart);
            GUILayout.Label("Transition length = " + animatorVatTest.Vat.currentTransition.Length);
            GUILayout.Label("Transition out start = " + animatorVatTest.Vat.currentTransition.ToTransitionTime);
        }
        
    }
}
