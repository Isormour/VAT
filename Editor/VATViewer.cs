using UnityEditor;
using UnityEngine;

public class VATViewer : EditorWindow
{
    AnimatorVAT Animator;
    GUIStyle stateStyle;
    GUIStyle stateStyleHighLight;

    public void InitWindow()
    {
        InitStyles();
    }
    void InitStyles()
    {
        stateStyle = new GUIStyle(GUI.skin.box);
        stateStyle.normal.background = CreateStyleTexture(400, 40, new Color(0.7f, 0.7f, 0.7f));

        stateStyleHighLight = new GUIStyle(GUI.skin.box);
        stateStyleHighLight.normal.background = CreateStyleTexture(400, 40, Color.white);
    }
    private Texture2D CreateStyleTexture(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; ++i)
        {
            pix[i] = col;
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
    private void Update()
    {
        Repaint();
    }
    [MenuItem("DB/VAT/VATViewer")]
    public static void CreateWindow()
    {
        GetWindow<VATViewer>("VATViewer").InitWindow();
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

        DrawMainInfo(animVat);

        GUILayout.BeginHorizontal();
        GUILayout.BeginVertical(); DrawLeftSection(animVat); GUILayout.EndVertical();
        GUILayout.BeginVertical(); DrawRightSection(animVat); GUILayout.EndVertical();
        GUILayout.EndHorizontal();
    }
    void DrawMainInfo(AnimatorVAT animVat)
    {

        GUILayout.Label("Currnet state " + animVat.currentState.StateName);
        GUILayout.Label("Currnet time " + animVat.textureTime);
        GUILayout.Label("Is InTransition " + animVat.inTransition);
        GUILayout.Label("Currnet transition " + animVat.currentTransition == null ? animVat.currentTransition.name : "null");

    }
    void DrawLeftSection(AnimatorVAT animVat)
    {
        if (animVat.currentTransition == null) return;

        GUILayout.Label("Transition in start = " + animVat.currentTransition.ExitTime);
        GUILayout.Label("Transition length = " + animVat.currentTransition.TransitionDuration);
        GUILayout.Label("Transition out start = " + animVat.currentTransition.ToTransitionTime);

        float transitionEnterTextureTime = animVat.currentTransition.Transition.TextureLength * animVat.currentTransition.ExitTime;
        float trasitionWindow = animVat.currentState.VAT.TextureLength - transitionEnterTextureTime - animVat.textureTime;


        GUILayout.Label("Time to transition = " + Mathf.Abs((trasitionWindow)));
    }
    void DrawRightSection(AnimatorVAT animVat)
    {
        VATState[] states = animVat.animatorController.States;
        float height = 50;
        Vector2 startPosition = new Vector2(400, 40);

        for (int i = 0; i < states.Length; i++)
        {
            bool isCurrentState = animVat.currentState == states[i];
            Rect stateRect = new Rect(startPosition + new Vector2(0, height * i), new Vector2(100, height - 10));
            Color lastcolor = GUI.color;
            if (!isCurrentState)
            {
                GUI.color = new Color(0.0f, 0.0f, 0.0f);
                GUI.Box(stateRect, "");
            }
            else
            {
                GUI.color = new Color(2.0f, 1.7f, 1.7f);
                float textureDuration = animVat.CurrentVAT.TextureLength;
                float widthMult = +animVat.textureTime / textureDuration;
                if (animVat.inTransition)
                {
                    GUI.color = new Color(0.5f, 0.5f, 1.0f);
                    float transitionDuration = animVat.currentTransition.Transition.TextureLength;
                    widthMult = animVat.textureTime / transitionDuration;
                }
                GUI.Box(stateRect, "");
                GUI.color = new Color(10.0f, 10.0f, 10.0f);

                Rect LineRect = new Rect(new Vector2(stateRect.x, stateRect.y + 34), new Vector2(stateRect.width * widthMult, 2));
                GUI.Box(LineRect, "");
            }

            GUI.color = lastcolor;
            GUI.Label(stateRect, states[i].StateName);


        }
    }
}
