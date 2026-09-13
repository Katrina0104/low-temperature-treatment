using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class StepStat
{
    public string stepName;
    public int correctCount;
    public int wrongCount;
    public List<float> intervals = new List<float>(); // 秒（已扣除看提示的時間）
    public int hintCount;                             // 此步驟按提示的次數
    public float FamiliarityIndex => ComputeFamiliarity();

    /// <summary>此步驟累計的總停留秒數。</summary>
    public float TotalSeconds
    {
        get { float s = 0f; foreach (var t in intervals) s += t; return s; }
    }

    private float ComputeFamiliarity()
    {
        if (intervals.Count == 0) return 0f;
        float mean = 0f;
        foreach (var t in intervals) mean += t;
        mean /= intervals.Count;

        // 單邊衰減：比 baseline 快 → 1.0（操作熟練不該被懲罰）；
        // 比 baseline 慢 → 隨倍數衰減，用來偵測認知遲滯。
        float timeScore = Mathf.Clamp01(baselineSeconds / Mathf.Max(mean, baselineSeconds));

        // 提示是明確的評量項目：需要看提示代表對該步驟不熟。
        // 看提示的時間本身不計入 intervals，所以這裡不會重複扣兩次。
        return Mathf.Clamp01(timeScore - hintCount * hintPenalty);
    }

    public float baselineSeconds = 5f; // 之後用多次測試跑出的平均值填入
    public float hintPenalty = 0.15f;  // 每按一次提示扣掉的熟悉度
}

public class LearningAnalyticsManager : MonoBehaviour
{
    public static LearningAnalyticsManager Instance;

    // key: 目前狀態, value: (下一狀態, 轉移機率)
    private Dictionary<string, Dictionary<string, float>> transitionMatrix
        = new Dictionary<string, Dictionary<string, float>>();

    private Dictionary<string, StepStat> stats = new Dictionary<string, StepStat>();

    // 矩陣只用四個機率階：1.0 唯一正確／0.9 正確選項／0.5 隨機事件分支／0.1 錯誤。
    // 門檻必須落在 0.1 與 0.5 之間，否則 prob < errorThreshold 對 0.1 不成立，
    // 所有錯誤路徑都會被誤判為正確。
    public float errorThreshold = 0.2f;

    private string currentState;
    private float stateEnterTime;
    private float pausedInCurrentState;   // 目前步驟中，花在看提示的秒數

    void Awake()
    {
        Instance = this;
        BuildTransitionMatrix();   // 建在 Awake，避免與 AIController 的初始化順序衝突
    }

