using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EventManager : MonoBehaviour
{
    [Header("引用設定")]
    public VRDialogueController dialogueController;
    public TextMeshProUGUI dayText;
    public Image dayTransitionPanel;
    public GameObject redAlarmLight;

    [Header("遊戲數值")]
    public int currentDay = 1;
    public bool isRewarming = false; // 復溫階段標記

    [Header("漸變設定")]
    public float fadeDuration = 1.5f; // 變暗與變亮各需幾秒
    public float stayDuration = 1.0f; // 在最暗的時候停留多久

    void Start()
    {
        UpdateDayUI();
        SetAlarm(false);
    }

    // 更新天數 UI
    public void UpdateDayUI()
    {
        if (dayText != null) dayText.text = "Day" + currentDay;
    }

    // --- 設備問題 ---
    public void RollEquipmentEvent()
    {
        float val = Random.value;
        if (val < 0.5f)
        {
            Debug.Log($"<color=green>[EVENT 成功]</color> 設備事件觸發 (機率:{val:F2} < 0.5)");
            SetAlarm(true);
            dialogueController.JumpToSection("::EVENT_EQUIPMENT");
        }
        else
        {
            Debug.Log($"<color=gray>[EVENT 跳過]</color> 設備事件未觸發 (機率:{val:F2} >= 0.5)");
            dialogueController.OnNextStep();
        }
    }

    // --- 顫抖問題 ---
    public void RollShiveringEvent()
    {
        float val = Random.value;
        if (val < 0.5f)
        {
            Debug.Log($"<color=green>[EVENT 成功]</color> 顫抖事件觸發 (機率:{val:F2} < 0.5)");
            SetAlarm(true);
            dialogueController.JumpToSection("::EVENT_SHIVERING");
        }
        else
        {
            Debug.Log($"<color=gray>[EVENT 跳過]</color> 顫抖事件未觸發 (機率:{val:F2} >= 0.5)");
            dialogueController.OnNextStep();
        }
    }

    // --- 血壓問題 ---
    public void RollBPEvent()
    {
        float chance = isRewarming ? 0.5f : 0.33f;
        float val = Random.value;
        if (val < chance)
        {
            Debug.Log($"<color=green>[EVENT 成功]</color> 血壓事件觸發 (機率:{val:F2} < {chance})");
            SetAlarm(true);
            dialogueController.JumpToSection("::EVENT_BP_UNSTABLE");
        }
        else
        {
            Debug.Log($"<color=gray>[EVENT 跳過]</color> 血壓事件未觸發 (機率:{val:F2} >= {chance})");
            dialogueController.OnNextStep();
        }
    }

    public void SetAlarm(bool active)
    {
        if (redAlarmLight != null) redAlarmLight.SetActive(active);
    }

    // 當劇本標籤 [INC_DAY] 觸發時呼叫此方法[cite: 1, 2]
    public void NextDay()
    {
        StartCoroutine(DayTransitionRoutine());
    }

    private IEnumerator DayTransitionRoutine()
    {
        Debug.Log("<color=magenta>[FADE]</color> 開始天數切換過場...");
        yield return StartCoroutine(Fade(0f, 200f / 255f, fadeDuration));

        currentDay++;
        UpdateDayUI();
        Debug.Log($"<color=magenta>[SYSTEM]</color> 天數已更新為: {currentDay}");

        yield return new WaitForSeconds(stayDuration);
        yield return StartCoroutine(Fade(200f / 255f, 0f, fadeDuration));
        Debug.Log("<color=magenta>[FADE]</color> 過場結束");
    }

    private IEnumerator Fade(float startAlpha, float targetAlpha, float duration)
    {
        float elapsed = 0f;
        Color c = dayTransitionPanel.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            dayTransitionPanel.color = c;
            yield return null;
        }

        // 確保最終數值精準[cite: 1]
        c.a = targetAlpha;
        dayTransitionPanel.color = c;
    }
}

