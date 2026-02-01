using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;

public class VRDialogueController : MonoBehaviour
{
    [Header("UI 引用")]
    public GameObject dialoguePlane;       // 對話背景板
    public TextMeshProUGUI dialogueContent; // 對話文字
    public Button yesButton;
    public Button noButton;

    [Header("NPC 設定")]
    public Animator npcAnimator;
    public float moveSpeed = 2.0f;
    public float rotationSpeed = 5.0f;

    [Header("配置")]
    public TextAsset dialogueFile;
    public InputActionProperty rightTriggerAction; // 右手 Trigger 繼續

    private List<string> lines = new List<string>();
    private int currentIndex = 0;
    private bool isWaitingForChoice = false;
    private string yesTarget;
    private string noTarget;
    private Coroutine movementCoroutine;

    private void Awake() => LoadDialogue();

    private void Start()
    {
        // 確保背景板開啟
        if (dialoguePlane != null) dialoguePlane.SetActive(true);

        // 綁定按鈕事件
        if (yesButton != null) yesButton.onClick.AddListener(OnYesClicked);
        if (noButton != null) noButton.onClick.AddListener(OnNoClicked);

        // 初始狀態：隱藏按鈕 (隱藏按鈕時文字會自動開啟)
        SetButtonsActive(false);

        // 避開開頭的 :: 標籤並顯示第一行
        CheckAndSkipLabels();
        ShowLine(false);
    }

    private void OnEnable()
    {
        if (rightTriggerAction.action != null)
        {
            rightTriggerAction.action.started += OnTriggerPressed;
            rightTriggerAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (rightTriggerAction.action != null)
            rightTriggerAction.action.started -= OnTriggerPressed;
    }

    private void OnTriggerPressed(InputAction.CallbackContext context)
    {
        // 只有在非選擇模式下，按 Trigger 才能換行
        if (dialoguePlane != null && dialoguePlane.activeSelf && !isWaitingForChoice)
        {
            OnNextStep();
        }
    }

    public void OnNextStep()
    {
        currentIndex++;

        if (currentIndex < lines.Count)
        {
            string line = lines[currentIndex].Trim();

            // 處理 JumpTo 直接跳轉
            if (line.StartsWith("JumpTo::"))
            {
                JumpToSection(line.Replace("JumpTo", "").Trim());
                return;
            }

            // 遇到隱形標籤行，代表該段落結束
            if (line.StartsWith("::"))
            {
                ResetDialogueUI();
                return;
            }

            // 判斷是否接續上一行文字 [w]
            bool isAppend = false;
            if (currentIndex > 0 && lines[currentIndex - 1].Contains("[w]"))
            {
                isAppend = true;
            }

            ShowLine(isAppend);
        }
        else
        {
            ResetDialogueUI();
        }
    }

    void ShowLine(bool append)
    {
        if (currentIndex >= lines.Count) return;

        string rawLine = lines[currentIndex].Trim();

        // 如果是標籤行，自動跳過
        if (rawLine.StartsWith("::"))
        {
            OnNextStep();
            return;
        }

        // 清理 UI 標籤
        string cleanLine = rawLine.Replace("[w]", "").Replace("[lr]", "");

        // 解析功能標籤 []
        if (cleanLine.StartsWith("[") && cleanLine.Contains("]"))
        {
            int tagEnd = cleanLine.IndexOf(']');
            string tagContent = cleanLine.Substring(1, tagEnd - 1);
            string dialogueText = cleanLine.Substring(tagEnd + 1).Trim();

            // 1. 移動處理
            if (tagContent.StartsWith("NpcMove"))
            {
                HandleNpcMoveTag(tagContent);
                UpdateText(dialogueText, append);
            }
            // 2. 選擇處理
            else if (tagContent.StartsWith("CHOICE:"))
            {
                string[] targets = tagContent.Replace("CHOICE:", "").Split(',');
                yesTarget = targets[0].Trim();
                noTarget = targets[1].Trim();

                isWaitingForChoice = true;
                UpdateText(dialogueText, append); // 更新文字內容
                SetButtonsActive(true);           // 顯示按鈕 (此時文字會被隱藏)
            }
            // 3. 一般動畫 Trigger
            else
            {
                if (npcAnimator != null) npcAnimator.SetTrigger(tagContent);
                UpdateText(dialogueText, append);
            }
        }
        else
        {
            UpdateText(cleanLine, append);
        }
    }

    void UpdateText(string newText, bool append)
    {
        if (dialogueContent == null) return;

        if (append)
            dialogueContent.text += "\n" + newText;
        else
            dialogueContent.text = newText;
    }

    // --- 按鈕與文字互斥邏輯 ---
    void SetButtonsActive(bool state)
    {
        if (yesButton != null) yesButton.gameObject.SetActive(state);
        if (noButton != null) noButton.gameObject.SetActive(state);

        // 如果按鈕出現，文字就關閉；按鈕關閉，文字就出現
        if (dialogueContent != null)
        {
            dialogueContent.gameObject.SetActive(!state);
        }
    }

    public void OnYesClicked() => JumpToSection(yesTarget);
    public void OnNoClicked() => JumpToSection(noTarget);

    public void JumpToSection(string sectionTag)
    {
        isWaitingForChoice = false;
        SetButtonsActive(false); // 恢復文字顯示

        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].Trim() == sectionTag)
            {
                currentIndex = i;
                currentIndex++; // 跳過標籤行
                ShowLine(false);
                return;
            }
        }
    }

    // --- 移動邏輯 ---
    void HandleNpcMoveTag(string tag)
    {
        string[] parts = tag.Split(',');
        if (parts.Length >= 4)
        {
            float x = float.Parse(parts[1]);
            float y = float.Parse(parts[2]);
            float z = float.Parse(parts[3]);
            MoveNpc(new Vector3(x, y, z));
        }
    }

    void MoveNpc(Vector3 target)
    {
        if (movementCoroutine != null) StopCoroutine(movementCoroutine);
        movementCoroutine = StartCoroutine(MoveNpcRoutine(target));
    }

    IEnumerator MoveNpcRoutine(Vector3 target)
    {
        if (npcAnimator != null) npcAnimator.SetTrigger("walk");

        while (Vector3.Distance(npcAnimator.transform.position, target) > 0.1f)
        {
            Vector3 direction = (target - npcAnimator.transform.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion lookRot = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
                npcAnimator.transform.rotation = Quaternion.Slerp(npcAnimator.transform.rotation, lookRot, Time.deltaTime * rotationSpeed);
            }
            npcAnimator.transform.position = Vector3.MoveTowards(npcAnimator.transform.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }
    }

    // --- 系統功能 ---
    void ResetDialogueUI()
    {
        if (dialogueContent != null) dialogueContent.text = "";
        SetButtonsActive(false);
        isWaitingForChoice = false;
    }

    void CheckAndSkipLabels()
    {
        while (currentIndex < lines.Count && lines[currentIndex].Trim().StartsWith("::"))
        {
            currentIndex++;
        }
    }

    void LoadDialogue()
    {
        if (dialogueFile != null)
        {
            lines = new List<string>(dialogueFile.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries));
        }
    }
}