using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 進場模式選擇：學習模式跑 Dialog.txt.md，測驗模式跑 Practice.txt.md。
/// 兩者共用同一個場景，只切換 TextAsset。
/// </summary>
public class ModeSelector : MonoBehaviour
{
    [Header("UI")]
    public GameObject startView;
    public Button learningButton;
    public Button practiceButton;
    [Tooltip("療程中隨時回到 StartView 重選腳本")]
    public Button chooseStateButton;

    [Header("系統連結")]
    public VRDialogueController dialogueController;
    public AIController aiController;

    [Header("腳本")]
    public TextAsset learningScript;   // Dialog.txt.md
    public TextAsset practiceScript;   // Practice.txt.md

    void Start()
    {
        if (learningButton != null) learningButton.onClick.AddListener(OnLearningSelected);
        if (practiceButton != null) practiceButton.onClick.AddListener(OnPracticeSelected);
        if (chooseStateButton != null) chooseStateButton.onClick.AddListener(ReturnToStartView);

        if (startView != null) startView.SetActive(true);
        if (dialogueController != null && dialogueController.dialoguePlane != null)
            dialogueController.dialoguePlane.SetActive(false);
    }

    public void OnLearningSelected() => Begin(learningScript, false);
    public void OnPracticeSelected() => Begin(practiceScript, true);

    private void Begin(TextAsset script, bool practiceMode)
    {
        if (script == null)
        {
            Debug.LogError("[ModeSelector] 腳本未指派，請在 Inspector 拉入 TextAsset");
            return;
        }

        if (startView != null) startView.SetActive(false);
        if (aiController != null) aiController.BeginSession(practiceMode);
        if (dialogueController != null) dialogueController.StartDialogue(script, practiceMode);
    }

    /// <summary>
    /// 回到 StartView 重選腳本：清空對話文本、收起對話與報告 UI。
    ///
    /// 注意：場景中的物件狀態不會跟著還原（已穿戴的貼片、已連接的管路、
    /// ReplaceActivateChildren 的 activated 旗標都是單向的），
    /// 所以重選後再跑一次，那些物理操作步驟會直接通過。
    /// 要做正式的實驗量測，請重新載入場景而不是用這個按鈕。
    /// </summary>
    public void ReturnToStartView()
    {
        if (dialogueController != null) dialogueController.ResetToStartView();
        if (aiController != null && aiController.reportView != null) aiController.reportView.Hide();
        if (startView != null) startView.SetActive(true);

        Debug.Log("<color=cyan>[ModeSelector]</color> 已回到模式選單，對話文本已清空");
    }
}
