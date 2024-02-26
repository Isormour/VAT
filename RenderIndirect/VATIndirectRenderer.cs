using System;
using System.Collections.Generic;
using UnityEngine;


public class VATIndirectRenderer : MonoBehaviour 
{
    public static VATIndirectRenderer Instance;
    Dictionary<Type, Dictionary<Tuple<Mesh, Material>,VATGroupRenderer>> RenderersByStructType;

    internal void AddObjectToRender<T>(AnimatorVATIndirect animatorVATIndirect)
    {
        if (!RenderersByStructType.ContainsKey(typeof(T)))
        {
            RenderersByStructType.Add(typeof(T), new Dictionary<Tuple<Mesh, Material>, VATGroupRenderer>());
        }
        
        Dictionary<Tuple<Mesh, Material>, VATGroupRenderer> structGroups = RenderersByStructType[typeof(T)];
       
        Material mat = animatorVATIndirect.mat;
        Mesh mesh = animatorVATIndirect.mesh;
        Tuple<Mesh, Material> keyPair = new Tuple<Mesh, Material>(mesh,mat);
        if (!structGroups.ContainsKey(keyPair))
        {
            structGroups.Add(keyPair, new VATGroupRenderer(keyPair));
        }
        structGroups[keyPair].AddAnimator(animatorVATIndirect);
    }

    void Awake()
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
