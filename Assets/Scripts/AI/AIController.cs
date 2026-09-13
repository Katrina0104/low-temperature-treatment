using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// HMM 評鑑的控制層：接收狀態轉移、判定對錯、統計錯誤率、產出報告、驅動復習。
/// 掛在與 LearningAnalyticsManager 同一個 GameObject 上。
/// </summary>
public class AIController : MonoBehaviour
{
    public static AIController Instance;

    [Header("系統連結")]
    public VRDialogueController dialogueController;
    public LearningReportView reportView;

    [Header("模式")]
    public bool isPracticeMode = false;

    [Header("即時導引（僅學習模式）")]
    public bool showHintOnError = true;

    [Header("報告輸出")]
    public bool writeJsonReport = true;

    // --- 本次 session 統計 ---
    private int decisionCount, errorCount;
    private string previousState = "";
    private readonly List<string> errorTrail = new List<string>();
    private float sessionStartTime;
    private bool sessionActive;

    // --- 復習狀態 ---
    private bool isReviewing;
    private string reviewSection = "";                                     // 報告列的標籤，記錄結果用
    private readonly HashSet<string> reviewScope = new HashSet<string>();  // 這一段復習涵蓋的標籤
    private readonly Dictionary<string, bool> reviewResults = new Dictionary<string, bool>();

    // --- 提示使用次數（逐步驟的次數記在 StepStat 裡，這裡只留總計）---
    private int totalHints;

    public float ErrorRate => decisionCount == 0 ? 0f : (float)errorCount / decisionCount;
    public bool IsReviewing => isReviewing;
    public bool WasReviewPassed(string label)
        => reviewResults.TryGetValue(label, out bool ok) && ok;

    void Awake() => Instance = this;

    // ==================== Session ====================

    public void BeginSession(bool practiceMode)
    {
        isPracticeMode = practiceMode;
        decisionCount = errorCount = 0;
        previousState = "";
        errorTrail.Clear();
        reviewResults.Clear();
        totalHints = 0;
        isReviewing = false;
        reviewScope.Clear();
        sessionStartTime = Time.time;
        sessionActive = true;

        if (LearningAnalyticsManager.Instance != null)
            LearningAnalyticsManager.Instance.ResetSession();

        Debug.Log($"<color=cyan>[AI]</color> Session 開始，模式：{(practiceMode ? "測驗" : "學習")}");
    }

    /// <summary>
    /// 由 VRDialogueController 在每次進入 :: 狀態時呼叫。
    /// 回傳 false 表示要中止對話流程（復習結束、返回報告）。
    /// </summary>
    public bool OnEnterState(string label)
    {
        if (!sessionActive) return true;

        var lam = LearningAnalyticsManager.Instance;
        if (lam == null)
        {
            Debug.LogWarning("<color=orange>[AI]</color> 找不到 LearningAnalyticsManager.Instance");
            return true;
        }

        // ---------- 復習模式 ----------
        if (isReviewing)
        {
            // 流程走出這個步驟涵蓋的標籤 = 這一段練完了，中止流程回報告。
            // 回傳 false 時，範圍外那一段的台詞不會被顯示出來。
            if (!reviewScope.Contains(label))
            {
                EndReview(true);
                return false;
            }

            // 範圍內的轉移只記 log，不寫入評鑑統計（避免改寫玩家剛看到的熱圖）
            if (!string.IsNullOrEmpty(previousState) && label != previousState
                && !lam.PeekTransition(previousState, label))
            {
                Debug.Log($"<color=cyan>[AI 復習]</color> {previousState} -> {label}：答錯，重試中");
            }

            previousState = label;
            return true;
        }

        // ---------- 正式評鑑 ----------
        if (string.IsNullOrEmpty(previousState))
        {
            lam.EnterState(label);
            previousState = label;
            return true;
        }

        // 自我迴圈 = [IF] 未通過的原地等待。不計決策、不呼叫 EnterState，
        // 讓停留時間持續累積成一筆完整 interval。
        if (label == previousState) return true;

        bool ok = lam.CheckTransition(label);
        decisionCount++;

        if (!ok)
        {
            errorCount++;
            errorTrail.Add($"{StepCatalog.DisplayName(previousState)} -> {StepCatalog.DisplayName(label)}");
            Debug.Log($"<color=red>[AI]</color> 流程錯誤 {previousState} -> {label} | " +
                      $"累計錯誤率 {ErrorRate:P1} ({errorCount}/{decisionCount})");

            if (!isPracticeMode && showHintOnError)
            {
                string suggest = lam.GetMostLikelyNext(previousState);
                if (!string.IsNullOrEmpty(suggest))
                    Debug.Log($"<color=yellow>[AI 導引]</color> 建議的正確步驟：{StepCatalog.DisplayName(suggest)}");
            }
        }

        lam.EnterState(label);
        previousState = label;
        return true;
    }

