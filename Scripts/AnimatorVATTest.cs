using UnityEngine;

public class AnimatorVATTest : MonoBehaviour,IAnimatorVat
{
    [SerializeField] MeshRenderer rend;
    [SerializeField] AnimatorControllerVAT animatorController;
    public AnimatorVAT Vat { private set; get; }
    [SerializeField] float TimeSpeedMult = 0.1f;
    void Start()
    {
        MaterialPropertyBlock matProperties = new MaterialPropertyBlock();
        Vat = new AnimatorVAT(matProperties, rend, animatorController);
    }

    // Update is called once per frame
    void Update()
    {
        Vat.Update(Time.deltaTime * TimeSpeedMult);
        if (Input.GetKeyDown(KeyCode.Q))
        {
            Vat.Play("Run");
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            Vat.Play("Attack");
        }
    }

    public AnimatorVAT GetAnimatorVat()
    {
        return this.Vat;
    }
}