    private void BuildTransitionMatrix()
    {
        // ===== START → 病患核對 =====
        RegisterTransition("::START", "::Check_Patient", 1.0f);
        RegisterTransition("::Check_Patient", "::Patient_START", 0.9f);
        RegisterTransition("::Check_Patient", "::Patient_WRONG", 0.1f);
        RegisterTransition("::Patient_WRONG", "::Check_Patient", 0.1f); // 迴圈重試
        RegisterTransition("::Patient_START", "::BREATH_CHECK", 1.0f);

        // ===== 生命徵象確認 =====
        RegisterTransition("::BREATH_CHECK", "::GCS_CHECK", 0.9f);
        RegisterTransition("::BREATH_CHECK", "::BREATH_WRONG", 0.1f);
        RegisterTransition("::BREATH_WRONG", "::BREATH_CHECK", 0.1f);
        RegisterTransition("::GCS_CHECK", "::TEMP_CHOICE", 1.0f);
        RegisterTransition("::TEMP_CHOICE", "::Temperature_control_transfer_pad", 1.0f);

        // ===== 溫控傳遞墊穿戴 =====
        RegisterTransition("::Temperature_control_transfer_pad", "::Check_Patch", 1.0f);
        RegisterTransition("::Check_Patch", "::Water_Supply_Pipe", 0.9f);   // 貼片已穿戴
        RegisterTransition("::Check_Patch", "::Check_Patch", 0.1f);         // 尚未穿戴，原地等待

        // ===== 輸水管連接 =====
        RegisterTransition("::Water_Supply_Pipe", "::Check_Button", 1.0f);
        RegisterTransition("::Check_Button", "::Treatment", 0.9f);
        RegisterTransition("::Check_Button", "::Check_Button", 0.1f);

        // ===== 療程設定（降溫33度） =====
        RegisterTransition("::Treatment", "::Check_Setup", 1.0f);
        RegisterTransition("::Check_Setup", "::Next_Step", 0.9f);
        RegisterTransition("::Check_Setup", "::Check_Setup", 0.1f);

        // ===== 療程注意事項（純線性） =====
        RegisterTransition("::Next_Step", "::Note5", 1.0f);
        RegisterTransition("::Note5", "::Note6", 1.0f);
        RegisterTransition("::Note6", "::Note7", 1.0f);
        RegisterTransition("::Note7", "::Note8", 1.0f);
        RegisterTransition("::Note8", "::Note9", 1.0f);
        RegisterTransition("::Note9", "::Note10", 1.0f);
        RegisterTransition("::Note10", "::Day_1", 1.0f);

        // ===== 測驗模式：療程注意事項回憶題（Practice.txt.md 專用分支）=====
        RegisterTransition("::Note5", "::Note5_Yes", 0.9f);   RegisterTransition("::Note5", "::Note5_No", 0.1f);
        RegisterTransition("::Note5_No", "::Note5", 0.1f);    RegisterTransition("::Note5_Yes", "::Note6", 0.9f);
        RegisterTransition("::Note6", "::Note6_Yes", 0.9f);   RegisterTransition("::Note6", "::Note6_No", 0.1f);
        RegisterTransition("::Note6_No", "::Note6", 0.1f);    RegisterTransition("::Note6_Yes", "::Note7", 0.9f);
        RegisterTransition("::Note7", "::Note7_No", 0.9f);    RegisterTransition("::Note7", "::Note7_Yes", 0.1f);
        RegisterTransition("::Note7_Yes", "::Note7", 0.1f);   RegisterTransition("::Note7_No", "::Note8", 0.9f);
        RegisterTransition("::Note8", "::Note8_No", 0.9f);    RegisterTransition("::Note8", "::Note8_Yes", 0.1f);
        RegisterTransition("::Note8_Yes", "::Note8", 0.1f);   RegisterTransition("::Note8_No", "::Note9", 0.9f);
        RegisterTransition("::Note9", "::Note9_Yes", 0.9f);   RegisterTransition("::Note9", "::Note9_No", 0.1f);
        RegisterTransition("::Note9_No", "::Note9", 0.1f);    RegisterTransition("::Note9_Yes", "::Note10", 0.9f);
        RegisterTransition("::Note10", "::Note10_Yes", 0.9f); RegisterTransition("::Note10", "::Note10_No", 0.1f);
        RegisterTransition("::Note10_No", "::Note10", 0.1f);  RegisterTransition("::Note10_Yes", "::Day_1", 0.9f);

        // ===== Day 1 =====
        RegisterTransition("::Day_1", "::Event1", 1.0f);
        RegisterTransition("::Event1", "::Event1_No", 0.9f);   // 不需重接輸水管才是正解
        RegisterTransition("::Event1", "::Event1_Yes", 0.1f);
        RegisterTransition("::Event1_Yes", "::Event1", 0.1f);
        RegisterTransition("::Event1_No", "::Event2", 0.9f);

        RegisterTransition("::Event2", "::Event2_Yes", 0.9f);  // 需塗抹乳液才是正解
        RegisterTransition("::Event2", "::Event2_No", 0.1f);
        RegisterTransition("::Event2_No", "::Event2", 0.1f);
        RegisterTransition("::Event2_Yes", "::Event3", 0.9f);

        // Event3：三個 ROLL_* 隨機事件，走完後接 Day2
        RegisterTransition("::Event3", "::EVENT_EQUIPMENT", 0.5f);  // ROLL_EQUIP 觸發時
        RegisterTransition("::Event3", "::EVENT_SHIVERING", 0.5f);  // ROLL_SHIVER 觸發時
        RegisterTransition("::Event3", "::EVENT_BP_UNSTABLE", 0.5f);// ROLL_BP 觸發時
        RegisterTransition("::Event3", "::Day2", 0.9f);             // 全部通過後接下一天

        // ===== Day 2 =====
        RegisterTransition("::Day2", "::Event4", 1.0f);
        RegisterTransition("::Event4", "::Event4_Yes", 0.9f);  // 鼓膜溫度正常
        RegisterTransition("::Event4", "::Event4_No", 0.1f);
        RegisterTransition("::Event4_No", "::Event4", 0.1f);
        RegisterTransition("::Event4_Yes", "::Event5", 0.9f);

        RegisterTransition("::Event5", "::Event5_Yes", 0.9f);
        RegisterTransition("::Event5", "::Event5_No", 0.1f);
        RegisterTransition("::Event5_No", "::Event5", 0.1f);
        RegisterTransition("::Event5_Yes", "::Event6", 0.9f);

        RegisterTransition("::Event6", "::EVENT_EQUIPMENT", 0.5f);
        RegisterTransition("::Event6", "::EVENT_SHIVERING", 0.5f);
        RegisterTransition("::Event6", "::EVENT_BP_UNSTABLE", 0.5f);
        RegisterTransition("::Event6", "::Day3", 0.9f);

        // ===== Day 3 =====
        RegisterTransition("::Day3", "::Event7", 1.0f);
        RegisterTransition("::Event7", "::Event7_Yes", 0.9f);
        RegisterTransition("::Event7", "::Event7_No", 0.1f);
        RegisterTransition("::Event7_No", "::Event7", 0.1f);
        RegisterTransition("::Event7_Yes", "::Event8", 0.9f);

        RegisterTransition("::Event8", "::Event8_Yes", 0.9f);
        RegisterTransition("::Event8", "::Event8_No", 0.1f);
        RegisterTransition("::Event8_No", "::Event8", 0.1f);
        RegisterTransition("::Event8_Yes", "::Event9", 0.9f);

        RegisterTransition("::Event9", "::EVENT_EQUIPMENT", 0.5f);
        RegisterTransition("::Event9", "::EVENT_SHIVERING", 0.5f);
        RegisterTransition("::Event9", "::EVENT_BP_UNSTABLE", 0.5f);
        RegisterTransition("::Event9", "::End", 0.9f);

        // ===== 結束流程（拆管、收尾） =====
        RegisterTransition("::End", "::Check_Button_stop", 1.0f);
        RegisterTransition("::Check_Button_stop", "::Check_Button_empty", 0.9f); // stop 已按下
        RegisterTransition("::Check_Button_stop", "::Check_Button_stop", 0.1f);
        RegisterTransition("::Check_Button_empty", "::Continue", 0.9f);         // 排空鍵已按下
        RegisterTransition("::Check_Button_empty", "::Check_Button_empty", 0.1f);
        RegisterTransition("::Continue", "::Check_pipeline_end", 1.0f);
        RegisterTransition("::Check_pipeline_end", "::Remove_patch", 0.9f);     // 機台端管路已切斷
        RegisterTransition("::Check_pipeline_end", "::Check_pipeline_end", 0.1f);
        RegisterTransition("::Remove_patch", "::Remove_YES", 0.9f);            // 確認移除
        RegisterTransition("::Remove_patch", "::Remove_No", 0.1f);
        RegisterTransition("::Remove_No", "::Remove_patch", 0.1f);
        RegisterTransition("::Remove_YES", "[END]", 1.0f);                     // 終止狀態

        // ===== 支線事件：設備缺水/脫落 =====
        RegisterTransition("::EVENT_EQUIPMENT", "::Check_Empty", 1.0f);
        RegisterTransition("::Check_Empty", "::Check_EQUIPMENT", 0.9f);        // Empty鍵已按
        RegisterTransition("::Check_Empty", "::Check_Empty", 0.1f);
        RegisterTransition("::Check_EQUIPMENT", "::EQUIPMENT_Connected", 0.9f);// 已斷開機台端
        RegisterTransition("::Check_EQUIPMENT", "::Check_EQUIPMENT", 0.1f);
        RegisterTransition("::EQUIPMENT_Connected", "::Check_EQUIPMENT_back", 1.0f);
        RegisterTransition("::Check_EQUIPMENT_back", "[RETURN]", 0.9f);        // 已接回，回到主流程
        RegisterTransition("::Check_EQUIPMENT_back", "::Check_EQUIPMENT_back", 0.1f);

        // ===== 支線事件：顫抖 =====
        RegisterTransition("::EVENT_SHIVERING", "::Check_MgSO4", 1.0f);
        RegisterTransition("::Check_MgSO4", "::SHIVERING_Yes", 0.9f);   // 給予硫酸鎂
        RegisterTransition("::Check_MgSO4", "::SHIVERING_No", 0.9f);    // 兩個都是合理臨床選項
        RegisterTransition("::SHIVERING_Yes", "[RETURN]", 0.9f);
        RegisterTransition("::SHIVERING_No", "::SHIVERING1_Yes", 0.9f); // 改給止痛藥
        RegisterTransition("::SHIVERING_No", "::SHIVERING1_No", 0.1f);
        RegisterTransition("::SHIVERING1_Yes", "[RETURN]", 0.9f);
        RegisterTransition("::SHIVERING1_No", "::Check_MgSO4", 0.1f);

        // ===== 支線事件：血壓不穩（復溫調整） =====
        RegisterTransition("::EVENT_BP_UNSTABLE", "::Temperature", 1.0f);
        RegisterTransition("::Temperature", "::Speed", 0.9f);       // 溫度設37度正確
        RegisterTransition("::Temperature", "::Temperature", 0.1f);
        RegisterTransition("::Speed", "::Speed_Check", 1.0f);
        RegisterTransition("::Speed_Check", "[RETURN]", 0.9f);      // 速率設0.25°C/hr正確
        RegisterTransition("::Speed_Check", "::Speed_Check", 0.1f);
    }

