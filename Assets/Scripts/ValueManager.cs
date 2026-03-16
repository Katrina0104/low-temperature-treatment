using UnityEngine;
using TMPro; // 必須引用此命名空間才能控制 TextMeshPro

public class ValueManager : MonoBehaviour
{
    // 用於顯示數值的 TMP 組件
    public TextMeshProUGUI valueDisplay;
    public TextMeshProUGUI valueDisplayW;

    // 儲存當前的數值
    private int coolDownValue = 36;
    private int warmUpValue = 36;

    // 在遊戲開始時先更新一次顯示
    void Start()
    {
        UpdateDisplay();
    }

    // 增加數值的方法（供按鈕調用）
    public void IncreaseValue()
    {
        coolDownValue++;
        UpdateDisplay();
    }

    public void IncreaseValueW()
    {
        warmUpValue++;
        UpdateDisplay();
    }

    // 減少數值的方法（供按鈕調用）
    public void DecreaseValue()
    {
        coolDownValue--;
        UpdateDisplay();
    }

    public void DecreaseValueW()
    {
        warmUpValue--;
        UpdateDisplay();
    }

    // 更新 TMP 文字的輔助方法
    void UpdateDisplay()
    {
        valueDisplay.text = "body temp" + coolDownValue.ToString();
        valueDisplayW.text = "body temp" + warmUpValue.ToString();
    }

 
}
