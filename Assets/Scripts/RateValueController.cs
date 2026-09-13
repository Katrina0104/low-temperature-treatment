using UnityEngine;
using TMPro;

public class RateValueController : MonoBehaviour
{
    public TextMeshProUGUI rateDisplay;
    public string label = "速率";
    public float currentRate = 0.55f; // 預設每小時 0.5 度
    public float step = 0.05f;       // 每次加減 0.05
    public float minRate = 0.1f;
    public float maxRate = 2.0f;

    private float startRate;

    void Awake() => startRate = currentRate;

    void Start() => UpdateDisplay();

    /// <summary>
    /// 回到 Inspector 上設定的初始速率。
    /// 血壓不穩事件每次觸發前呼叫，讓學習者每一天都要重新設定一次。
    /// </summary>
    public void ResetRate()
    {
        currentRate = startRate;
        UpdateDisplay();
    }

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
            rateDisplay.text = $"{label}:{currentRate:F2}°C/hr";
    }
}