    // 手動依照 Dialog.txt.md 的正確流程建表
    public void RegisterTransition(string from, string to, float probability)
    {
        if (!transitionMatrix.ContainsKey(from))
            transitionMatrix[from] = new Dictionary<string, float>();
        transitionMatrix[from][to] = probability;
    }

    public void EnterState(string stateName)
    {
        // 先結算上一個狀態的停留時間（扣掉看提示的那段）
        if (!string.IsNullOrEmpty(currentState))
        {
            float dt = Mathf.Max(0f, Time.time - stateEnterTime - pausedInCurrentState);
            GetOrCreateStat(currentState).intervals.Add(dt);
        }
        pausedInCurrentState = 0f;

        currentState = stateName;
        stateEnterTime = Time.time;

        if (!stats.ContainsKey(stateName)) stats[stateName] = new StepStat { stepName = stateName };
    }

    // 每次玩家做出動作(collider觸發/按鈕/CHOICE)呼叫這個
    public bool CheckTransition(string attemptedNext)
    {
        float prob = 0f;
        if (transitionMatrix.ContainsKey(currentState) &&
            transitionMatrix[currentState].ContainsKey(attemptedNext))
        {
            prob = transitionMatrix[currentState][attemptedNext];
        }

        var stat = GetOrCreateStat(currentState);
        if (prob < errorThreshold)
        {
            stat.wrongCount++;
            Debug.Log($"<color=red>[HMM 判定]</color> {currentState} -> {attemptedNext} 機率 {prob:F2}，判定為流程錯誤");
            return false;
        }
        else
        {
            stat.correctCount++;
            return true;
        }
    }

