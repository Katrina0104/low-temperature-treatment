using System.Collections.Generic;

/// <summary>
/// 狀態標籤 → 中文顯示名 + 是否可單獨復習。
/// 不可復習的原因是該步驟依賴不可逆的場景狀態（貼片已穿戴、管路已連接、
/// ReplaceOnCollision 已 Destroy 原物件），跳回去會立刻通過而練不到東西。
/// </summary>
public static class StepCatalog
{
    public struct Info
    {
        public string displayName;
        public bool reviewable;
        /// <summary>
        /// 同一個知識點在不同天出現時，指向代表用的標籤，報告會合併成一列。
        /// 留空代表自成一組。
        /// </summary>
        public string group;

        public Info(string n, bool r, string g = "")
        {
            displayName = n; reviewable = r; group = g;
        }
    }

    private static readonly Dictionary<string, Info> table = new Dictionary<string, Info>
    {
        { "::Check_Patient",                    new Info("病患身份核對", true) },
        { "::BREATH_CHECK",                     new Info("生命徵象判讀", true) },
        { "::GCS_CHECK",                        new Info("意識與顱內壓評估", false) },
        { "::TEMP_CHOICE",                      new Info("核心體溫探頭置放", false) },
        { "::Temperature_control_transfer_pad", new Info("溫控傳遞墊選型", false) },
        { "::Check_Patch",                      new Info("溫控傳遞墊穿戴", false) },
        { "::Water_Supply_Pipe",                new Info("輸水管準備", false) },
        { "::Check_Button",                     new Info("輸水管連接", false) },
        { "::Treatment",                        new Info("療程設定", false) },
        { "::Check_Setup",                      new Info("降溫溫度設定", true) },
        { "::Note5",                            new Info("血糖管理", true) },
        { "::Note6",                            new Info("凝血功能與出血風險", true) },
        { "::Note7",                            new Info("免疫系統與感染風險", true) },
        { "::Note8",                            new Info("心律不整", true) },
        { "::Note9",                            new Info("意識狀態", true) },
        { "::Note10",                           new Info("電解質與液體平衡", true) },
        { "::Event1",                           new Info("輸水管重接判斷", true) },
        // 乳液與鼓膜溫度在三天裡重複出現，合併成一列統計（Event2 / Event4 為代表標籤）
        { "::Event2",                           new Info("乳液塗抹判斷", true) },
        { "::Event5",                           new Info("乳液塗抹判斷", true, "::Event2") },
        { "::Event8",                           new Info("乳液塗抹判斷", true, "::Event2") },
        { "::Event4",                           new Info("鼓膜溫度判讀", true) },
        { "::Event7",                           new Info("鼓膜溫度判讀", true, "::Event4") },
        { "::Check_Button_stop",                new Info("停止治療程序", false) },
        { "::Check_Button_empty",               new Info("傳遞墊排空", false) },
        { "::Check_pipeline_end",               new Info("機台端管路切斷", false) },
        { "::Remove_patch",                     new Info("貼片移除確認", true) },
        { "::Check_Empty",                      new Info("貼片清空（設備事件）", false) },
        { "::Check_EQUIPMENT",                  new Info("貼片斷開（設備事件）", false) },
        { "::Check_EQUIPMENT_back",             new Info("貼片接回（設備事件）", false) },
        { "::Check_MgSO4",                      new Info("顫抖處置選擇", true) },
        { "::Temperature",                      new Info("復溫溫度設定", true) },
        { "::Speed_Check",                      new Info("復溫速率設定", true) },
    };

    /// <summary>是否為需要列入評鑑的步驟（過場敘事不列）。</summary>
    public static bool IsAssessed(string label) => table.ContainsKey(label);

    public static string DisplayName(string label)
        => table.TryGetValue(label, out var i) ? i.displayName : label;

    public static bool IsReviewable(string label)
        => table.TryGetValue(label, out var i) && i.reviewable;

    /// <summary>
    /// 報告合併用的代表標籤。跨天重複的知識點會回傳同一個值，
    /// 其餘步驟回傳自己。復習按鈕也是跳到這個標籤。
    /// </summary>
    public static string GroupKey(string label)
    {
        if (table.TryGetValue(label, out var i) && !string.IsNullOrEmpty(i.group))
            return i.group;
        return label;
    }

    // ==================== 復習計畫 ====================

    public enum ReviewReset { None, Cooling, Rewarming }

    /// <summary>
    /// 一個步驟的復習怎麼跑：從哪個標籤開始、哪些標籤算在這一段裡、
    /// 開始前要不要把儀器數值退回預設、要不要亮警示燈。
    /// 流程一走出 scope（或遇到 [RETURN] / [END] / [REPORT]）就結束復習、回到報告。
    /// </summary>
    public struct ReviewPlan
    {
        public string start;
        public string[] scope;
        public ReviewReset reset;
        public bool alarm;

        public ReviewPlan(string start, string[] scope, ReviewReset reset = ReviewReset.None, bool alarm = false)
        {
            this.start = start; this.scope = scope; this.reset = reset; this.alarm = alarm;
        }
    }

    private static readonly string[] BpScope =
        { "::EVENT_BP_UNSTABLE", "::Temperature", "::Speed", "::Speed_Check" };

    private static readonly string[] ShiverScope =
        { "::EVENT_SHIVERING", "::Check_MgSO4", "::SHIVERING_Yes", "::SHIVERING_No", "::SHIVERING1_Yes", "::SHIVERING1_No" };

    /// <summary>不是單純「題目 + _Yes/_No」的步驟，明確寫出復習範圍。</summary>
    private static readonly Dictionary<string, ReviewPlan> reviewPlans = new Dictionary<string, ReviewPlan>
    {
        { "::Check_Patient", new ReviewPlan("::Check_Patient", new[] { "::Check_Patient", "::Patient_START", "::Patient_WRONG" }) },
        { "::BREATH_CHECK",  new ReviewPlan("::BREATH_CHECK",  new[] { "::BREATH_CHECK", "::BREATH_WRONG" }) },
        { "::Remove_patch",  new ReviewPlan("::Remove_patch",  new[] { "::Remove_patch", "::Remove_YES", "::Remove_No" }) },

        // 儀器設定：從指示那段開始，並先把數值退回預設，否則上一輪設好的值會讓檢查直接通過
        { "::Check_Setup",   new ReviewPlan("::Treatment", new[] { "::Treatment", "::Check_Setup" }, ReviewReset.Cooling) },

        // 支線事件：整段從警告開始重跑，跑到 [RETURN] 結束
        { "::Temperature",   new ReviewPlan("::EVENT_BP_UNSTABLE", BpScope, ReviewReset.Rewarming, alarm: true) },
        { "::Speed_Check",   new ReviewPlan("::EVENT_BP_UNSTABLE", BpScope, ReviewReset.Rewarming, alarm: true) },
        { "::Check_MgSO4",   new ReviewPlan("::EVENT_SHIVERING",   ShiverScope, ReviewReset.None, alarm: true) },
    };

    /// <summary>
    /// 取得復習計畫。沒有特別登記的一律當作選擇題：
    /// 從題目本身開始，範圍是題目與 _Yes / _No 兩個分支（Note5～10、Event1/2/4）。
    /// </summary>
    public static ReviewPlan GetReviewPlan(string label)
    {
        if (reviewPlans.TryGetValue(label, out var plan)) return plan;
        return new ReviewPlan(label, new[] { label, label + "_Yes", label + "_No" });
    }
}
