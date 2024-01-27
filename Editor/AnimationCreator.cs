using Codice.Client.BaseCommands;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditorInternal;
using UnityEngine;
using static UnityEditor.VersionControl.Asset;
using static UnityEngine.Random;
using AnimatorController = UnityEditor.Animations.AnimatorController;

public class AnimationCreator : EditorWindow
{
    public ComputeShader infoTexGen;
    public Material animBaseMaterial;
    public AnimationClip[] clips;
    public GameObject model;
    public static float AnimDelta = 0.025f;
    public struct VertInfo
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector3 tangent;
    }

    [MenuItem("DB/VAT/VATBaker")]

    public static void CreateWindow()
    {
        GetWindow<AnimationCreator>("VATBaker");
    }
    private void OnGUI()
    {
        DrawProperties();
        Setup();
        DrawButton();
    }
    void DrawProperties()
    {
        infoTexGen = (ComputeShader)EditorGUILayout.ObjectField("infoTexGen", infoTexGen, typeof(ComputeShader), false);
        animBaseMaterial = (Material)EditorGUILayout.ObjectField("playShader", animBaseMaterial, typeof(Material), false);
        model = (GameObject)EditorGUILayout.ObjectField("model", model, typeof(GameObject), true);
    }
    void Setup()
    {
        if (infoTexGen == null)
        {
            infoTexGen = AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.db.VAT/Graphics/MeshInfoTextureGen.compute");
        }

        if (animBaseMaterial == null)
        {
            animBaseMaterial = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.db.VAT/Graphics/AnimatedMatBasic.mat");
        }
    }
    void DrawButton()
    {
        if (animBaseMaterial == null) return;
        if (infoTexGen == null) return;
        if (model == null) return;

        if (GUILayout.Button("Bake Clips"))
        {
            BakeForClips();
        }
        if (GUILayout.Button("Bake Animator"))
        {
            BakeForAnimator();
        }
    }

    void BakeForClips()
    {
        if (!model.gameObject.activeInHierarchy) model.gameObject.SetActive(true);
        var animator = model.GetComponent<Animator>();
        clips = animator.runtimeAnimatorController.animationClips;
        var skin = model.GetComponentInChildren<SkinnedMeshRenderer>();
        var vCount = skin.sharedMesh.vertexCount;
        var texWidth = Mathf.NextPowerOfTwo(vCount);
        var mesh = new Mesh();
        string name = model.name;

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

        AnimatorControllerVAT animatorController = CreateInstance<AnimatorControllerVAT>();
        animatorController.States = new VATState[clips.Length];

        List<VertInfo> totalVerts = new List<VertInfo>();
        float startTime = 0;
        for (int i = 0; i < clips.Length; i++)
        {
            int frames = Mathf.NextPowerOfTwo((int)(clips[i].length / AnimDelta));
            int dt = (int)(clips[i].length / frames);
            List<VertInfo> verts = SampleAnimation(clips[i], skin, mesh, vCount, frames, dt, model.gameObject);
            AnimationVAT animationVAT = CreateVATObject(subFolder, clips[i].name, clips[i].length, clips[i].events, clips[i].isLooping);
            animationVAT.StartTime = startTime;
            startTime += animationVAT.Duration;
            animatorController.States[i] = new VATState(clips[i].name, animationVAT, 0);
            totalVerts.AddRange(verts);
        }
        AssetDatabase.CreateAsset(animatorController, Path.Combine(subFolderPath, "VAT_CONTROLLER_" + name + ".asset"));
        Texture2D[] VATTextures = CreateVatTextures(texWidth, animatorController, vCount, subFolderPath, totalVerts);
        animatorController.VATPosition = VATTextures[0];
        animatorController.VATNormal = VATTextures[1];
        animatorController.VATTangent = VATTextures[2];


        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        animBaseMaterial.SetTexture("_VATAnimationTexture", VATTextures[0]);
        animBaseMaterial.SetTexture("_VATNormalTexture", VATTextures[1]);
        animBaseMaterial.SetTexture("_VATTangentTexture", VATTextures[2]);
    }

    void BakeForAnimator()
    {
        if (!model.activeInHierarchy) model.SetActive(true);
        var animator = model.GetComponent<Animator>();
        clips = animator.runtimeAnimatorController.animationClips;
        var skin = model.GetComponentInChildren<SkinnedMeshRenderer>();
        var vCount = skin.sharedMesh.vertexCount;
        var texWidth = Mathf.NextPowerOfTwo(vCount);
        var mesh = new Mesh();
        string name = model.name;

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
        AnimatorStateMachine stateMachine = ((AnimatorController)controller).layers[0].stateMachine;
        ChildAnimatorState[] states = stateMachine.states;
        Dictionary<AnimatorState, AnimatorStateTransition[]> StateToTransition = new Dictionary<AnimatorState, AnimatorStateTransition[]>();
        Dictionary<AnimatorState, AnimationVAT> StateToVat = new Dictionary<AnimatorState, AnimationVAT>();
        Dictionary<AnimatorState, TransitionVAT[]> StateToVATTransition = new Dictionary<AnimatorState, TransitionVAT[]>();
        //store and remove all transition so animation will be baked without them
        for (int i = 0; i < states.Length; i++)
        {
            StateToTransition.Add(states[i].state, new AnimatorStateTransition[states[i].state.transitions.Length]);
            StateToVATTransition.Add(states[i].state, new TransitionVAT[states[i].state.transitions.Length]);
            for (int j = 0; j < StateToTransition[states[i].state].Length; j++)
            {
                StateToTransition[states[i].state][j] = Instantiate(states[i].state.transitions[j]);
                StateToTransition[states[i].state][j].name = states[i].state.transitions[j].name;
            }
            for (int j = 0; j < states[i].state.transitions.Length; j++)
            {
                states[i].state.RemoveTransition(states[i].state.transitions[j]);
            }
        }
        float startTime = 0;
        for (int i = 0; i < stateMachine.states.Length; i++)
        {
            AnimatorState state = stateMachine.states[i].state;
            AnimationClip clip = (AnimationClip)state.motion;
            StateToVat.Add(state, CreateVATObject(subFolderPath, clip.name, clip.length, clip.events, clip.isLooping));
        }
        AnimatorControllerVAT animatorController = CreateInstance<AnimatorControllerVAT>();
        animatorController.States = new VATState[states.Length];

        for (int i = 0; i < animatorController.States.Length; i++)
        {
            int transitionCount = StateToVATTransition[states[i].state].Length;
            animatorController.States[i] = new VATState(states[i].state.name, StateToVat[states[i].state], transitionCount);
        }
        AnimatorState defaultState = stateMachine.defaultState;

        for (int i = 0; i < states.Length; i++)
        {
            for (int j = 0; j < StateToTransition[states[i].state].Length; j++)
            {
                stateMachine.defaultState = states[i].state;

                AnimatorStateTransition animTransition = StateToTransition[states[i].state][j];
                TransitionVAT transitionVat = CreateInstance<TransitionVAT>();
                transitionVat.From = animatorController.States[i];
                transitionVat.To = animatorController.GetState(animTransition.destinationState.name);
                transitionVat.FromTransitionStart = states [i].state.motion.averageDuration * animTransition.exitTime;
                transitionVat.Length = animTransition.duration;
                transitionVat.ToTransitionStart = animTransition.destinationState.motion.averageDuration * animTransition.offset;
                string transitionName = states[i].state.name + "-" + animTransition.destinationState.name;

                string transitionAssetName = "VAT_Transition_" + transitionName;
                string transitionAnimationName = transitionVat.From.StateName + "_TO_" + transitionVat.To.StateName;
                animatorController.States[i].Transitions[j] = transitionVat;
                transitionVat.Transition = CreateVATObject(subFolderPath, transitionAnimationName, transitionVat.Length, new AnimationEvent[0], false);
                AssetDatabase.CreateAsset(transitionVat, Path.Combine(subFolderPath, transitionAssetName + ".asset"));
            }
        }
        stateMachine.defaultState = defaultState;
        animatorController.BoundsScale = skin.transform.localScale.x;
        AssetDatabase.CreateAsset(animatorController, Path.Combine(subFolderPath, "VAT_CONTROLLER_" + name + ".asset"));

        List<VertInfo> totalVertexData = new List<VertInfo>();
        // gather vertex positions

        float animStart = 0;
        for (int i = 0; i < states.Length; i++)
        {
            AnimatorState state = states[i].state;
            stateMachine.defaultState = state;
            totalVertexData.AddRange(GetClipData(animator, state, skin, mesh, vCount));
            StateToVat[state].StartTime = animStart;
            animStart += StateToVat[state].Duration;
            for (int j = 0; j < StateToTransition[state].Length; j++)
            {
                state.AddTransition(StateToTransition[state][j]);
                totalVertexData.AddRange(GetClipData(animator, state, skin, mesh, vCount));
                state.RemoveTransition(StateToTransition[state][j]);
                animatorController.States[i].Transitions[j].Transition.StartTime = startTime;
                animStart += StateToVat[state].Duration;
            }
        }
        float totalTime = animStart;
        for (int i = 0; i < states.Length; i++)
        {
            AnimatorState state = states[i].state;
            StateToVat[state].StartTime = StateToVat[state].StartTime/totalTime;
            for (int j = 0; j < StateToTransition[state].Length; j++)
            {
                float temp = animatorController.States[i].Transitions[j].Transition.StartTime;
                animatorController.States[i].Transitions[j].Transition.StartTime = temp / totalTime;
            }
        }


        stateMachine.defaultState = defaultState;
        // apply old transitions
        for (int i = 0; i < states.Length; i++)
        {
            for (int j = 0; j < StateToTransition[states[i].state].Length; j++)
            {
                states[i].state.AddTransition(StateToTransition[states[i].state][j]);
            }
        }

        Texture2D[] VATTextures = CreateVatTextures(texWidth, animatorController, vCount, subFolderPath, totalVertexData);
        animatorController.VATPosition = VATTextures[0];
        animatorController.VATNormal = VATTextures[1];
        animatorController.VATTangent = VATTextures[2];

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
    public List<VertInfo> GetClipData(Animator anim,AnimatorState state, SkinnedMeshRenderer skin, Mesh mesh, int vCount)
    {
        List<VertInfo> infoList = SampleAnimator(anim, state, skin, mesh, vCount);
        return infoList;
    }

    public List<VertInfo> GetClipData(AnimationClip clip, SkinnedMeshRenderer skin, Mesh mesh, int vCount, GameObject modelObject)
    {
        int frames = Mathf.NextPowerOfTwo((int)(clip.length / AnimDelta));
        int dt = (int)(clip.length / frames);

        List<VertInfo> infoList = SampleAnimation(clip, skin, mesh, frames, dt, vCount, modelObject);
        return infoList;
    }

    private List<VertInfo> SampleAnimation(AnimationClip clip, SkinnedMeshRenderer skin, Mesh mesh, int frames, float deltaTime, int vCount, GameObject modelObject)
    {
        List<VertInfo> infoList = new List<VertInfo>();
        for (var i = 0; i < frames; i++)
        {
            clip.SampleAnimation(modelObject, deltaTime * i);
            skin.BakeMesh(mesh, true);

            infoList.AddRange(Enumerable.Range(0, vCount)
                .Select(idx => new VertInfo()
                {
                    position = mesh.vertices[idx] * skin.transform.localScale.x,
                    normal = mesh.normals[idx] * skin.transform.localScale.y,
                    tangent = mesh.tangents[idx] * skin.transform.localScale.z
                })
            );
        }
        return infoList;
    }
    public List<VertInfo> SampleAnimator(Animator anim, AnimatorState state, SkinnedMeshRenderer skin, Mesh mesh, int vCount)
    {
       
        AnimationClip clip = (AnimationClip)state.motion;
        var frames = Mathf.NextPowerOfTwo((int)(clip.length / AnimDelta));
        var dt = clip.length / frames;
        var infoList = new List<VertInfo>();
        anim.Play(state.name);
        Debug.Log("Play state " + state.name);
        for (var i = 0; i < frames; i++)
        {
            anim.Update(dt);
            skin.BakeMesh(mesh);
            infoList.AddRange(Enumerable.Range(0, vCount)
                .Select(idx => new VertInfo()
                {
                    position = mesh.vertices[idx],
                    normal = mesh.normals[idx],
                    tangent = mesh.tangents[idx]
                })
            );
        }
        return infoList;
    }

    // TODO: get vert info for transitions
    public void CreateVATTransitionTexture(Animator anim, AnimatorState state, int texWidth, SkinnedMeshRenderer skin, Mesh mesh, int vCount, string subFolderPath, string transitionName, ComputeShader shader, TransitionVAT transition)
    {
        AnimationClip clip = (AnimationClip)state.motion;
        Debug.Log("transition leng1 = " + transition.Length);
        var frames = Mathf.NextPowerOfTwo((int)(transition.Length / AnimDelta));
        var dt = transition.Length / frames;

        transition.Length = frames * dt;

        Debug.Log("transition leng2 = " + transition.Length);
        Debug.Log("transition ToStart = " + transition.ToTransitionStart);

        var infoList = new List<VertInfo>();
        anim.Rebind();
        anim.Play(state.name);
        anim.Update(transition.FromTransitionStart - dt);

        skin.BakeMesh(mesh, true);
        for (var i = 0; i < frames; i++)
        {
            anim.Update(dt);
            skin.BakeMesh(mesh, true);

            infoList.AddRange(Enumerable.Range(0, vCount)
                .Select(idx => new VertInfo()
                {
                    position = mesh.vertices[idx] * skin.transform.localScale.x,
                    normal = mesh.normals[idx] * skin.transform.localScale.y,
                    tangent = mesh.tangents[idx] * skin.transform.localScale.z
                })
            );
        }
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
        RenderTexture[] textures = CreateTextures(texWidth, frames, model.name);
        GenerateVATTextures(textures, totalVerts, this.infoTexGen,vCount, frames);
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
    void GenerateVATTextures(RenderTexture[] textures, List<VertInfo> infoList, ComputeShader shader, int vCount, int frames)
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
            tempEvent.Time = events[i].time / length;
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
    struct VATAnimData
    {
        public List<VertInfo> VertexData;
        public float Length;
        public int StartFrame;
        public AnimationEvent events;
    }
}
