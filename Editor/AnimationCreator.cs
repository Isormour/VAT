using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;

public class AnimationCreator : EditorWindow
{

    public ComputeShader infoTexGen;
    public Material animBaseMaterial;
    public AnimationClip[] clips;
    public GameObject model;

    public struct VertInfo
    {
        public Vector3 position;
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
            infoTexGen = AssetDatabase.LoadAssetAtPath<ComputeShader>("Packages/com.db.VAT/MeshInfoTextureGen.compute");
        }
        if (animBaseMaterial == null)
        {
            animBaseMaterial = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.db.VAT/AnimatedMatBasic.mat");
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

            foreach (var rt in new[] { pRt })
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

                var verexArry = mesh.vertices;
                infoList.AddRange(Enumerable.Range(0, vCount)
                    .Select(idx => new VertInfo()
                    {
                        position = verexArry[idx],
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
            Graphics.CopyTexture(pRt, posTex);

            var mat = new Material(animBaseMaterial);
            mat.SetTexture("_MainTex", skin.sharedMaterial.mainTexture);
            mat.SetTexture("_PosTex", posTex);
            mat.SetFloat("_Length", clip.length);
            if (clip.wrapMode == WrapMode.Loop)
            {
                mat.SetFloat("_Loop", 1f);
                mat.EnableKeyword("ANIM_LOOP");
            }

            var go = new GameObject(name + "." + clip.name);
            go.AddComponent<MeshRenderer>().sharedMaterial = mat;
            go.AddComponent<MeshFilter>().sharedMesh = skin.sharedMesh;

            AssetDatabase.CreateAsset(posTex, Path.Combine(subFolderPath, pRt.name + ".asset"));
            AssetDatabase.CreateAsset(mat, Path.Combine(subFolderPath, string.Format("{0}.{1}.animTex.asset", name, clip.name)));
            PrefabUtility.SaveAsPrefabAsset(go, Path.Combine(folderPath, go.name + ".prefab").Replace("\\", "/"));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
#endif
        }
    }
}
