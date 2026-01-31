using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class VRDialogueController : MonoBehaviour
{
    [Header("UI 引用")]
    public GameObject dialoguePlane;
    public TextMeshProUGUI dialogueContent;

    [Header("互動按鈕 (初始設為非啟動)")]
    public GameObject yesButton;
    public GameObject noButton;

    [Header("NPC 動畫")]
    public Animator npcAnimator;

    [Header("配置")]
    public TextAsset dialogueFile;

    [Header("VR 控制設定")]
    public InputActionProperty leftTriggerAction;
    public InputActionProperty rightTriggerAction;

    private List<string> lines = new List<string>();
    private int currentIndex = 0;

    private void Awake()
    {
        LoadDialogue();
    }

    private void Start()
    {
        if (dialoguePlane != null) dialoguePlane.SetActive(true);

        // 初始確保按鈕是關閉的
        if (yesButton != null) yesButton.SetActive(false);
        if (noButton != null) noButton.SetActive(false);

        ShowLine();
    }

    private void OnEnable() => SetupVRInput();

    private void OnDisable()
    {
        if (leftTriggerAction.action != null) leftTriggerAction.action.started -= OnTriggerPressed;
        if (rightTriggerAction.action != null) rightTriggerAction.action.started -= OnTriggerPressed;
    }

    void SetupVRInput()
    {
        if (leftTriggerAction.action != null)
        {
            leftTriggerAction.action.started += OnTriggerPressed;
            leftTriggerAction.action.Enable();
        }
        if (rightTriggerAction.action != null)
        {
            rightTriggerAction.action.started += OnTriggerPressed;
            rightTriggerAction.action.Enable();
        }
    }

    private void OnTriggerPressed(InputAction.CallbackContext context)
    {
        // 只有在按鈕「沒出現」的時候，Trigger 才能換行
        // 這樣可以強迫玩家在最後做出選擇
        if (dialoguePlane != null && dialoguePlane.activeSelf)
        {
            bool isChoiceMode = (yesButton != null && yesButton.activeSelf);
            if (!isChoiceMode)
            {
                OnNextStep();
            }
        }
    }

    void LoadDialogue()
    {
        if (dialogueFile != null)
        {
            lines.Clear();
            string[] splitLines = dialogueFile.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
            lines.AddRange(splitLines);
        }
    }

    public void OnNextStep()
    {
        currentIndex++;
        if (currentIndex < lines.Count)
        {
            ShowLine();
        }
        else
        {
            // 對話播完，顯示 Yes/No 按鈕
            ShowChoiceButtons();
        }
    }

    void ShowLine()
    {
        if (lines.Count > 0 && currentIndex < lines.Count)
        {
            string rawLine = lines[currentIndex].Trim();

            if (rawLine.StartsWith("[") && rawLine.Contains("]"))
            {
                int tagEnd = rawLine.IndexOf(']');
                string actionTag = rawLine.Substring(1, tagEnd - 1);
                string textText = rawLine.Substring(tagEnd + 1);

                if (npcAnimator != null) npcAnimator.SetTrigger(actionTag);
                dialogueContent.text = textText;
            }
            else
            {
                dialogueContent.text = rawLine;
            }
        }
    }

    void ShowChoiceButtons()
    {
        // 最後一行顯示完畢後的提示
        dialogueContent.text = "請選擇你的決定：";
        if (yesButton != null) yesButton.SetActive(true);
        if (noButton != null) noButton.SetActive(true);
    }

    // 由 Yes Button 的 On Click 事件調用
    public void OnYesClicked()
    {
        Debug.Log("Yes clicked!");
        // 這裡可以觸發 NPC 另一個動畫，例如開心
        if (npcAnimator != null) npcAnimator.SetTrigger("Happy");
        EndDialogue();
    }

    // 由 No Button 的 On Click 事件調用
    public void OnNoClicked()
    {
        Debug.Log("No clicked!");
        // 這裡可以觸發 NPC 生氣或難過動畫
        if (npcAnimator != null) npcAnimator.SetTrigger("Sad");
        EndDialogue();
    }

    void EndDialogue()
    {
        if (yesButton != null) yesButton.SetActive(false);
        if (noButton != null) noButton.SetActive(false);
        if (dialoguePlane != null) dialoguePlane.SetActive(false);
        currentIndex = 0;
    }
}