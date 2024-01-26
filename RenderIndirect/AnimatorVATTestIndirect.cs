using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorVATTestIndirect : MonoBehaviour
{
    [SerializeField] Mesh mesh;
    [SerializeField] AnimatorControllerVAT animatorController;
    public Material mat;
    [SerializeField] float TimeSpeedMult = 0.1f;
    AnimatorVATIndirect vat;
    void Start()
    {
        MaterialPropertyBlock matProperties = new MaterialPropertyBlock();
        vat = new AnimatorVATIndirect(matProperties,this.transform,mat,mesh,animatorController);
    }

    // Update is called once per frame
    void Update()
    {
        vat.Update(Time.deltaTime * TimeSpeedMult);
        if (Input.GetKeyDown(KeyCode.Q))
        {
            vat.Play("RunningState");
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            vat.Play("AttackState");
        }
    }
}
