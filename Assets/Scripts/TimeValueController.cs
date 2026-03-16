using UnityEngine;
using TMPro;

public class TimeValueController : MonoBehaviour
{
    [Header("UI 引用")]
    public TextMeshProUGUI timeDisplay; // 拖入對應的 TMP 文字
    public TextMeshProUGUI nameDisplay; // 拖入對應的 TMP 文字（顯示名稱）

    [Header("設定")]
    public string labelName = "降溫時間"; // 例如：降溫時間 或 復溫時間
    public int startTotalMinutes = 0;   // 初始總分鐘數

    // 內部變數：統一儲存為總分鐘 (例如 125 分鐘)
    private int totalMinutes;

    void Awake()
    {
        totalMinutes = startTotalMinutes;
    }

    void Start()
    {
        UpdateDisplay();
    }

    // 增加/減少分鐘（由按鈕調用）
    // 你可以設置一個按鈕傳入 1，另一個按鈕傳入 60 (直接加一小時)
    public void AddMinutes(int amount)
    {
        totalMinutes += amount;

        // 防止時間變為負數
        if (totalMinutes < 0) totalMinutes = 0;

        UpdateDisplay();
    }

    // --- 這是你要求的：處理 60 分鐘進位並顯示的函式 ---
    public void UpdateDisplay()
    {
        if (timeDisplay == null) return;

        // 計算小時：總分鐘除以 60 (整數除法)
        int hours = totalMinutes / 60;

        // 計算剩餘分鐘：總分鐘對 60 取餘數 (MOD)
        // 這樣分鐘永遠會在 0~59 之間循環
        int minutes = totalMinutes % 60;

        // 格式化顯示
        // :D2 代表數字如果只有一位數，前面會自動補 0 (例如 05 分)
        timeDisplay.text = $"{labelName}{hours}:{minutes:D2}";
        nameDisplay.text = $"{labelName}{hours}:{minutes:D2}"; // 如果你想要在另一個 Text 顯示名稱

        // 如果你想要 01:30 這種格式，可以使用：
        // timeDisplay.text = $"{labelName}: {hours:D2}:{minutes:D2}";
    }

    // 方便其他腳本獲取最終的總時間（以分鐘為單位）
    public int GetTotalMinutes() => totalMinutes;
}