using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class VATIndirectRenderer : MonoBehaviour 
{
    public static VATIndirectRenderer Instance;
   protected Dictionary<Type, Dictionary<Tuple<Mesh, Material>,VATGroupRenderer>> RenderersByStructType;

    internal void AddObjectToRender<T>(AnimatorVATIndirect animatorVATIndirect)
    {
        Dictionary<Tuple<Mesh, Material>, VATGroupRenderer> structGroups = RenderersByStructType[typeof(T)];  
        Material mat = animatorVATIndirect.mat;
        Mesh mesh = animatorVATIndirect.mesh;
        Tuple<Mesh, Material> keyPair = new Tuple<Mesh, Material>(mesh,mat);
        if (!structGroups.ContainsKey(keyPair))
        {
            Debug.LogError("Struct group not defined");
        }
        structGroups[keyPair].AddAnimator(animatorVATIndirect);
    }
    public void AddParamStructGroup(Type structType, Mesh mesh, Material mat,VATGroupRenderer.BufferSetter paramsSetter)
    {
        if (!RenderersByStructType.ContainsKey(structType))
        {
            RenderersByStructType.Add(structType, new Dictionary<Tuple<Mesh, Material>, VATGroupRenderer>());
        }
        Dictionary<Tuple<Mesh, Material>, VATGroupRenderer> structGroups = RenderersByStructType[structType];
        Tuple<Mesh, Material> keyPair = new Tuple<Mesh, Material>(mesh, mat);
        structGroups.Add(keyPair, new VATGroupRenderer(keyPair, structType, paramsSetter));
    }
   
    protected virtual void Awake()
    {
        if (Instance != null)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        RenderersByStructType = new Dictionary<Type, Dictionary<Tuple<Mesh, Material>, VATGroupRenderer>>();
    }

    void OnDestroy()
    {
        foreach (var renderers in RenderersByStructType.Values)
        {
            foreach (var renderer in renderers.Values)
            {
                renderer.Deinitialize();
            }
        }
    }


    void Update()
    {
        foreach (var renderers in RenderersByStructType.Values)
        {
            foreach (var renderer in renderers.Values)
            {
                renderer.DrawGroup();
            }
        }
    }
}
