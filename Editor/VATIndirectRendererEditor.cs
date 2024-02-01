using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VATIndirectRenderer))]
public class VATIndirectRendererEditor : Editor
{
    GameObject prefab;
    int amount;
    public override void OnInspectorGUI()
    {
        VATIndirectRenderer renderer = (VATIndirectRenderer)target;
        base.OnInspectorGUI();
        amount = EditorGUILayout.IntField("amount", amount);
        prefab =(GameObject)EditorGUILayout.ObjectField("Prefab", prefab, typeof(GameObject),true);
        if (GUILayout.Button("Create Instances"))
        {
            renderer.owners =  CreateDebugInstances(amount);
        }
    }

    AnimatorVATTestIndirect[] CreateDebugInstances(int amount)
    {
        GameObject TempParent = new GameObject("Debug Parent");
        int total = 0;
        AnimatorVATTestIndirect[] Array = new AnimatorVATTestIndirect[(int)Math.Pow(amount,3)];
        for (int i = 0; i < amount; i++)
        {
            for (int j = 0; j < amount; j++)
            {
                for (int k = 0; k < amount; k++)
                {
                    GameObject instance = Instantiate(prefab, TempParent.transform);
                    instance.transform.localPosition = new Vector3(i,j,k);
                    Array[total] = instance.GetComponent<AnimatorVATTestIndirect>();
                    total++;
                    instance.name = "copy " + total;
                    
                }
            }
        }
        return Array;
    }
}
