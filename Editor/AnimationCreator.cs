using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using AnimatorController = UnityEditor.Animations.AnimatorController;

public class AnimationCreator : EditorWindow
{
    public ComputeShader InfoTexGen;
    public AnimationClip[] Clips;
    public GameObject Model;
    public Material BaseMaterial;
    public static float AnimDelta = 0.025f;
    public struct VertInfo
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector3 Tangent;
    }
    struct VATBakeData
    {
        public List<VertInfo> TotalVertexData;
        public float AnimationStart;
        public int TotalFrames;
    }

    [MenuItem("DB/VAT/VATBaker")]

    public static void CreateWindow()
    {
        GetWindow<AnimationCreator>("VATBaker");
    }
    private void OnGUI()
    {
        DrawProperties();
        SetupCreator();
        DrawButton();
    }
    void DrawProperties()
    {
        InfoTexGen = (ComputeShader)EditorGUILayout.ObjectField("infoTexGen", InfoTexGen, typeof(ComputeShader), false);
        Model = (GameObject)EditorGUILayout.ObjectField("model", Model, typeof(GameObject), true);
        BaseMaterial = (Material)EditorGUILayout.ObjectField("material", BaseMaterial, typeof(Material), true);
    }
    void SetupCreator()
    {
        if (InfoTexGen == null)
        {
            InfoTexGen = AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.db.vat_animator/Graphics/MeshInfoTextureGen.compute");
        }
    }
    void DrawButton()
    {
        if (InfoTexGen == null) return;
        if (Model == null) return;
        if (BaseMaterial == null) return;

        if (GUILayout.Button("Bake Animator"))
        {
            BakeAnimator();
        }
        if (GUILayout.Button("Bake for Indirect rendering"))
        {
            BakeAnimator(true);
        }
    }

    void BakeAnimator(bool forIndirectRendering = false)
    {
        if (!Model.activeInHierarchy) Model.SetActive(true);
        var animator = Model.GetComponent<Animator>();
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        Clips = animator.runtimeAnimatorController.animationClips;
        var skin = Model.GetComponentInChildren<SkinnedMeshRenderer>();
        var vCount = skin.sharedMesh.vertexCount;
        var texWidth = Mathf.NextPowerOfTwo(vCount);
        var mesh = new Mesh();
        string name = Model.name;

        var folderName = "BakedAnimationTex";
        var folderPath = Path.Combine("Assets/VAT", folderName);

        if (!AssetDatabase.IsValidFolder("Assets/VAT"))
            AssetDatabase.CreateFolder("Assets", "VAT");

        if (!AssetDatabase.IsValidFolder(folderPath))
            AssetDatabase.CreateFolder("Assets/VAT", folderName);

        var subFolder = name;
        var subFolderPath = Path.Combine(folderPath, subFolder);
        if (!AssetDatabase.IsValidFolder(subFolderPath))
            AssetDatabase.CreateFolder(folderPath, subFolder);

        RuntimeAnimatorController controller = animator.runtimeAnimatorController;
        EditorUtility.SetDirty(controller);
        AnimatorStateMachine stateMachine = ((AnimatorController)controller).layers[0].stateMachine;
        EditorUtility.SetDirty(stateMachine);
        ChildAnimatorState[] states = stateMachine.states;

        Dictionary<AnimatorState, AnimatorStateTransition[]> StateToTransition = new Dictionary<AnimatorState, AnimatorStateTransition[]>();
        Dictionary<AnimatorState, AnimationVAT> StateToVat = new Dictionary<AnimatorState, AnimationVAT>();
        Dictionary<AnimatorState, TransitionVAT[]> StateToVATTransition = new Dictionary<AnimatorState, TransitionVAT[]>();

        StoreTransitions(states, StateToTransition, StateToVATTransition);
        CreateVATAssets(subFolderPath, stateMachine, StateToVat);

        AnimatorControllerVAT animatorController;
        if (forIndirectRendering)
        {
            animatorController = CreateInstance<AnimatorControllerVATIndirect>();
            AnimatorControllerVATIndirect temp = (AnimatorControllerVATIndirect)animatorController;
            temp.Mesh = skin.sharedMesh;
        }
        else
        {
            animatorController = CreateInstance<AnimatorControllerVAT>();
        }

        EditorUtility.SetDirty(animatorController);
        animatorController.States = new VATState[states.Length];

        CreateVATStates(states, StateToVat, StateToVATTransition, animatorController);
        AnimatorState defaultState = stateMachine.defaultState;

        CreateVATTransitions(subFolderPath, stateMachine, states, StateToTransition, animatorController);
        stateMachine.defaultState = defaultState;
        animatorController.BoundsScale = skin.transform.localScale.x;

        VATBakeData bakeData = BakeVATs(animator,
            skin,
            vCount,
            mesh,
            stateMachine,
            states,
            StateToTransition,
            StateToVat,
            animatorController);

        // set time stamps for texture
        SetTimestampsForVATs(states, StateToTransition, StateToVat, animatorController, bakeData);
        ApplyOriginalTransitions(stateMachine, states, StateToTransition);
        stateMachine.defaultState = defaultState;
        EditorUtility.SetDirty(controller);

        AssetDatabase.SaveAssets();

        Texture2D[] VATTextures = CreateVatTextures(texWidth, animatorController, vCount, subFolderPath, bakeData.TotalVertexData);
        animatorController.Mat = new Material(BaseMaterial);
        animatorController.Mat.SetTexture("_VATAnimationTexture", VATTextures[0]);
        animatorController.Mat.SetTexture("_VATNormalTexture", VATTextures[1]);
        animatorController.Mat.SetTexture("_VATTangentTexture", VATTextures[2]);

        AssetDatabase.CreateAsset(animatorController.Mat, Path.Combine(subFolderPath, "VAT_MATERIAL_" + name + ".mat"));
        AssetDatabase.CreateAsset(animatorController, Path.Combine(subFolderPath, "VAT_CONTROLLER_" + name + ".asset"));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void ApplyOriginalTransitions(AnimatorStateMachine stateMachine, ChildAnimatorState[] states, Dictionary<AnimatorState, AnimatorStateTransition[]> StateToTransition)
    {
        for (int i = 0; i < states.Length; i++)
        {
            for (int j = 0; j < StateToTransition[states[i].state].Length; j++)
            {
                stateMachine.states[i].state.AddTransition(StateToTransition[states[i].state][j]);
                AssetDatabase.AddObjectToAsset(StateToTransition[states[i].state][j], stateMachine.states[i].state);
            }
        }
    }

    private static void SetTimestampsForVATs(ChildAnimatorState[] states, Dictionary<AnimatorState, AnimatorStateTransition[]> StateToTransition, Dictionary<AnimatorState, AnimationVAT> StateToVat, AnimatorControllerVAT animatorController, VATBakeData bakeData)
    {
        float totalTime = bakeData.AnimationStart;
        for (int i = 0; i < states.Length; i++)
        {
            AnimatorState state = states[i].state;
            EditorUtility.SetDirty(StateToVat[state]);
            AnimationVAT animationVAT = StateToVat[state];
            animationVAT.TextureStartTime = animationVAT.TextureStartTime / totalTime;
            animationVAT.TextureEndTime = animationVAT.TextureStartTime + (animationVAT.Frames / (float)bakeData.TotalFrames);
            for (int j = 0; j < StateToTransition[state].Length; j++)
            {
                float tempStartTime = animatorController.States[i].Transitions[j].Transition.TextureStartTime;
                tempStartTime = tempStartTime / totalTime;
                AnimationVAT transitionObject = animatorController.States[i].Transitions[j].Transition;
                EditorUtility.SetDirty(transitionObject);
                transitionObject.TextureStartTime = tempStartTime;
                int frames = transitionObject.Frames;
                transitionObject.TextureEndTime = tempStartTime + (frames / (float)bakeData.TotalFrames);
                SerializedObject temp = new SerializedObject(transitionObject);
                temp.ApplyModifiedProperties();
            }
        }
    }
    private VATBakeData BakeVATs(Animator animator, SkinnedMeshRenderer skin, int vCount, Mesh mesh, AnimatorStateMachine stateMachine, ChildAnimatorState[] states, Dictionary<AnimatorState, AnimatorStateTransition[]> StateToTransition, Dictionary<AnimatorState, AnimationVAT> StateToVat, AnimatorControllerVAT animatorController)
    {
        VATBakeData data = new VATBakeData();
        // gather vertex positions
        data.TotalVertexData = new List<VertInfo>();
        data.AnimationStart = 0;
        data.TotalFrames = 0;
        for (int i = 0; i < states.Length; i++)
        {
            AnimatorState state = states[i].state;
            EditorUtility.SetDirty(state);
            stateMachine.defaultState = state;
            data.TotalVertexData.AddRange(GetClipData(animator, state, skin, mesh, vCount));
            StateToVat[state].TextureStartTime = data.AnimationStart;
            data.AnimationStart += StateToVat[state].Duration;
            data.TotalFrames += StateToVat[state].Frames;
            for (int j = 0; j < StateToTransition[state].Length; j++)
            {
                state.AddTransition(StateToTransition[state][j]);
                data.TotalVertexData.AddRange(GetClipData(animator, state, StateToTransition[state][j], skin, mesh, vCount));
                state.RemoveTransition(StateToTransition[state][j]);
                AnimationVAT transitionObject = animatorController.States[i].Transitions[j].Transition;
                transitionObject.TextureStartTime = data.AnimationStart;
                data.AnimationStart += transitionObject.Duration;
                data.TotalFrames += transitionObject.Frames;
            }
        }
        return data;
    }
    private void CreateVATTransitions(string subFolderPath, AnimatorStateMachine stateMachine, ChildAnimatorState[] states, Dictionary<AnimatorState, AnimatorStateTransition[]> StateToTransition, AnimatorControllerVAT animatorController)
    {
        //creatre transitions
        for (int i = 0; i < states.Length; i++)
        {
            for (int j = 0; j < StateToTransition[states[i].state].Length; j++)
            {
                stateMachine.defaultState = states[i].state;

                AnimatorStateTransition animTransition = StateToTransition[states[i].state][j];

                TransitionVAT transitionVat = CreateInstance<TransitionVAT>();
                transitionVat.From = animatorController.States[i];
                transitionVat.To = animatorController.GetState(animTransition.destinationState.name);
                transitionVat.ExitTime = animTransition.exitTime;
                transitionVat.TransitionDuration = animTransition.duration;
                transitionVat.TransitionOffset = animTransition.offset;
                string transitionName = states[i].state.name + "-" + animTransition.destinationState.name;

                string transitionAssetName = "VAT_Transition_" + transitionName;
                string transitionAnimationName = transitionVat.From.StateName + "_TO_" + transitionVat.To.StateName;
                animatorController.States[i].Transitions[j] = transitionVat;
                transitionVat.Transition = CreateVATObject(subFolderPath, transitionAnimationName, transitionVat.TransitionDuration, new AnimationEvent[0], false);
                AssetDatabase.CreateAsset(transitionVat, Path.Combine(subFolderPath, transitionAssetName + ".asset"));
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
    }

    private static void CreateVATStates(ChildAnimatorState[] states, Dictionary<AnimatorState, AnimationVAT> StateToVat, Dictionary<AnimatorState, TransitionVAT[]> StateToVATTransition, AnimatorControllerVAT animatorController)
    {
        for (int i = 0; i < animatorController.States.Length; i++)
        {
            int transitionCount = StateToVATTransition[states[i].state].Length;
            animatorController.States[i] = new VATState(states[i].state.name, StateToVat[states[i].state], transitionCount);
        }
    }

    private void CreateVATAssets(string subFolderPath, AnimatorStateMachine stateMachine, Dictionary<AnimatorState, AnimationVAT> StateToVat)
    {
        for (int i = 0; i < stateMachine.states.Length; i++)
        {
            AnimatorState state = stateMachine.states[i].state;
            AnimationClip clip = (AnimationClip)state.motion;
            StateToVat.Add(state, CreateVATObject(subFolderPath, clip.name, clip.length, clip.events, clip.isLooping));
        }
    }

    private static void StoreTransitions(ChildAnimatorState[] states, Dictionary<AnimatorState, AnimatorStateTransition[]> StateToTransition, Dictionary<AnimatorState, TransitionVAT[]> StateToVATTransition)
    {
        //store and remove all transition so animation will be baked without them
        for (int sIndex = 0; sIndex < states.Length; sIndex++)
        {
            AnimatorState animatorState = states[sIndex].state;
            int transitionLength = animatorState.transitions.Length;

            StateToTransition.Add(animatorState, new AnimatorStateTransition[transitionLength]);
            StateToVATTransition.Add(animatorState, new TransitionVAT[transitionLength]);

            for (int tIndex = 0; tIndex < StateToTransition[animatorState].Length; tIndex++)
            {
                StateToTransition[animatorState][tIndex] = Instantiate(animatorState.transitions[tIndex]);
                StateToTransition[animatorState][tIndex].name = animatorState.transitions[tIndex].name;
            }
            for (int j = 0; j < transitionLength; j++)
            {
                animatorState.RemoveTransition(animatorState.transitions[j]);
            }
        }
    }

    private List<VertInfo> GetClipData(Animator animator, AnimatorState state, AnimatorStateTransition transition, SkinnedMeshRenderer skin, Mesh mesh, int vCount)
    {
        float AnimTime = ((AnimationClip)state.motion).length;
        return GetClipData(animator, state, skin, mesh, vCount, transition.duration, transition.exitTime * AnimTime);
    }
    public List<VertInfo> GetClipData(Animator anim, AnimatorState state, SkinnedMeshRenderer skin, Mesh mesh, int vCount, float length = -1f, float startTime = -1)
    {
        List<VertInfo> infoList = SampleAnimator(anim, state, skin, mesh, vCount, length, startTime);
        return infoList;
    }
    public List<VertInfo> SampleAnimator(Animator anim, AnimatorState state, SkinnedMeshRenderer skin, Mesh mesh, int vCount, float length = -1, float startTime = -1)
    {
        AnimationClip clip = (AnimationClip)state.motion;
        if (length < 0)
        {
            length = clip.length;
        }

        var frames = Mathf.NextPowerOfTwo((int)(length / AnimDelta));
        var dt = length / frames;
        var infoList = new List<VertInfo>();
        anim.Play(state.name);
        if (startTime > 0)
        {
            anim.Update(startTime);
        }
        for (var i = 0; i < frames; i++)
        {
            anim.Update(dt);
            skin.BakeMesh(mesh);
            infoList.AddRange(Enumerable.Range(0, vCount)
                .Select(idx => new VertInfo()
                {
                    Position = mesh.vertices[idx],
                    Normal = mesh.normals[idx],
                    Tangent = mesh.tangents[idx]
                })
            );
        }
        return infoList;
    }
    Texture2D[] CreateVatTextures(int texWidth, AnimatorControllerVAT controller, int vCount, string subfolder, List<VertInfo> totalVerts)
    {
        int AllAnimations = controller.States.Length;
        for (int i = 0; i < controller.States.Length; i++)
        {
            AllAnimations += controller.States[i].Transitions.Length;
        }
        AnimationVAT[] animations = new AnimationVAT[AllAnimations];
        int AnimationIndex = 0;

        //set animation array
        for (int i = 0; i < controller.States.Length; i++)
        {
            animations[AnimationIndex] = controller.States[i].VAT;
            AnimationIndex++;
            for (int j = 0; j < controller.States[i].Transitions.Length; j++)
            {
                animations[AnimationIndex] = controller.States[i].Transitions[j].Transition;
                AnimationIndex++;
            }
        }

        int frames = totalVerts.Count / vCount;
        RenderTexture[] textures = CreateTextures(texWidth, frames, Model.name);
        ApplyDataToTextures(textures, totalVerts, this.InfoTexGen, vCount, frames);
        Texture2D[] VATTextures = CreateTextureAssets(textures, subfolder);
        return VATTextures;
    }
    RenderTexture[] CreateTextures(int texWidth, int frames, string modelName)
    {
        var pRt = new RenderTexture(texWidth, frames, 0, RenderTextureFormat.ARGBHalf);
        pRt.name = string.Format("{0}_posTex", modelName);
        var nRt = new RenderTexture(texWidth, frames, 0, RenderTextureFormat.ARGBHalf);
        nRt.name = string.Format("{0}_normTex", modelName);
        var tRt = new RenderTexture(texWidth, frames, 0, RenderTextureFormat.ARGBHalf);
        tRt.name = string.Format("{0}_tangentTex", modelName);

        foreach (var rt in new[] { pRt, nRt, tRt })
        {
            rt.enableRandomWrite = true;
            rt.Create();
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.clear);
        }
        return new RenderTexture[] { pRt, nRt, tRt };
    }
    void ApplyDataToTextures(RenderTexture[] textures, List<VertInfo> infoList, ComputeShader shader, int vCount, int frames)
    {
        var buffer = new ComputeBuffer(infoList.Count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(VertInfo)));
        buffer.SetData(infoList.ToArray());

        var kernel = shader.FindKernel("CSMain");
        uint x, y, z;
        shader.GetKernelThreadGroupSizes(kernel, out x, out y, out z);

        shader.SetInt("VertCount", vCount);
        shader.SetBuffer(kernel, "Info", buffer);
        shader.SetTexture(kernel, "OutPosition", textures[0]);
        shader.SetTexture(kernel, "OutNormal", textures[1]);
        shader.SetTexture(kernel, "OutTangent", textures[2]);
        shader.Dispatch(kernel, vCount / (int)x + 1, frames / (int)y + 1, 1);

        buffer.Release();
    }
    Texture2D[] CreateTextureAssets(RenderTexture[] textures, string subFolderPath)
    {
        var posTex = RenderTextureToTexture2D.Convert(textures[0]);
        var normTex = RenderTextureToTexture2D.Convert(textures[1]);
        var tanTex = RenderTextureToTexture2D.Convert(textures[2]);

        Graphics.CopyTexture(textures[0], posTex);
        Graphics.CopyTexture(textures[1], normTex);
        Graphics.CopyTexture(textures[2], tanTex);
        posTex.wrapMode = TextureWrapMode.Clamp;
        normTex.wrapMode = TextureWrapMode.Clamp;
        tanTex.wrapMode = TextureWrapMode.Clamp;

        AssetDatabase.CreateAsset(posTex, Path.Combine(subFolderPath, textures[0].name + ".asset"));
        AssetDatabase.CreateAsset(normTex, Path.Combine(subFolderPath, textures[1].name + ".asset"));
        AssetDatabase.CreateAsset(tanTex, Path.Combine(subFolderPath, textures[2].name + ".asset"));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return new Texture2D[] { posTex, normTex, tanTex };
    }
    AnimationVAT CreateVATObject(string subFolderPath, string clipName, float length, AnimationEvent[] events, bool isLooping)
    {
        AnimationVAT VATObject = CreateInstance<AnimationVAT>();

        AnimationVAT.VATEvent[] vatEvents = new AnimationVAT.VATEvent[events.Length];
        for (int i = 0; i < events.Length; i++)
        {
            AnimationVAT.VATEvent tempEvent = new AnimationVAT.VATEvent();
            tempEvent.TimeNormalized = events[i].time / length;
            tempEvent.Name = events[i].functionName;
            vatEvents[i] = tempEvent;
        }

        VATObject.Events = vatEvents;
        VATObject.IsLooped = isLooping;
        VATObject.AnimDelta = AnimDelta;
        int frames = Mathf.NextPowerOfTwo((int)(length / AnimDelta));
        VATObject.Frames = frames;

        AssetDatabase.CreateAsset(VATObject, Path.Combine(subFolderPath, "VAT_" + clipName + ".asset"));
        return VATObject;

    }
}
