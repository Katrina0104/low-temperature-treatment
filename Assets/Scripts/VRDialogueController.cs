using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;

public class VRDialogueController : MonoBehaviour
{
    [Header("UI 引用")]
    public GameObject dialoguePlane;
    public TextMeshProUGUI dialogueContent;
    public Button yesButton;
    public Button noButton;

    [Header("NPC 設定")]
    public Animator npcAnimator;
    public float moveSpeed = 2.0f;
    public float rotationSpeed = 5.0f;

    [Header("配置")]
    public TextAsset dialogueFile;
    public InputActionProperty rightTriggerAction;

    private List<string> lines = new List<string>();
    private int currentIndex = 0;
    private int returnIndex = -1;
    private bool isWaitingForChoice = false;
    private bool isCountingDown = false; // 用於 WAIT 標籤
    private string yesTarget;
    private string noTarget;
    private Coroutine movementCoroutine;

    [Header("外部系統連結")]
    public EventManager eventManager;

    private void Awake() => LoadDialogue();

    private void Start()
    {
        if (dialoguePlane != null) dialoguePlane.SetActive(true);

        if (yesButton != null) yesButton.onClick.AddListener(OnYesClicked);
        if (noButton != null) noButton.onClick.AddListener(OnNoClicked);

        SetButtonsActive(false);
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
        // 只有在非選擇模式、非計時模式下，按 Trigger 才能換行
        if (!isWaitingForChoice && !isCountingDown)
        {
            OnNextStep();
        }
    }

    public void OnNextStep()
    {
        currentIndex++;
        ProcessCurrentLine();
    }

    // 將邏輯拆分，避免直接遞迴 OnNextStep 導致 StackOverflow
    private void ProcessCurrentLine()
    {
        if (currentIndex >= lines.Count)
        {
            ResetDialogueUI();
            return;
        }

        string line = lines[currentIndex].Trim();

        // 1. 處理跳轉標籤
        if (line.StartsWith("JumpTo::"))
        {
            JumpToSection(line.Replace("JumpTo", "").Trim());
            return;
        }

        if (line.StartsWith("[RETURN]"))
        {
            if (returnIndex != -1)
            {
                currentIndex = returnIndex; // 回到 ROLL 那一行
                returnIndex = -1;
                OnNextStep(); // 執行 ROLL 的下一行
            }
            return;
        }

        // 2. 處理邏輯標籤 (自動跳過，不顯示)
        if (line.StartsWith("::") || line.StartsWith("[ENDIF]") || line.StartsWith("[ELSE]"))
        {
            currentIndex++;
            ProcessCurrentLine(); // 使用私有方法進行內部跳轉
            return;
        }

        // 3. 處理 IF 邏輯 (偵測物件是否為 Active)
        if (line.StartsWith("[IF:IS_ACTIVE"))
        {
            string targetName = "";
            if (line.Contains(":"))
            {
                string[] parts = line.Replace("[", "").Replace("]", "").Split(':');
                if (parts.Length > 2) targetName = parts[2].Trim();
            }

            // 尋找場景中的物件
            // 修改後的程式碼：支援尋找隱藏物件與子物件
            GameObject targetGO = null;

            // 1. 先嘗試找顯示的物件
            targetGO = GameObject.Find(targetName);

            // 2. 如果找不到，改用遍歷全場景的方法 (包含隱藏的子物件)
            if (targetGO == null)
            {
                Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
                foreach (Transform t in allTransforms)
                {
                    // 條件：名稱正確 且 物件不是 Prefab 資源 (必須存在於場景中)
                    if (t.name == targetName && t.gameObject.scene.name != null)
                    {
                        targetGO = t.gameObject;
                        break;
                    }
                }
            }
            bool isActive = (targetGO != null && targetGO.activeInHierarchy);

            // --- DEBUG 資訊 ---
            if (targetGO == null)
            {
                Debug.LogWarning($"<color=red>[錯誤]</color> 找不到名為 '{targetName}' 的物件！請檢查 Hierarchy 名稱。");
            }
            else
            {
                Debug.Log($"<color=yellow>[檢查]</color> 物件 '{targetName}' 當前狀態: {(isActive ? "顯示中(True)" : "隱藏中(False)")}");
            }

            if (!isActive)
            {
                Debug.Log("<color=cyan>[跳轉]</color> 條件不成立，跳往 [ELSE]/[ENDIF]");
                SkipToTarget("[ELSE]", "[ENDIF]");
                OnNextStep();
                return;
            }
            else
            {
                Debug.Log("<color=green>[通過]</color> 條件成立，繼續執行內容");
                OnNextStep();
                return;
            }
        }

        //4. Event
        // --- 在 ProcessCurrentLine() 內部對應位置加入 ---

        if (line.StartsWith("[RETURN]"))
        {
            if (returnIndex != -1)
            {
                Debug.Log($"<color=cyan>[RETURN]</color> 執行返回邏輯，回到索引: {returnIndex}");
                currentIndex = returnIndex;
                returnIndex = -1;
                OnNextStep();
            }
            else
            {
                Debug.LogWarning("<color=red>[警告]</color> 執行 [RETURN] 但 returnIndex 為 -1 (無紀錄回傳點)");
            }
            return;
        }

        // --- Event 觸發 Log ---

        if (line.StartsWith("[ROLL_EQUIP]"))
        {
            Debug.Log("<color=white>[ROLL]</color> 請求設備事件判定...");
            returnIndex = currentIndex; // <--- 關鍵！紀錄當前位置，之後 [RETURN] 才知道回哪裡
            if (eventManager != null) eventManager.RollEquipmentEvent();
            return;
        }
        if (line.StartsWith("[ROLL_SHIVER]"))
        {
            Debug.Log("<color=white>[ROLL]</color> 請求顫抖事件判定...");
            returnIndex = currentIndex; // <--- 關鍵！
            if (eventManager != null) eventManager.RollShiveringEvent();
            return;
        }
        if (line.StartsWith("[ROLL_BP]"))
        {
            Debug.Log("<color=white>[ROLL]</color> 請求血壓事件判定...");
            returnIndex = currentIndex; // <--- 關鍵！
            if (eventManager != null) eventManager.RollBPEvent();
            return;
        }


        if (line.StartsWith("[INC_DAY]"))
        {
            Debug.Log("<color=magenta>[SYSTEM]</color> 觸發天數增加 [INC_DAY]");
            if (eventManager != null) eventManager.NextDay();
            OnNextStep();
            return;
        }

        if (line.StartsWith("[ALARM_OFF]"))
        {
            if (eventManager != null) eventManager.SetAlarm(false);
            OnNextStep();
            return;
        }

        // 5. 顯示台詞內容
        bool isAppend = (currentIndex > 0 && lines[currentIndex - 1].Contains("[w]"));
        ShowLine(isAppend);
    }

