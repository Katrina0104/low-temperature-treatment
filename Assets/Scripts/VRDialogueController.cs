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
    public InputActionProperty nextLineAction; // 原本的 rightTriggerAction 改名或重新綁定為 A 鍵
    public InputActionProperty prevLineAction; // 新增 B 鍵的綁定

    private List<string> lines = new List<string>();
    private int currentIndex = 0;
    private int returnIndex = -1;
    private bool isWaitingForChoice = false;
    private bool isCountingDown = false; // 用於 WAIT 標籤
    private string yesTarget;
    private string noTarget;
    private Coroutine movementCoroutine;
    private Coroutine waitCoroutine;

    [Header("外部系統連結")]
    public EventManager eventManager;

    [Header("防卡鍵保護開關")]
    private bool isButtonAvailable = true; // 保護鎖，防止 Unity 運作延遲漏掉放開訊號

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
        // 1. 綁定下一行 (A鍵)
        if (nextLineAction.action != null)
        {
            nextLineAction.action.performed += OnNextLinePerformed;
            nextLineAction.action.Enable();
        }

        // 2. 綁定上一行 (B鍵)
        if (prevLineAction.action != null)
        {
            prevLineAction.action.performed += OnPrevLinePerformed;
            prevLineAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        // 3. 確實解除下一行監聽
        if (nextLineAction.action != null)
        {
            nextLineAction.action.performed -= OnNextLinePerformed;
            nextLineAction.action.Disable(); // 順便關閉 Action 釋放資源
        }

        // 4. 確實解除上一行監聽 (修正原本誤寫成 += 的代碼)
        if (prevLineAction.action != null)
        {
            prevLineAction.action.performed -= OnPrevLinePerformed;
            prevLineAction.action.Disable();
        }
    }

    // 右手 A 鍵被確實按下時觸發
    private void OnNextLinePerformed(InputAction.CallbackContext context)
    {
        // 如果保護鎖啟動中、等待選擇中或倒數中，直接攔截不執行
        if (!isButtonAvailable || isWaitingForChoice || isCountingDown) return;

        // 啟動 0.2 秒保護鎖，讓模擬器有時間消化「按鍵彈起」的訊號
        StartCoroutine(ButtonCooldownRoutine());

        OnNextStep();
    }

    // 右手 B 鍵被確實按下時觸發
    private void OnPrevLinePerformed(InputAction.CallbackContext context)
    {
        if (!isButtonAvailable || isWaitingForChoice || isCountingDown) return;

        StartCoroutine(ButtonCooldownRoutine());

        OnPrevStep(); // 呼叫你寫好的回溯上一行方法
    }

    // 0.2 秒的極短保護冷卻
    private IEnumerator ButtonCooldownRoutine()
    {
        isButtonAvailable = false;
        yield return new WaitForSeconds(0.2f); // 0.2秒對人類無感，但對處理器來說很久
        isButtonAvailable = true;
    }

    public void OnNextStep()
    {
        Debug.Log($"OnNextStep called, currentIndex: {currentIndex}");
        currentIndex++;
        ProcessCurrentLine();
    }

    public void OnPrevStep()
    {
        if (currentIndex <= 0) return;

        // 重置可能卡住的狀態
        isWaitingForChoice = false;
        isCountingDown = false;
        SetButtonsActive(false);

        // 停止所有 Coroutine（避免 WAIT 倒數還在跑）
        StopAllCoroutines();

        do
        {
            currentIndex--;
        } while (currentIndex > 0 && IsLogicLabel(lines[currentIndex].Trim()));

        ProcessCurrentLine();
    }

    // 輔助判斷是否為邏輯標籤，避免回溯時卡在系統指令上
    private bool IsLogicLabel(string line)
    {
        return line.StartsWith("::") ||
               line.StartsWith("[IF") ||
               line.StartsWith("[ELSE]") ||
               line.StartsWith("[ENDIF]") ||
               line.StartsWith("JumpTo::") ||
               line.StartsWith("[ROLL") ||
               line.StartsWith("[INC_DAY]");
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

        //5.調整溫度跟時間的textmesh
        if (line.StartsWith("[IF:TEXT_CHECK"))
        {
            // 格式範例: [IF:TEXT_CHECK:TemperatureValueText:33度]
            string targetObjectName = "";
            string expectedText = "";

            string[] parts = line.Replace("[", "").Replace("]", "").Split(':');
            if (parts.Length >= 4)
            {
                targetObjectName = parts[2].Trim(); // 物件名稱
                expectedText = parts[3].Trim();     // 想要看到的文字
            }

            // 呼叫自定義的檢查方法
            bool isMatch = CheckSpecificText(targetObjectName, expectedText);

            if (!isMatch)
            {
                Debug.Log($"<color=orange>[檢查失敗]</color> 物件 '{targetObjectName}' 文字不含 '{expectedText}'，跳往 [ELSE]");
                SkipToTarget("[ELSE]", "[ENDIF]");
                OnNextStep();
                return;
            }
            else
            {
                Debug.Log($"<color=green>[檢查通過]</color> 物件 '{targetObjectName}' 內容正確！");
                OnNextStep();
                return;
            }
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
                    if (waitCoroutine != null) StopCoroutine(waitCoroutine); // 萬一有舊的先停
                    waitCoroutine = StartCoroutine(WaitTimeRoutine(s));      // 記住新的
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
        waitCoroutine = null;   // 跑完自己清掉
        OnNextStep();
    }

    public void JumpToSection(string sectionTag)
    {
        if (waitCoroutine != null)
        {
            StopCoroutine(waitCoroutine);  // ← 這才是真正停掉
            waitCoroutine = null;
        }

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

    private bool CheckSpecificText(string objectName, string targetText)
    {
        GameObject targetGO = null;

        // 1. 遍歷全場景（包含隱藏物件）尋找目標物件
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform t in allTransforms)
        {
            if (t.name == objectName && t.gameObject.scene.name != null)
            {
                targetGO = t.gameObject;
                break;
            }
        }

        if (targetGO != null)
        {
            // 2. 獲取文字組件
            TextMeshProUGUI tmp = targetGO.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                string currentText = tmp.text.Trim();
                // 使用 Contains 比較安全，避免空格或隱藏字元干擾
                return currentText.Contains(targetText);
            }
            else
            {
                Debug.LogWarning($"<color=red>[錯誤]</color> 物件 '{objectName}' 沒有 TextMeshProUGUI 組件！");
            }
        }
        else
        {
            Debug.LogWarning($"<color=red>[錯誤]</color> 找不到名為 '{objectName}' 的文字物件！");
        }

        return false;
    }
}