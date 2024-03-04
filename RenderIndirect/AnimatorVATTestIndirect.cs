using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimatorVATTestIndirect : MonoBehaviour
{
    [SerializeField] AnimatorControllerVATIndirect animatorController;
    [SerializeField] float TimeSpeedMult = 0.1f;
    AnimatorVATIndirect vat;
    public float VATTextureTime{ get { return vat.textureTime; } }
    public Transform CashedTransform { private set; get; }
    void Start()
    {
        CashedTransform = this.transform;
        MaterialPropertyBlock matProperties = new MaterialPropertyBlock();
        vat = new AnimatorVATIndirect(matProperties,this.transform,animatorController);
        vat.SetEnabled(true);
    }

    // Update is called once per frame
    void Update()
    {
        vat.Update(Time.deltaTime * TimeSpeedMult);
    }
}