    void ShowLine(bool append)
    {
        if (currentIndex >= lines.Count) return;

        string rawLine = lines[currentIndex].Trim();
        string cleanLine = rawLine.Replace("[w]", "").Replace("[lr]", "");

        if (cleanLine.StartsWith("[") && cleanLine.Contains("]"))
        {
            int tagEnd = cleanLine.IndexOf(']');
            string tagContent = cleanLine.Substring(1, tagEnd - 1);
            string dialogueText = cleanLine.Substring(tagEnd + 1).Trim();

            // 處理標籤功能
            if (tagContent.StartsWith("NpcMove"))
            {
                HandleNpcMoveTag(tagContent);
                UpdateText(dialogueText, append);
            }
            else if (tagContent.StartsWith("CHOICE:"))
            {
                string[] targets = tagContent.Replace("CHOICE:", "").Split(',');
                // 統一格式：確保標籤前綴一致
                yesTarget = targets[0].Trim().StartsWith("::") ? targets[0].Trim() : "::" + targets[0].Trim();
                noTarget = targets[1].Trim().StartsWith("::") ? targets[1].Trim() : "::" + targets[1].Trim();

                isWaitingForChoice = true;
                UpdateText(dialogueText, append);
                SetButtonsActive(true);
            }
            else if (tagContent.StartsWith("WAIT:"))
            {
                if (float.TryParse(tagContent.Replace("WAIT:", ""), out float s))
                {
                    UpdateText(dialogueText, append);
                    StartCoroutine(WaitTimeRoutine(s));
                }
            }
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

    // --- 輔助邏輯 ---

    bool CheckSocketCondition(string line)
    {
        string[] parts = line.Replace("[", "").Replace("]", "").Split(':');
        string targetItemName = (parts.Length > 2) ? parts[2].Trim() : "";

        Debug.Log($"<color=yellow>[IF 條件檢查]</color> 尋找目標: '<b>{targetItemName}</b>'");

        ReplaceActivateChildren[] sockets = FindObjectsOfType<ReplaceActivateChildren>();
        Debug.Log($"<color=yellow>[IF 條件檢查]</color> 找到 {sockets.Length} 個 Socket 物件");

        foreach (var s in sockets)
        {
            string socketName = s.gameObject.name;
            bool isActivated = s.CheckIfActivated();
            Debug.Log($"  - Socket 名稱: '<b>{socketName}</b>' (長度: {socketName.Length}), 已激活: {isActivated}");

            if (socketName == targetItemName && isActivated)
            {
                Debug.Log($"<color=green>[IF 條件成立✓]</color> '{targetItemName}' 已連接");
                return true;
            }

            if (socketName == targetItemName && !isActivated)
            {
                Debug.Log($"<color=orange>[IF 條件失敗✗]</color> 找到 '{targetItemName}'，但尚未激活");
            }
        }

        Debug.Log($"<color=red>[IF 條件失敗✗]</color> 未找到或未激活 '{targetItemName}'");
        return false;
    }

    void SkipToTarget(string targetA, string targetB)
    {
        while (currentIndex < lines.Count)
        {
            string l = lines[currentIndex].Trim();
            if (l == targetA || l == targetB) return;
            currentIndex++;
        }
    }

    IEnumerator WaitTimeRoutine(float seconds)
    {
        isCountingDown = true;
        yield return new WaitForSeconds(seconds);
        isCountingDown = false;
        OnNextStep(); // 時間到自動下一行
    }

    public void JumpToSection(string sectionTag)
    {
        isWaitingForChoice = false;
        isCountingDown = false;
        SetButtonsActive(false);

        string target = sectionTag.StartsWith("::") ? sectionTag : "::" + sectionTag;

        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].Trim() == target)
            {
                currentIndex = i;
                ProcessCurrentLine(); // 找到後立即處理該行(標籤會被自動跳過)
                return;
            }
        }
        Debug.LogError("找不到標籤: " + target);
    }

    public void OnYesClicked() => JumpToSection(yesTarget);
    public void OnNoClicked() => JumpToSection(noTarget);

    // --- 基礎 UI 與移動 ---

    void UpdateText(string newText, bool append)
    {
        if (dialogueContent == null) return;
        if (append) dialogueContent.text += "\n" + newText;
        else dialogueContent.text = newText;
    }

    void SetButtonsActive(bool state)
    {
        if (yesButton != null) yesButton.gameObject.SetActive(state);
        if (noButton != null) noButton.gameObject.SetActive(state);
        if (dialogueContent != null) dialogueContent.gameObject.SetActive(!state);
    }

    void ResetDialogueUI()
    {
        if (dialogueContent != null) dialogueContent.text = "";
        SetButtonsActive(false);
        isWaitingForChoice = false;
        isCountingDown = false;
    }

    void CheckAndSkipLabels()
    {
        while (currentIndex < lines.Count && lines[currentIndex].Trim().StartsWith("::")) currentIndex++;
    }

    void LoadDialogue()
    {
        if (dialogueFile != null)
            lines = new List<string>(dialogueFile.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries));
    }

    void HandleNpcMoveTag(string tag)
    {
        string[] parts = tag.Split(',');
        if (parts.Length >= 4)
        {
            if (float.TryParse(parts[1], out float x) && float.TryParse(parts[2], out float y) && float.TryParse(parts[3], out float z))
            {
                MoveNpc(new Vector3(x, y, z));
            }
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
            Vector3 dir = (target - npcAnimator.transform.position).normalized;
            if (dir != Vector3.zero) npcAnimator.transform.rotation = Quaternion.Slerp(npcAnimator.transform.rotation, Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z)), Time.deltaTime * rotationSpeed);
            npcAnimator.transform.position = Vector3.MoveTowards(npcAnimator.transform.position, target, moveSpeed * Time.deltaTime);
            yield return null;
        }
        if (npcAnimator != null) npcAnimator.SetTrigger("idle");
    }
}