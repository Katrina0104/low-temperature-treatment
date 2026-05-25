using UnityEngine;
using TMPro;

public class RateValueController : MonoBehaviour
{
    public TextMeshProUGUI rateDisplay;
    public string label = "速率";
    public float currentRate = 0.5f; // 預設每小時 0.5 度
    public float step = 0.05f;       // 每次加減 0.1
    public float minRate = 0.1f;
    public float maxRate = 2.0f;

    void Start() => UpdateDisplay();

    public void AddRate()
    {
        currentRate = Mathf.Min(currentRate + step, maxRate);
        UpdateDisplay();
    }

    public void SubRate()
    {
        currentRate = Mathf.Max(currentRate - step, minRate);
        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        if (rateDisplay != null)
            rateDisplay.text = $"{label}:{currentRate:F1}°C/hr";
    }
}