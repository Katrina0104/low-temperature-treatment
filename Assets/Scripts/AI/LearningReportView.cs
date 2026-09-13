using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 世界空間的學習報告面板：總覽數據 + 教學瓶頸熱圖 + 跳轉復習。
///
/// 注意：這個元件必須掛在「一直保持啟用」的物件上，panel 指向要開關的子物件。
/// 若直接掛在 panel 本身，panel 被關閉後 Awake 與事件都不會執行。
/// </summary>
public class LearningReportView : MonoBehaviour
{
    [Header("UI 引用")]
    public GameObject panel;                 // 整張報告面板的 root（本元件的子物件）
    public TextMeshProUGUI summaryText;      // 上方總覽
    public Transform rowContainer;           // ScrollView 的 Content
    [Tooltip("單列模板 Row.prefab。子物件名稱必須是 Name / Stat / HeatBar / ReviewButton")]
    public GameObject rowPrefab;
    public Button closeButton;               // 「結束」

    [Header("熱圖配色")]
    public Color okColor = new Color(0.30f, 0.75f, 0.45f);
    public Color badColor = new Color(0.85f, 0.25f, 0.25f);

    [Header("事件")]
    public UnityEvent onClosed;              // 關閉報告後要做什麼（可留空）

    private readonly List<GameObject> spawnedRows = new List<GameObject>();
    private bool initialized;

    void Awake()
    {
        Init();
        if (panel != null) panel.SetActive(false);
    }

    private void Init()
    {
        if (initialized) return;
        initialized = true;
        if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
    }

    public void Show(List<AIController.ReportRow> rows, bool practiceMode, float duration,
                     int decisionCount, int errorCount, float errorRate, AIController ai)
    {
        Init();
        if (panel == null)
        {
            Debug.LogWarning("[LearningReportView] panel 未指派，報告無法顯示");
            return;
        }
        panel.SetActive(true);

        if (rowPrefab == null)
            Debug.LogError("[LearningReportView] rowPrefab 未指派，報告只會顯示總覽、列不出步驟。請拉入 Row.prefab");

        if (summaryText != null)
            summaryText.text =
                $"{(practiceMode ? "測驗" : "學習")}結果\n" +
                $"總時長 {duration / 60f:F1} 分鐘\n" +
                $"決策 {decisionCount} 次 | 錯誤 {errorCount} 次 | 錯誤率 {errorRate:P0}";

        EnsureContainerLayout();
        ClearRows();
        foreach (var r in rows) BuildRow(r, ai);
    }

    /// <summary>
    /// Scroll View 的 Content 若沒有排版元件，生成的列會全部疊在同一點。
    /// 這裡只在缺少時補上，你自己在 Inspector 加過就不會被覆蓋。
    /// </summary>
    private void EnsureContainerLayout()
    {
        if (rowContainer == null) return;

        // 最常見的誤接：rowContainer 拉成 Scroll View，而不是它底下的 Content。
        // 這會讓列長在 Viewport 外面不被裁切，排版元件也會和 ScrollRect 打架。
        var scroll = rowContainer.GetComponent<ScrollRect>();
        if (scroll != null)
        {
            if (scroll.content == null)
            {
                Debug.LogError("[報告] rowContainer 指到 Scroll View，但該 ScrollRect 沒有設定 Content");
                return;
            }
            Debug.LogWarning($"<color=orange>[報告]</color> rowContainer 指到的是 Scroll View，" +
                             $"已自動改用它的 Content（'{scroll.content.name}'）。" +
                             $"請在 Inspector 把 rowContainer 改拉 Scroll View/Viewport/Content。");
            rowContainer = scroll.content;
        }

        if (rowContainer.GetComponent<VerticalLayoutGroup>() == null)
        {
            var vl = rowContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.spacing = 4f;
            vl.padding = new RectOffset(4, 4, 4, 4);
            vl.childAlignment = TextAnchor.UpperLeft;
            vl.childControlWidth = true;
            vl.childControlHeight = true;
            vl.childForceExpandWidth = true;
            vl.childForceExpandHeight = false;
        }

        if (rowContainer.GetComponent<ContentSizeFitter>() == null)
        {
            var fitter = rowContainer.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void OnCloseClicked()
    {
        Hide();
        onClosed?.Invoke();
    }

    private void ClearRows()
    {
        foreach (var go in spawnedRows) if (go != null) Destroy(go);
        spawnedRows.Clear();
    }

    /// <summary>
    /// 遞迴依名稱尋找子孫物件。
    /// Transform.Find 只找直接子物件，這樣 Row.prefab 就不能有中間層
    /// （例如把 Name 和 ReviewButton 包進一個 TopLine 做成兩行版面）。
    /// 用遞迴之後 prefab 可以自由分層，只要名稱對得上就抓得到。
    /// </summary>
    private static Transform FindDeep(Transform root, string name)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform c = root.GetChild(i);
            if (c.name == name) return c;

            Transform found = FindDeep(c, name);
            if (found != null) return found;
        }
        return null;
    }

    private void BuildRow(AIController.ReportRow r, AIController ai)
    {
        if (rowContainer == null || rowPrefab == null) return;

        GameObject go = Instantiate(rowPrefab, rowContainer);
        go.SetActive(true);
        spawnedRows.Add(go);

        var nameText  = FindDeep(go.transform, "Name")?.GetComponent<TextMeshProUGUI>();
        var statText  = FindDeep(go.transform, "Stat")?.GetComponent<TextMeshProUGUI>();
        var heatBar   = FindDeep(go.transform, "HeatBar")?.GetComponent<Image>();
        var reviewBtn = FindDeep(go.transform, "ReviewButton")?.GetComponent<Button>();
        var reviewTxt = reviewBtn != null ? reviewBtn.GetComponentInChildren<TextMeshProUGUI>() : null;

        if (nameText != null)
            // <space=2em> 是 TMP 的排版標籤，插入兩個字寬的空白。
            // 不用全形空白「　」是因為它不在字集檔裡，重新烘焙成靜態圖集後會變豆腐。
            nameText.text = "<space=2em>" + r.displayName + (r.reviewPassed ? "  <color=#4CAF50>✓</color>" : "");

        if (statText != null)
            statText.text = $"錯 {r.wrongCount} | 提示 {r.hintCount} | 熟悉度 {r.familiarity:F2} | {r.totalSeconds:F0}s";

        if (heatBar != null)
            heatBar.color = Color.Lerp(okColor, badColor, r.HeatLevel);

        if (reviewBtn != null)
        {
            if (r.reviewable)
            {
                reviewBtn.gameObject.SetActive(true);
                reviewBtn.onClick.RemoveAllListeners();
                string target = r.label;                 // 避免閉包捕捉迴圈變數
                reviewBtn.onClick.AddListener(() => ai.StartReview(target));
                if (reviewTxt != null) reviewTxt.text = "復習";
            }
            else
            {
                // 物理操作步驟依賴不可逆場景狀態，無法單步復習
                reviewBtn.gameObject.SetActive(false);
                if (statText != null) statText.text += "（需重跑完整流程）";
            }
        }
    }

}
