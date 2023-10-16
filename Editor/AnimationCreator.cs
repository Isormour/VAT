using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.IO;

public class AnimationCreator : EditorWindow
{
    public ComputeShader infoTexGen;
    public Material animBaseMaterial;
    public AnimationClip[] clips;
    public GameObject model;

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

        if (GUILayout.Button("Bake"))
        {
            Bake();
        }
    }

    void Bake()
    {
        var animator = model.GetComponent<Animator>();
        clips = animator.runtimeAnimatorController.animationClips;
        var skin = model.GetComponentInChildren<SkinnedMeshRenderer>();
        var vCount = skin.sharedMesh.vertexCount;
        var texWidth = Mathf.NextPowerOfTwo(vCount);
        var mesh = new Mesh();
        name = model.name;
        foreach (var clip in clips)
        {
            var frames = Mathf.NextPowerOfTwo((int)(clip.length / 0.05f));
            var dt = clip.length / frames;
            var infoList = new List<VertInfo>();

            var pRt = new RenderTexture(texWidth, frames, 0, RenderTextureFormat.ARGBHalf);
            pRt.name = string.Format("{0}.{1}.posTex", name, clip.name);
            var nRt = new RenderTexture(texWidth, frames, 0, RenderTextureFormat.ARGBHalf);
            nRt.name = string.Format("{0}.{1}.normTex", name, clip.name);
            var tRt = new RenderTexture(texWidth, frames, 0, RenderTextureFormat.ARGBHalf);
            tRt.name = string.Format("{0}.{1}.tangentTex", name, clip.name);

            foreach (var rt in new[] { pRt, nRt, tRt })
            {
                rt.enableRandomWrite = true;
                rt.Create();
                RenderTexture.active = rt;
                GL.Clear(true, true, Color.clear);
            }

            for (var i = 0; i < frames; i++)
            {
                clip.SampleAnimation(model, dt * i);
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
            var buffer = new ComputeBuffer(infoList.Count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(VertInfo)));
            buffer.SetData(infoList.ToArray());

            var kernel = infoTexGen.FindKernel("CSMain");
            uint x, y, z;
            infoTexGen.GetKernelThreadGroupSizes(kernel, out x, out y, out z);

            infoTexGen.SetInt("VertCount", vCount);
            infoTexGen.SetBuffer(kernel, "Info", buffer);
            infoTexGen.SetTexture(kernel, "OutPosition", pRt);
            infoTexGen.SetTexture(kernel, "OutNormal", nRt);
            infoTexGen.SetTexture(kernel, "OutTangent", tRt);
            infoTexGen.Dispatch(kernel, vCount / (int)x + 1, frames / (int)y + 1, 1);

            buffer.Release();

#if UNITY_EDITOR

           
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

            var posTex = RenderTextureToTexture2D.Convert(pRt);
            var normTex = RenderTextureToTexture2D.Convert(nRt);
            var tanTex = RenderTextureToTexture2D.Convert(tRt);

            Graphics.CopyTexture(pRt, posTex);
            Graphics.CopyTexture(nRt, normTex);
            Graphics.CopyTexture(tRt, tanTex);

            AssetDatabase.CreateAsset(posTex, Path.Combine(subFolderPath, pRt.name + ".asset"));
            AssetDatabase.CreateAsset(normTex, Path.Combine(subFolderPath, nRt.name + ".asset"));
            AssetDatabase.CreateAsset(tanTex, Path.Combine(subFolderPath, tRt.name + ".asset"));



            AnimationVAT VATObject = CreateInstance<AnimationVAT>();
            VATObject.VATTexture = posTex;
            VATObject.VATNormal = normTex;
            VATObject.VATTangent = tanTex;
            VATObject.Duration = clip.averageDuration;

            AnimationVAT.VATEvent[] events =  new AnimationVAT.VATEvent[clip.events.Length];
            for (int i = 0; i < clip.events.Length; i++)
            {
                AnimationVAT.VATEvent tempEvent = new AnimationVAT.VATEvent();
                tempEvent.Time = clip.events[i].time/clip.length;
                tempEvent.Name = clip.events[i].functionName;
                events[i] = tempEvent;
            }

            VATObject.Events = events;
            VATObject.IsLooped = clip.isLooping;

            posTex.filterMode = FilterMode.Point;
            normTex.filterMode = FilterMode.Point;
            tanTex.filterMode = FilterMode.Point;

            if (!clip.isLooping)
            {
                posTex.wrapMode = TextureWrapMode.Clamp;
                normTex.wrapMode = TextureWrapMode.Clamp;
                tanTex.wrapMode = TextureWrapMode.Clamp;
            }

            AssetDatabase.CreateAsset(VATObject, Path.Combine(subFolderPath, "VAT_" + clip.name + ".asset"));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
        }
    }
}
