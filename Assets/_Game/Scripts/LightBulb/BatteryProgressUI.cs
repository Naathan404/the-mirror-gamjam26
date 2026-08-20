using UnityEngine;
using UnityEngine.UI;

public class BatterySliderUI : MonoBehaviour
{
    [SerializeField] private Slider batterySlider;
    [SerializeField] private LightBulbController lightBulbController;

    private void OnEnable()
    {
        if (lightBulbController != null)
        {
            lightBulbController.OnBatteryProgressChanged += UpdateSlider;
        }
    }

    private void OnDisable()
    {
        if (lightBulbController != null)
        {
            lightBulbController.OnBatteryProgressChanged -= UpdateSlider;
        }
    }

    private void UpdateSlider(float progress)
    {
        if (batterySlider != null)
        {
            batterySlider.value = progress;
        }
    }
}