    private StepStat GetOrCreateStat(string name)
    {
        if (!stats.ContainsKey(name)) stats[name] = new StepStat { stepName = name };
        return stats[name];
    }

    /// <summary>重玩時清空統計，保留轉移矩陣。</summary>
    public void ResetSession()
    {
        stats.Clear();
        currentState = null;
        stateEnterTime = 0f;
        pausedInCurrentState = 0f;
    }

    /// <summary>結算目前狀態的停留時間（產報告前呼叫）。</summary>
    public void FlushCurrentState()
    {
        if (!string.IsNullOrEmpty(currentState))
        {
            float dt = Mathf.Max(0f, Time.time - stateEnterTime - pausedInCurrentState);
            GetOrCreateStat(currentState).intervals.Add(dt);
            stateEnterTime = Time.time;
            pausedInCurrentState = 0f;
        }
    }

    /// <summary>
    /// 把看提示的那段時間從目前步驟的停留時間裡扣掉。
    /// 熟悉度改由 hintCount 明確扣分，時間不重複計算。
    /// </summary>
    public void AddPausedTime(float seconds)
    {
        if (seconds > 0f) pausedInCurrentState += seconds;
    }

    /// <summary>記錄某步驟被按了一次提示，回傳該步驟累計的提示次數。</summary>
    public int RecordHint(string state)
    {
        if (string.IsNullOrEmpty(state)) return 0;
        var st = GetOrCreateStat(state);
        st.hintCount++;
        return st.hintCount;
    }

    /// <summary>只查詢轉移是否合法，不寫入統計（復習模式用，避免污染評鑑數據）。</summary>
    public bool PeekTransition(string from, string to)
    {
        float prob = 0f;
        if (transitionMatrix.ContainsKey(from) && transitionMatrix[from].ContainsKey(to))
            prob = transitionMatrix[from][to];
        return prob >= errorThreshold;
    }

    /// <summary>取出機率最高的下一步，用於即時導引。</summary>
    public string GetMostLikelyNext(string state)
    {
        if (string.IsNullOrEmpty(state) || !transitionMatrix.ContainsKey(state)) return "";
        string best = ""; float bestP = -1f;
        foreach (var kv in transitionMatrix[state])
            if (kv.Value > bestP) { bestP = kv.Value; best = kv.Key; }
        return best;
    }

    // 課程結束時輸出熱圖資料
    public List<StepStat> GenerateHeatmapData()
    {
        return new List<StepStat>(stats.Values);
    }
}