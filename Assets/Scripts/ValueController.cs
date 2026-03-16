using UnityEngine;
using TMPro;

public class ValueController : MonoBehaviour
{
    [Header("UI 引用")]
    public TextMeshProUGUI valueDisplay; // 拖入這組對應的文字
    public TextMeshProUGUI nameDisplay;  // 拖入這組對應的文字（顯示名稱）

    [Header("設定")]
    public string valueName = "數值";      // 給這組數值一個名字
    public int startValue = 36;           // 初始值

    private int currentValue;

    void Awake()
    {
        currentValue = startValue;
    }

    void Start()
    {
        UpdateDisplay();
    }

    // 增加數值（可以在 Inspector 的按鈕事件帶入參數，例如填 1 或 10）
    public void AddValue(int amount)
    {
        currentValue += amount;
        UpdateDisplay();
    }

    // 減少數值
    public void SubtractValue(int amount)
    {
        currentValue -= amount;
        UpdateDisplay();
    }

    // --- 這是你要求的：單獨處理顯示的函式 ---
    public void UpdateDisplay()
    {
        if (valueDisplay != null)
        {
            valueDisplay.text = $"{valueName}{currentValue}";
            nameDisplay.text = $"{valueName}{currentValue}"; // 如果你想要在另一個 Text 顯示名稱
        }
    }

    // 提供給外部腳本讀取數值的方法
    public int GetCurrentValue()
    {
        return currentValue;
    }
}