    /// <summary>玩家按下提示時由 VRDialogueController 呼叫。</summary>
    public void OnHintUsed(string section)
    {
        if (!sessionActive || string.IsNullOrEmpty(section)) return;

        var lam = LearningAnalyticsManager.Instance;
        if (lam == null) return;

        int n = lam.RecordHint(section);   // 直接記進該步驟的統計，會影響熟悉度
        totalHints++;

        Debug.Log($"<color=yellow>[AI 提示]</color> {StepCatalog.DisplayName(section)} | " +
                  $"此步驟第 {n} 次 | 本次共 {totalHints} 次");
    }

    /// <summary>
    /// 提示關閉時呼叫，把閱讀提示的時間從停留時間扣掉。
    /// 熟悉度已經由 hintCount 明確扣分，時間不重複計算。
    /// </summary>
    public void OnHintClosed(float secondsOpen)
    {
        if (!sessionActive) return;
        if (LearningAnalyticsManager.Instance != null)
            LearningAnalyticsManager.Instance.AddPausedTime(secondsOpen);
    }

    // ==================== 報告 ====================

    /// <summary>由腳本的 [REPORT] 標籤觸發。</summary>
    public void GenerateReport()
    {
        var lam = LearningAnalyticsManager.Instance;
        if (lam == null) return;

        lam.FlushCurrentState();

        var rows = BuildRows(lam.GenerateHeatmapData());
        float duration = Time.time - sessionStartTime;

        LogReport(rows, duration);
        if (writeJsonReport) WriteJson(rows, duration);

        ShowReport(rows, duration);
    }

    private void ShowReport(List<ReportRow> rows, float duration)
    {
        if (dialogueController != null) dialogueController.SuspendForReport();
        if (reportView != null)
            reportView.Show(rows, isPracticeMode, duration, decisionCount, errorCount, ErrorRate, this);
    }

    /// <summary>復習結束或直接重看報告時呼叫。</summary>
    public void ReopenReport()
    {
        var lam = LearningAnalyticsManager.Instance;
        if (lam == null) return;
        var rows = BuildRows(lam.GenerateHeatmapData());
        ShowReport(rows, Time.time - sessionStartTime);
    }

    private List<ReportRow> BuildRows(List<StepStat> stats)
    {
        // 先依「代表標籤」把跨天重複的知識點合併
        // （例如 ::Event2 / ::Event5 / ::Event8 三天的乳液題合成一列）
        var merged = new Dictionary<string, StepStat>();
        var order = new List<string>();

        foreach (var s in stats)
        {
            if (!StepCatalog.IsAssessed(s.stepName)) continue;   // 過場敘事不列

            string key = StepCatalog.GroupKey(s.stepName);
            if (!merged.TryGetValue(key, out var m))
            {
                m = new StepStat
                {
                    stepName = key,
                    baselineSeconds = s.baselineSeconds,
                    hintPenalty = s.hintPenalty
                };
                merged[key] = m;
                order.Add(key);
            }

            m.correctCount += s.correctCount;
            m.wrongCount += s.wrongCount;
            m.hintCount += s.hintCount;
            m.intervals.AddRange(s.intervals);
        }

        var rows = new List<ReportRow>();
        foreach (string key in order)
        {
            var m = merged[key];
            rows.Add(new ReportRow
            {
                label = key,
                displayName = StepCatalog.DisplayName(key),
                wrongCount = m.wrongCount,
                correctCount = m.correctCount,
                familiarity = m.FamiliarityIndex,
                totalSeconds = m.TotalSeconds,
                reviewable = StepCatalog.IsReviewable(key),
                reviewPassed = WasReviewPassed(key),
                hintCount = m.hintCount
            });
        }

        rows.Sort((a, b) =>
        {
            int c = b.wrongCount.CompareTo(a.wrongCount);
            return c != 0 ? c : a.familiarity.CompareTo(b.familiarity);
        });
        return rows;
    }

    private void LogReport(List<ReportRow> rows, float duration)
    {
        var sb = new StringBuilder();
        sb.AppendLine("========== 學習診斷報告 ==========");
        sb.AppendLine($"模式：{(isPracticeMode ? "測驗" : "學習")}");
        sb.AppendLine($"總時長：{duration / 60f:F1} 分鐘");
        sb.AppendLine($"決策次數：{decisionCount} | 錯誤次數：{errorCount} | 錯誤率：{ErrorRate:P1} | 使用提示：{totalHints} 次");
        sb.AppendLine("---------- 教學瓶頸（依錯誤次數排序）----------");
        foreach (var r in rows)
            sb.AppendLine($"{r.displayName} | 錯 {r.wrongCount} 對 {r.correctCount} | " +
                          $"熟悉度 {r.familiarity:F2} | 停留 {r.totalSeconds:F1}s | 提示 {r.hintCount} 次");
        if (errorTrail.Count > 0)
        {
            sb.AppendLine("---------- 錯誤路徑 ----------");
            foreach (var e in errorTrail) sb.AppendLine(e);
        }
        sb.AppendLine("==================================");
        Debug.Log(sb.ToString());
    }

