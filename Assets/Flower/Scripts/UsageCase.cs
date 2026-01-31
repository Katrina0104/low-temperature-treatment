using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class VRDialogueSystem : MonoBehaviour
{
    [Header("UI 引用")]
    public GameObject dialoguePlane;       // 原 panelObject
    public TextMeshProUGUI dialogueContent; // 原 contentText

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
        // 確保初始狀態
        if (dialoguePlane != null) dialoguePlane.SetActive(true);
        ShowLine();
    }

    private void OnEnable()
    {
        SetupVRInput();
    }

    private void OnDisable()
    {
        // 解除訂閱，避免切換場景時報錯
        if (leftTriggerAction.action != null)
            leftTriggerAction.action.started -= OnLeftTriggerStarted;

        if (rightTriggerAction.action != null)
            rightTriggerAction.action.started -= OnRightTriggerStarted;
    }

    // 使用你要求的 Setup 方式
    void SetupVRInput()
    {
        if (leftTriggerAction.action != null)
        {
            leftTriggerAction.action.started += OnLeftTriggerStarted;
            leftTriggerAction.action.Enable();
        }

        if (rightTriggerAction.action != null)
        {
            rightTriggerAction.action.started += OnRightTriggerStarted;
            rightTriggerAction.action.Enable();
        }
    }

    private void OnLeftTriggerStarted(InputAction.CallbackContext context)
    {
        HandleTriggerInput();
    }

    private void OnRightTriggerStarted(InputAction.CallbackContext context)
    {
        HandleTriggerInput();
    }

    void HandleTriggerInput()
    {
        // 只有在對話面板顯示時，按下 Trigger 才有反應
        if (dialoguePlane != null && dialoguePlane.activeSelf)
        {
            OnNextStep();
        }
    }

    void LoadDialogue()
    {
        if (dialogueFile != null)
        {
            lines.Clear();
            // 讀取 txt 並分割行
            string[] splitLines = dialogueFile.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
            lines.AddRange(splitLines);
        }
        else
        {
            Debug.LogError("找不到對話 txt 檔！請檢查 Inspector 中的 dialogueFile 欄位。");
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
            EndDialogue();
        }
    }

    void ShowLine()
    {
        if (lines.Count > 0 && currentIndex < lines.Count)
        {
            string rawLine = lines[currentIndex].Trim();

            // 解析標籤，例如 [Wave]你好
            if (rawLine.StartsWith("[") && rawLine.Contains("]"))
            {
                int tagEnd = rawLine.IndexOf(']');
                string actionTag = rawLine.Substring(1, tagEnd - 1);
                string textToShow = rawLine.Substring(tagEnd + 1);

                if (npcAnimator != null)
                {
                    npcAnimator.SetTrigger(actionTag);
                }
                dialogueContent.text = textToShow;
            }
            else
            {
                dialogueContent.text = rawLine;
            }
        }
    }

    void EndDialogue()
    {
        if (dialoguePlane != null) dialoguePlane.SetActive(false);
        // 重置索引，以便下次對話可以從頭開始
        currentIndex = 0;
    }
}