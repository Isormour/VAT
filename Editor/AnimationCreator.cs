using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;


public class AnimationCreator : EditorWindow
{
    public ComputeShader infoTexGen;
    public ComputeShader infoTransitionGen;
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

    [MenuItem("DB/VATBaker")]

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
        infoTransitionGen = (ComputeShader)EditorGUILayout.ObjectField("infoTransitionGen", infoTransitionGen, typeof(ComputeShader), false);
        animBaseMaterial = (Material)EditorGUILayout.ObjectField("playShader", animBaseMaterial, typeof(Material), false);
        model = (GameObject)EditorGUILayout.ObjectField("model", model, typeof(GameObject), true);
    }
    void Setup()
    {
        if (infoTexGen == null)
        {
            infoTexGen = AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.db.VAT/Graphics/MeshInfoTextureGen.compute");
        }
        if (infoTransitionGen == null)
        {
            infoTransitionGen = AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.db.VAT/Graphics/InfoTransitionGen.compute");
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
        if (infoTransitionGen == null) return;
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

        foreach (var clip in clips)
        {
            int frames = Mathf.NextPowerOfTwo((int)(clip.length / AnimDelta));
            int dt = (int)(clip.length / frames);

            List<VertInfo> verts = SampleAnimation(clip, skin, mesh, vCount, frames, dt, model.gameObject);
            RenderTexture[] textures = CreateTextures(texWidth, frames, model.name, clip.name);
            GenerateVATTextures(textures[0], textures[1], textures[2], verts, this.infoTexGen, vCount, frames);
            Texture2D[] VATTextures = CreateVATTextureAssets(textures[0], textures[1], textures[2], subFolder, clip.name, clip.length, clip.events, clip.isLooping);
            AnimationVAT animationVat = CreateVATObject(subFolder, frames * dt, clip.events, clip.isLooping);
            animationVat.VATTexture = VATTextures[0];
            animationVat.VATNormal = VATTextures[1];
            animationVat.VATTangent = VATTextures[2];
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
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

        var controller = animator.runtimeAnimatorController;
        var stateMachine = ((UnityEditor.Animations.AnimatorController)controller).layers[0].stateMachine;
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

        foreach (var mState in stateMachine.states)
        {
            AnimationClip clip = (AnimationClip)mState.state.motion;
            StateToVat.Add(mState.state, CreateVATObject(subFolderPath, clip.length, clip.events, clip.isLooping));
        }

        //Apply old transitions
        for (int i = 0; i < states.Length; i++)
        {
            for (int j = 0; j < StateToTransition[states[i].state].Length; j++)
            {
                states[i].state.AddTransition(StateToTransition[states[i].state][j]);
            }
        }

        AnimatorControllerVAT animatorController = CreateInstance<AnimatorControllerVAT>();
        animatorController.States = new VATState[states.Length];

        for (int i = 0; i < animatorController.States.Length; i++)
        {
            animatorController.States[i] = new VATState();
            animatorController.States[i].StateName = states[i].state.name;
            animatorController.States[i].VAT = StateToVat[states[i].state];
            int transitionCount = StateToVATTransition[states[i].state].Length;
            animatorController.States[i].Transitions = new TransitionVAT[transitionCount];
        }
        AnimatorState defaultState = stateMachine.defaultState;
        //create vat textures for transitions
        for (int i = 0; i < states.Length; i++)
        {
            for (int j = 0; j < StateToTransition[states[i].state].Length; j++)
            {
                stateMachine.defaultState = states[i].state;

                AnimatorStateTransition animTransition = StateToTransition[states[i].state][j];
                TransitionVAT transitionVat = CreateInstance<TransitionVAT>();
                transitionVat.From = animatorController.States[i];
                transitionVat.To = animatorController.GetState(animTransition.destinationState.name);
                transitionVat.FromTransitionStart = states[i].state.motion.averageDuration * animTransition.exitTime;
                transitionVat.Length = animTransition.duration;
                transitionVat.ToTransitionStart = animTransition.destinationState.motion.averageDuration * animTransition.offset;
                string transitionName = states[i].state.name + "-" + animTransition.destinationState.name;
                CreateVATObject(subFolderPath, transitionVat.Length, new AnimationEvent[0], false);
                string assetName = "VAT_Transition_" + transitionName;
                animatorController.States[i].Transitions[j] = transitionVat;
                AssetDatabase.CreateAsset(transitionVat, Path.Combine(subFolderPath, assetName + ".asset"));
            }
        }
        stateMachine.defaultState = defaultState;
        animatorController.BoundsScale = skin.transform.localScale.x;
        AssetDatabase.CreateAsset(animatorController, Path.Combine(subFolderPath, "VAT_CONTROLLER_" + name + ".asset"));

    }
    public List<VertInfo> GetClipData(AnimationClip clip, SkinnedMeshRenderer skin, Mesh mesh, int vCount, string subFolderPath, string modelName, GameObject modelObject, ComputeShader shader)
    {
        int frames = Mathf.NextPowerOfTwo((int)(clip.length / AnimDelta));
        int dt = (int)(clip.length / frames);

        List<VertInfo> infoList = SampleAnimation(clip, skin, mesh, frames, dt, vCount, modelObject);
        return infoList;
        // RenderTexture[] rts = CreateTextures(texWidth, frames, modelName, clip.name);

        // GenerateVATTextures(rts[0], rts[1], rts[2], infoList, shader, vCount, frames);
        // CreateVATTextureAssets(rts[0], rts[1], rts[2], subFolderPath, clip.name, clip.length, clip.events, clip.isLooping);
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
            //add to global texture
        }
        return infoList;
    }

    public void CreateVATTransitionTexture(Animator anim, AnimatorState state, SkinnedMeshRenderer skin, Mesh mesh, int vCount, string subFolderPath, string transitionName, TransitionVAT transition)
    {
        AnimationClip clip = (AnimationClip)state.motion;
        Debug.Log("transition leng1 = " + transition.Length);
        var frames = Mathf.NextPowerOfTwo((int)(transition.Length / AnimDelta));
        var dt = transition.Length / frames;

        transition.Length = frames * dt;

        Debug.Log("transition leng2 = " + transition.Length);
        Debug.Log("transition ToStart = " + transition.ToTransitionStart);
        List<VertInfo> infoList = SampleAnimator(anim, state, skin, mesh, vCount);


        //RenderTexture[] rts = CreateTextures(texWidth, frames, "VAT_TransitionTexture_", transitionName);
        //GenerateVATTextures(rts[0], rts[1], rts[2], infoList, shader, vCount, frames);
        //transition.Transition = CreateVATAssets(rts[0], rts[1], rts[2], subFolderPath, transitionName,transition.Length,new AnimationEvent[0], false);

    }
    public List<VertInfo> SampleAnimator(Animator anim, AnimatorState state, SkinnedMeshRenderer skin, Mesh mesh, int vCount)
    {
        AnimationClip clip = (AnimationClip)state.motion;
        var frames = Mathf.NextPowerOfTwo((int)(clip.length / AnimDelta));
        var dt = clip.length / frames;
        var infoList = new List<VertInfo>();
        anim.Play(state.name);

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

    RenderTexture[] CreateTextures(int texWidth, int frames, string modelName, string animName)
    {
        var pRt = new RenderTexture(texWidth, frames, 0, RenderTextureFormat.ARGBHalf);
        pRt.name = string.Format("{0}.{1}.posTex", modelName, animName);
        var nRt = new RenderTexture(texWidth, frames, 0, RenderTextureFormat.ARGBHalf);
        nRt.name = string.Format("{0}.{1}.normTex", modelName, animName);
        var tRt = new RenderTexture(texWidth, frames, 0, RenderTextureFormat.ARGBHalf);
        tRt.name = string.Format("{0}.{1}.tangentTex", modelName, animName);

        foreach (var rt in new[] { pRt, nRt, tRt })
        {
            rt.enableRandomWrite = true;
            rt.Create();
            RenderTexture.active = rt;
            GL.Clear(true, true, Color.clear);
        }
        return new RenderTexture[] { pRt, nRt, tRt };
    }
    void GenerateVATTextures(RenderTexture positionRT, RenderTexture normalRT, RenderTexture tangentRT, List<VertInfo> infoList, ComputeShader shader, int vCount, int frames)
    {
        var buffer = new ComputeBuffer(infoList.Count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(VertInfo)));
        buffer.SetData(infoList.ToArray());

        var kernel = shader.FindKernel("CSMain");
        uint x, y, z;
        shader.GetKernelThreadGroupSizes(kernel, out x, out y, out z);

        shader.SetInt("VertCount", vCount);
        shader.SetBuffer(kernel, "Info", buffer);
        shader.SetTexture(kernel, "OutPosition", positionRT);
        shader.SetTexture(kernel, "OutNormal", normalRT);
        shader.SetTexture(kernel, "OutTangent", tangentRT);
        shader.Dispatch(kernel, vCount / (int)x + 1, frames / (int)y + 1, 1);

        buffer.Release();
    }
    Texture2D[] CreateVATTextureAssets(RenderTexture positionRT, RenderTexture normalRT, RenderTexture tangentRT, string subFolderPath, string name, float duration, AnimationEvent[] events, bool isLooping)
    {
        var posTex = RenderTextureToTexture2D.Convert(positionRT);
        var normTex = RenderTextureToTexture2D.Convert(normalRT);
        var tanTex = RenderTextureToTexture2D.Convert(tangentRT);

        Graphics.CopyTexture(positionRT, posTex);
        Graphics.CopyTexture(normalRT, normTex);
        Graphics.CopyTexture(tangentRT, tanTex);

        AssetDatabase.CreateAsset(posTex, Path.Combine(subFolderPath, positionRT.name + ".asset"));
        AssetDatabase.CreateAsset(normTex, Path.Combine(subFolderPath, normalRT.name + ".asset"));
        AssetDatabase.CreateAsset(tanTex, Path.Combine(subFolderPath, tangentRT.name + ".asset"));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        return new Texture2D[] { posTex, normTex, tanTex };
    }
    AnimationVAT CreateVATObject(string subFolderPath, float duration, AnimationEvent[] events, bool isLooping)
    {
        AnimationVAT VATObject = CreateInstance<AnimationVAT>();
        VATObject.Duration = duration;

        AnimationVAT.VATEvent[] vatEvents = new AnimationVAT.VATEvent[events.Length];
        for (int i = 0; i < events.Length; i++)
        {
            AnimationVAT.VATEvent tempEvent = new AnimationVAT.VATEvent();
            tempEvent.Time = events[i].time / duration;
            tempEvent.Name = events[i].functionName;
            vatEvents[i] = tempEvent;
        }

        VATObject.Events = vatEvents;
        VATObject.IsLooped = isLooping;


        AssetDatabase.CreateAsset(VATObject, Path.Combine(subFolderPath, "VAT_" + name + ".asset"));
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