    private void WriteJson(List<ReportRow> rows, float duration)
    {
        var payload = new ReportPayload
        {
            mode = isPracticeMode ? "practice" : "learning",
            timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            durationSeconds = duration,
            decisionCount = decisionCount,
            errorCount = errorCount,
            errorRate = ErrorRate,
            hintCount = totalHints,
            errorTrail = new List<string>(errorTrail),
            steps = rows
        };
        string path = Path.Combine(Application.persistentDataPath,
            $"report_{payload.mode}_{System.DateTime.Now:yyyyMMdd_HHmmss}.json");
        try
        {
            File.WriteAllText(path, JsonUtility.ToJson(payload, true));
            Debug.Log($"<color=cyan>[AI]</color> 報告已寫出：{path}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"<color=orange>[AI]</color> 報告寫出失敗：{e.Message}");
        }
    }

    // ==================== 復習 ====================

    /// <summary>由報告面板的「復習」按鈕呼叫。</summary>
    public void StartReview(string label)
    {
        if (!StepCatalog.IsReviewable(label))
        {
            Debug.LogWarning($"<color=orange>[AI]</color> {label} 依賴不可逆場景狀態，不開放單步復習");
            return;
        }

        var plan = StepCatalog.GetReviewPlan(label);

        isReviewing = true;
        reviewSection = label;
        reviewScope.Clear();
        foreach (string s in plan.scope) reviewScope.Add(s);
        previousState = "";

        PrepareReviewScene(plan);

        if (reportView != null) reportView.Hide();
        if (dialogueController != null) dialogueController.StartReview(plan.start);

        Debug.Log($"<color=cyan>[AI 復習]</color> 開始復習：{StepCatalog.DisplayName(label)}（從 {plan.start} 開始）");
    }

    /// <summary>復習結束，回到報告。由 OnEnterState、[RETURN]、[END] 或手動按鈕觸發。</summary>
    public void EndReview(bool passed)
    {
        if (!isReviewing) return;
        isReviewing = false;

        if (passed && !string.IsNullOrEmpty(reviewSection))
            reviewResults[reviewSection] = true;

        // 支線事件的復習會開警示燈。正常流程靠 [ALARM_OFF] 關，但復習在 [RETURN] 就結束了，要自己關
        var em = dialogueController != null ? dialogueController.eventManager : null;
        if (em != null) em.SetAlarm(false);

        Debug.Log($"<color=cyan>[AI 復習]</color> 結束：{StepCatalog.DisplayName(reviewSection)}");
        reviewSection = "";
        reviewScope.Clear();
        ReopenReport();
    }

    /// <summary>
    /// 復習開始前把場景準備好：儀器數值退回預設（不然上一輪設好的值會讓檢查直接通過），
    /// 支線事件則打開警示燈，讓復習的情境跟實際事件一樣。
    /// </summary>
    private void PrepareReviewScene(StepCatalog.ReviewPlan plan)
    {
        var em = dialogueController != null ? dialogueController.eventManager : null;
        if (em == null)
        {
            if (plan.reset != StepCatalog.ReviewReset.None || plan.alarm)
                Debug.LogWarning("<color=orange>[AI 復習]</color> 找不到 EventManager，儀器數值沒有重設，檢查可能會直接通過");
            return;
        }

        if (plan.reset == StepCatalog.ReviewReset.Cooling) em.ResetCoolingSettings();
        else if (plan.reset == StepCatalog.ReviewReset.Rewarming) em.ResetRewarmingSettings();

        if (plan.alarm) em.SetAlarm(true);
    }

    /// <summary>復習中途卡住時的逃生門，可接在任何按鈕上。</summary>
    public void AbortReview()
    {
        if (!isReviewing) return;
        EndReview(false);
    }

    // ==================== 資料結構 ====================

    [System.Serializable]
    public class ReportRow
    {
        public string label;
        public string displayName;
        public int wrongCount;
        public int correctCount;
        public float familiarity;
        public float totalSeconds;
        public bool reviewable;
        public bool reviewPassed;
        public int hintCount;

        /// <summary>熱圖強度 0（無問題）～1（嚴重瓶頸）。</summary>
        public float HeatLevel
        {
            get
            {
                int total = wrongCount + correctCount;
                float errRate = total == 0 ? 0f : (float)wrongCount / total;
                return Mathf.Clamp01(errRate * 0.7f + (1f - familiarity) * 0.3f);
            }
        }
    }

    [System.Serializable]
    private class ReportPayload
    {
        public string mode;
        public string timestamp;
        public float durationSeconds;
        public int decisionCount;
        public int errorCount;
        public float errorRate;
        public int hintCount;
        public List<string> errorTrail;
        public List<ReportRow> steps;
    }
}
