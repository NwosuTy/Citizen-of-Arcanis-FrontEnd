using UnityEngine;
using UnityEngine.UI;

public class UIBar : MonoBehaviour
{
    private Camera mainCamera;
    private Transform character;

    [SerializeField] private Slider slider;

    public void SetBillboard(Camera mc, Transform t, float value)
    {
        character = t;
        mainCamera = mc;
    
        SetMaxValue(value);
    }

    public void SetMaxValue(float value)
    {
        slider.maxValue = value;
        slider.value = value;
    }

    public void SetCurrentValue(float value)
    {
        slider.value = value;
    }

    private void LateUpdate()
    {
        if(mainCamera != null && character != null)
        {
            transform.LookAt(mainCamera.transform);
            transform.position = character.position + new Vector3(0, 2.15f, 0);
        }
    }
}
