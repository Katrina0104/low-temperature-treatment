using UnityEngine;
using TMPro;

public class TemperatureSimulation : MonoBehaviour
{
    [Header("UI 引用")]
    public TextMeshProUGUI currentTempDisplay;
    public TextMeshProUGUI statusDisplay;
    public TextMeshProUGUI coolingSettingText; // 拖入顯示 33.0 的那個文字
    public TextMeshProUGUI rewarmingSettingText; // 拖入顯示 36.5 的那個文字
    public RectTransform graphArea;
    public RectTransform timeIndicator;

    [Header("圖表引用")]
    public RealTimeGraph patientTempGraph;

    //[Header("數值來源")]
    //public TimeValueController coolingTimer;
    //public TimeValueController rewarmingTimer;

    [Header("時間與流速")]
    public float timeMultiplier = 10f;
    public float totalGraphTimeMinutes = 720f;

    [Header("速率來源 (取代原本的 Timer)")]
    public RateValueController coolingRateCtrl;   // 拖入降溫速率控制器
    public RateValueController rewarmingRateCtrl; // 拖入升溫速率控制器

    [Header("顯示換算後的預計時長")]
    public TextMeshProUGUI coolingDurationDisplay;   // 顯示「預計降溫時間」
    public TextMeshProUGUI coolingDurationDisplay1;   // 顯示「預計降溫時間」
    public TextMeshProUGUI rewarmingDurationDisplay; // 顯示「預計升溫時間」
    public TextMeshProUGUI rewarmingDurationDisplay1; // 顯示「預計升溫時間」

    [Header("溫度設定")]
    public float initialTemp = 37.0f;
    public float targetCoolTemp = 33.0f;
    public float targetRewarmTemp = 36.5f;

    private float currentTemp;
    private float elapsedTime = 0f;
    private float sessionTotalSimulatedSeconds = 0f;
    private float lastRecordTime = -1f;
    private bool isRunning = false;
    private bool isPaused = false;

    enum State { Idle, Cooling, Rewarming, Finished }
    private State currentState = State.Idle;

    void Start()
    {
        currentTemp = initialTemp;
        UpdateUI();
    }

    void Update()
    {
        UpdateEstimatedTimeUI(); // 讓玩家調整速率時，時間跟著跳動

        if (!isRunning || isPaused) return;

        // 2. 計算此幀經過的模擬時間
        float speedUpDeltaTime = Time.deltaTime * timeMultiplier;
        sessionTotalSimulatedSeconds += speedUpDeltaTime;
        float currentSimMinutes = sessionTotalSimulatedSeconds / 60f;

        UpdateTimeIndicator(currentSimMinutes);

        if (currentSimMinutes >= lastRecordTime + 1f)
        {
            patientTempGraph.AddDataPoint(currentSimMinutes, currentTemp);
            lastRecordTime = currentSimMinutes;
        }

        // 5. 執行溫度模擬
        if (currentState == State.Cooling)
        {
            SimulateTemperature(initialTemp, targetCoolTemp, GetCalculatedCoolingMinutes(), State.Rewarming, speedUpDeltaTime);
        }
        else if (currentState == State.Rewarming)
        {
            SimulateTemperature(targetCoolTemp, targetRewarmTemp, GetCalculatedRewarmingMinutes(), State.Finished, speedUpDeltaTime);
        }
    }

    void UpdateTimeIndicator(float currentSimMinutes)
    {
        if (timeIndicator == null || graphArea == null) return;

        // 1. 計算進度 (0.0 ~ 1.0)
        float totalMins = totalGraphTimeMinutes > 0 ? totalGraphTimeMinutes : 720f;
        float progress = Mathf.Clamp01(currentSimMinutes / totalMins);

        // 2. 獲取圖表框的總寬度
        float graphWidth = graphArea.rect.width;

        // 3. 直接計算 X 偏移量
        // 因為 scan_line 的錨點已經設在左邊，所以 0 就是起點
        float targetX = - progress * graphWidth;// 加負號是為了讓線從左邊開始向右移動

        // 4. 套用座標
        timeIndicator.anchoredPosition = new Vector2(targetX, timeIndicator.anchoredPosition.y);

        // 除錯日誌：現在你應該會看到 X 從 0 增加到 100 (或你的寬度)
        Debug.Log($"掃描線進度: {progress * 100:F2}%, X座標: {targetX:F2}");
    }

    void UpdateEstimatedTimeUI()
    {
        float cMin = GetCalculatedCoolingMinutes();
        float rMin = GetCalculatedRewarmingMinutes();

        if (coolingDurationDisplay != null)
            coolingDurationDisplay.text = $"{(int)cMin / 60}h {(int)cMin % 60}m";

        if (rewarmingDurationDisplay != null)
            rewarmingDurationDisplay.text = $"{(int)rMin / 60}h {(int)rMin % 60}m";
    }



    void SimulateTemperature(float startTemp, float endTemp, float durationMinutes, State nextState, float deltaTime)
    {
        if (durationMinutes <= 0)
        {
            currentTemp = endTemp;
            TransitionTo(nextState);
            return;
        }

        float durationSeconds = durationMinutes * 60f;
        elapsedTime += deltaTime;

        currentTemp = Mathf.Lerp(startTemp, endTemp, elapsedTime / durationSeconds);

        if (elapsedTime / durationSeconds >= 1.0f)
        {
            currentTemp = endTemp;
            TransitionTo(nextState);
        }
        UpdateUI();
    }

    // --- 新增：自動計算時長的函式 ---
    public float GetCalculatedCoolingMinutes()
    {
        if (coolingRateCtrl == null) return 0;
        float tempDiff = Mathf.Abs(initialTemp - targetCoolTemp);
        float rate = coolingRateCtrl.currentRate;
        return (tempDiff / rate) * 60f;
    }

    public float GetCalculatedRewarmingMinutes()
    {
        if (rewarmingRateCtrl == null) return 0;
        float tempDiff = Mathf.Abs(targetRewarmTemp - targetCoolTemp);
        float rate = rewarmingRateCtrl.currentRate;
        return (tempDiff / rate) * 60f;
    }

    void TransitionTo(State newState)
    {
        elapsedTime = 0f;
        currentState = newState;
        if (newState == State.Finished) isRunning = false;
    }

    public void StartSimulation()
    {
        // 重置所有數據
        currentTemp = initialTemp;
        elapsedTime = 0f;
        sessionTotalSimulatedSeconds = 0f;
        lastRecordTime = -1f;
        if (patientTempGraph != null) patientTempGraph.ClearGraph();

        currentState = State.Cooling;
        isRunning = true;
        isPaused = false;
    }

    public void ToggleStop() { 
        isPaused = !isPaused; 
        Debug.Log(isPaused ? "Simulation Paused" : "Simulation Resumed");
    }

    void UpdateUI()
    {
        currentTempDisplay.text = $"{currentTemp:F1} °C";
        statusDisplay.text = isPaused ? "PAUSED" : (isRunning ? currentState.ToString() : "READY");

        // 更新設定數值文字
        if (coolingSettingText != null) coolingSettingText.text = $"{targetCoolTemp:F1}";
        if (rewarmingSettingText != null) rewarmingSettingText.text = $"{targetRewarmTemp:F1}";
    }
}