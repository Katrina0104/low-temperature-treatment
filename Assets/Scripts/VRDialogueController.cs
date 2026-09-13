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
    private bool isUIButtonPressed = false;

    [Header("外部系統連結")]
    public EventManager eventManager;
    public AIController aiController;

    [Header("提示系統（僅測驗模式顯示）")]
    [Tooltip("「提示」按鈕。提示內容取自 HintCatalog，直接顯示在 dialoguePlane 上，再按一次或按 A 關閉")]
    public GameObject hintButton;

    [Header("除錯")]
    [Tooltip("勾選後按 Play 直接跑 dialogueFile，不等 ModeSelector")]
    public bool autoStartOnPlay = false;

    [Header("防卡鍵保護開關")]
    private bool isButtonAvailable = true; // 保護鎖，防止 Unity 運作延遲漏掉放開訊號

    private bool isPracticeMode = false;
    private bool isReviewMode = false;

    // 提示系統狀態
    private string currentSection = "";        // 目前所在的 :: 段落
    private string lastHintableSection = "";   // 最近一個「HintCatalog 裡有提示」的段落
    private bool isHintOpen = false;
    private string savedDialogueText = "";     // 提示蓋掉對話框前先存起來
    private float hintOpenedAt;                // 用來把閱讀提示的時間從停留時間扣掉

    public bool IsPracticeMode => isPracticeMode;
    public bool IsReviewMode => isReviewMode;
    public string CurrentSection => currentSection;

    private void Start()
    {
        if (yesButton != null) yesButton.onClick.AddListener(OnYesClicked);
        if (noButton != null) noButton.onClick.AddListener(OnNoClicked);
        // 提示按鈕若本身帶 Button 元件就自動接上，不用在 Inspector 手動掛 OnClick
        if (hintButton != null)
        {
            var hb = hintButton.GetComponent<Button>();
            if (hb != null) hb.onClick.AddListener(OnHintClicked);
        }

        SetButtonsActive(false);
        ResetHintState();

        if (autoStartOnPlay)
        {
            StartDialogue(dialogueFile, false);
        }
        else
        {
            // OnEnable 會先啟用 A/B 鍵，這裡要關掉，
            // 否則玩家還在 StartView 選模式時按 A 就會去推進一個空腳本
            if (dialoguePlane != null) dialoguePlane.SetActive(false);
            EnableInput(false);
        }
    }

    /// <summary>由 ModeSelector 呼叫，載入指定腳本並從頭開始。</summary>
    public void StartDialogue(TextAsset script, bool practiceMode)
    {
        StopAllCoroutines();
        waitCoroutine = null;
        movementCoroutine = null;

        dialogueFile = script;
        isPracticeMode = practiceMode;
        isReviewMode = false;
        LoadDialogue();

        ResetRuntimeState();
        currentIndex = 0;
        currentSection = "";
        lastHintableSection = "";

        if (dialoguePlane != null) dialoguePlane.SetActive(true);
        EnableInput(true);

        // 提示只在測驗模式提供，教學模式本來就會把答案講出來
        ResetHintState();
        if (hintButton != null) hintButton.SetActive(practiceMode);

        CheckAndSkipLabels();
        ShowLine(false);
    }

    /// <summary>由 AIController 呼叫，跳到單一步驟做復習。</summary>
    public void StartReview(string sectionTag)
    {
        StopAllCoroutines();
        waitCoroutine = null;
        movementCoroutine = null;

        isReviewMode = true;
        ResetRuntimeState();
        currentSection = "";
        lastHintableSection = "";   // 復習從事件開頭開始，提示不要沿用上一段的內容

        if (dialoguePlane != null) dialoguePlane.SetActive(true);
        EnableInput(true);

        ResetHintState();
        if (hintButton != null) hintButton.SetActive(isPracticeMode);

        JumpToSection(sectionTag);
    }

    /// <summary>顯示報告時暫停對話：收起對話框並停用輸入，避免按 A 誤推進流程。</summary>
    public void SuspendForReport()
    {
        StopAllCoroutines();
        waitCoroutine = null;
        movementCoroutine = null;

        isReviewMode = false;
        ResetRuntimeState();

        if (dialoguePlane != null) dialoguePlane.SetActive(false);
        EnableInput(false);

        ResetHintState();
        if (hintButton != null) hintButton.SetActive(false);
    }

    private void ResetRuntimeState()
    {
        returnIndex = -1;
        isWaitingForChoice = false;
        isCountingDown = false;
        isUIButtonPressed = false;
        isButtonAvailable = true;
        SetButtonsActive(false);
    }

    private void EnableInput(bool on)
    {
        if (nextLineAction.action != null)
        {
            if (on) nextLineAction.action.Enable(); else nextLineAction.action.Disable();
        }
        if (prevLineAction.action != null)
        {
            if (on) prevLineAction.action.Enable(); else prevLineAction.action.Disable();
        }
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
        if (!isButtonAvailable) return;

        // 啟動 0.2 秒保護鎖，讓模擬器有時間消化「按鍵彈起」的訊號
        StartCoroutine(ButtonCooldownRoutine());

        // 提示開啟中：這次按鍵只用來關掉提示，不推進流程
        if (isHintOpen) { CloseHint(); return; }

        if (isWaitingForChoice || isCountingDown) return;

        OnNextStep();
    }

    // 右手 B 鍵被確實按下時觸發
    private void OnPrevLinePerformed(InputAction.CallbackContext context)
    {
        if (!isButtonAvailable) return;

        if (isHintOpen)
        {
            StartCoroutine(ButtonCooldownRoutine());
            CloseHint();
            return;
        }

        if (isWaitingForChoice || isCountingDown) return;

        OnPrevStep(); // 呼叫你寫好的回溯上一行方法

        // 冷卻鎖要在 OnPrevStep 之後才啟動。OnPrevStep 裡有 StopAllCoroutines()，
        // 先啟動的冷卻協程會被一起殺掉，isButtonAvailable 就永遠停在 false，
        // 之後 A、B 鍵全部被擋下，對話卡死。
        StartCoroutine(ButtonCooldownRoutine());
    }

    // 0.2 秒的極短保護冷卻
    private IEnumerator ButtonCooldownRoutine()
    {
        isButtonAvailable = false;
        yield return new WaitForSeconds(0.2f); // 0.2秒對人類無感，但對處理器來說很久
        isButtonAvailable = true;
    }

    public void OnUIButtonPressed()
    {
        isUIButtonPressed = true;
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
               line.StartsWith("[INC_DAY]") ||
               line.StartsWith("[IF:BUTTON_PRESSED") ||
               line.StartsWith("[REPORT]") ||
               line.StartsWith("[END]");
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
            // 復習模式下沒有 ROLL 設過 returnIndex，直接視為該步驟練完，回報告
            if (isReviewMode)
            {
                if (aiController != null) aiController.EndReview(true);
                return;
            }

            if (returnIndex != -1)
            {
                currentIndex = returnIndex; // 回到 ROLL 那一行
                returnIndex = -1;
                OnNextStep(); // 執行 ROLL 的下一行
            }
            else
            {
                Debug.LogWarning("<color=red>[警告]</color> 執行 [RETURN] 但 returnIndex 為 -1 (無紀錄回傳點)");
            }
            return;
        }

        // 2. 處理狀態標籤 (自動跳過，不顯示)
        // 所有進入狀態的路徑都會經過這裡：JumpTo::、[CHOICE] 按鈕、EventManager
        // 觸發事件、以及流程自然往下掉，因此這是 HMM 唯一需要的掛載點。
        if (line.StartsWith("::"))
        {
            currentSection = line;
            // 分支標籤（例如 ::Note7_Yes）沒有自己的提示，
            // 記住最後一個有提示的段落，按提示才不會落空
            if (HintCatalog.Has(line)) lastHintableSection = line;

            // 回傳 false = 復習已完成，中止流程回到報告
            if (aiController != null && !aiController.OnEnterState(line)) return;

            currentIndex++;
            ProcessCurrentLine(); // 使用私有方法進行內部跳轉
            return;
        }

        if (line.StartsWith("[ENDIF]") || line.StartsWith("[ELSE]"))
        {
            currentIndex++;
            ProcessCurrentLine();
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

            GameObject targetGO = FindSceneObjectByName(targetName);
            bool isActive = (targetGO != null && targetGO.activeInHierarchy);

            // --- DEBUG 資訊 ---
            if (targetGO == null)
            {
                Debug.LogWarning($"<color=red>[錯誤]</color> 找不到名為 '{targetName}' 的物件！請檢查 Hierarchy 名稱。");
            }
            else if (isActive)
            {
                Debug.Log($"<color=yellow>[檢查]</color> 物件 '{targetName}' 當前狀態: 顯示中(True)");
            }
            else if (targetGO.activeSelf)
            {
                // 自己是開的但整條 hierarchy 不是 → 一定有某個父物件是關的
                Transform blocker = targetGO.transform.parent;
                while (blocker != null && blocker.gameObject.activeSelf) blocker = blocker.parent;
                Debug.LogWarning($"<color=red>[檢查失敗]</color> 物件 '{targetName}' 自己是啟用的(activeSelf=true)，" +
                                 $"但父物件 '<b>{(blocker != null ? blocker.name : "?")}</b>' 是關閉的，" +
                                 $"導致 activeInHierarchy=false。請改成啟用該父物件，或把檢查目標改成它。");
            }
            else
            {
                Debug.Log($"<color=yellow>[檢查]</color> 物件 '{targetName}' 當前狀態: 隱藏中(False)，尚未被 SetActive(true)");
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

        if (line.StartsWith("[IF:BUTTON_PRESSED"))
        {
            if (!isUIButtonPressed)
            {
                Debug.Log("<color=orange>[BUTTON CHECK]</color> 未按下");
                SkipToTarget("[ELSE]", "[ENDIF]");
                OnNextStep();
                return;
            }
            else
            {
                isUIButtonPressed = false; // 重置
                Debug.Log("<color=green>[BUTTON CHECK]</color> 已按下，通過！");
                OnNextStep();
                return;
            }
        }

        //4. Event
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
            // 格式範例: [IF:TEXT_CHECK:Cooling_temperature:33:EQ]   數值要等於 33
            //          [IF:TEXT_CHECK:reheat_rate:0.25:LTE]      數值不可超過 0.25
            // 第五段省略時預設為 EQ（等於）。
            string targetObjectName = "";
            string expectedText = "";
            string compareMode = "EQ";

            string[] parts = line.Replace("[", "").Replace("]", "").Split(':');
            if (parts.Length >= 4)
            {
                targetObjectName = parts[2].Trim(); // 物件名稱
                expectedText = parts[3].Trim();     // 想要看到的文字/數值
            }
            if (parts.Length >= 5) compareMode = parts[4].Trim().ToUpperInvariant();

            // 呼叫自定義的檢查方法
            bool isMatch = CheckSpecificText(targetObjectName, expectedText, compareMode);

            if (!isMatch)
            {
                Debug.Log($"<color=orange>[檢查失敗]</color> 物件 '{targetObjectName}' 檢查不符 '{expectedText}'，跳往 [ELSE]");
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

        // 產出學習診斷報告，之後不再往下跑，等玩家在報告面板操作
        if (line.StartsWith("[REPORT]"))
        {
            // 復習「貼片移除確認」會走到這裡；復習中不重新產生報告，直接回報告
            if (isReviewMode)
            {
                if (aiController != null) aiController.EndReview(true);
                return;
            }

            if (aiController != null) aiController.GenerateReport();
            else Debug.LogWarning("<color=orange>[REPORT]</color> aiController 未指派，無法產出報告");
            return;
        }

        if (line.StartsWith("[END]"))
        {
            // 復習模式跑到腳本結尾，視為該步驟練完，回報告而不是關閉整個系統
            if (isReviewMode)
            {
                if (aiController != null) aiController.EndReview(true);
                return;
            }

            Debug.Log("<color=red>[END]</color> 對話結束，停止所有流程");
            StopAllCoroutines();

            if (waitCoroutine != null) waitCoroutine = null;
            if (movementCoroutine != null) movementCoroutine = null;

            isWaitingForChoice = false;
            isCountingDown = false;
            isButtonAvailable = true;

            ResetDialogueUI();

            if (nextLineAction.action != null) nextLineAction.action.Disable();
            if (prevLineAction.action != null) prevLineAction.action.Disable();

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

    // ==================== 提示系統 ====================

    private void ResetHintState()
    {
        isHintOpen = false;
        savedDialogueText = "";
    }

    /// <summary>接在「提示」按鈕的 OnClick 上。再按一次會關閉。</summary>
    public void OnHintClicked()
    {
        if (isHintOpen) CloseHint();
        else OpenHint();
    }

    /// <summary>提示直接顯示在 dialoguePlane 上，暫時蓋掉原本的對話內容。</summary>
    private void OpenHint()
    {
        if (dialogueContent == null) return;

        string text = HintCatalog.Hint(lastHintableSection);
        if (string.IsNullOrEmpty(text)) text = "這一步沒有可用的提示。";

        savedDialogueText = dialogueContent.text;
        isHintOpen = true;
        hintOpenedAt = Time.time;

        // 選擇題進行中時 Yes/No 按鈕會蓋掉對話文字，先收起來才看得到提示
        if (isWaitingForChoice)
        {
            if (yesButton != null) yesButton.gameObject.SetActive(false);
            if (noButton != null) noButton.gameObject.SetActive(false);
        }

        dialogueContent.gameObject.SetActive(true);
        dialogueContent.text = "【提示】\n" + text;

        // 使用提示是重要的學習診斷訊號，要記錄
        if (aiController != null) aiController.OnHintUsed(lastHintableSection);
    }

    /// <summary>還原被提示蓋掉的對話內容。</summary>
    public void CloseHint()
    {
        if (!isHintOpen) return;
        isHintOpen = false;

        // 閱讀提示的時間不算進該步驟的停留時間（熟悉度改由提示次數扣分）
        if (aiController != null) aiController.OnHintClosed(Time.time - hintOpenedAt);

        if (dialogueContent != null) dialogueContent.text = savedDialogueText;
        savedDialogueText = "";

        // 原本停在選擇題的話，把 Yes/No 按鈕放回來
        if (isWaitingForChoice) SetButtonsActive(true);
    }

    // ==================== 回到模式選單 ====================

    /// <summary>
    /// 由 ModeSelector 的「重選模式」按鈕呼叫：停掉目前流程、清空對話文本、
    /// 收起所有對話 UI，讓玩家回到 StartView 重新選腳本。
    /// </summary>
    public void ResetToStartView()
    {
        StopAllCoroutines();
        waitCoroutine = null;
        movementCoroutine = null;

        isReviewMode = false;
        ResetRuntimeState();

        currentIndex = 0;
        currentSection = "";
        lastHintableSection = "";
        lines.Clear();          // 清空已載入的對話文本

        if (dialogueContent != null) dialogueContent.text = "";
        if (dialoguePlane != null) dialoguePlane.SetActive(false);
        EnableInput(false);

        ResetHintState();
        if (hintButton != null) hintButton.SetActive(false);
    }

    /// <summary>
    /// 依名稱尋找場景中的物件，包含目前是隱藏的。
    /// 原本用 scene.name != null 排除 Prefab 資產是無效的
    /// （Prefab 資產的 scene.name 是空字串 "" 而不是 null，一樣會通過），
    /// 改用 scene.IsValid() 才真的只留下場景裡的物件。
    /// </summary>
    private GameObject FindSceneObjectByName(string targetName)
    {
        if (string.IsNullOrEmpty(targetName)) return null;

        // 先找啟用中的，最快
        GameObject found = GameObject.Find(targetName);
        if (found != null) return found;

        // 找不到再遍歷全部（含隱藏物件）
        Transform[] all = Resources.FindObjectsOfTypeAll<Transform>();
        int matches = 0;
        foreach (Transform t in all)
        {
            if (t.name != targetName) continue;
            if (!t.gameObject.scene.IsValid()) continue;   // 排除 Prefab 資產
            if (found == null) found = t.gameObject;
            matches++;
        }

        if (matches > 1)
            Debug.LogWarning($"<color=orange>[注意]</color> 場景中有 {matches} 個物件都叫 '{targetName}'，" +
                             $"檢查會用找到的第一個，結果可能不是你想要的那個。");

        return found;
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

        // 提示正開著就先別推進，否則 [IF] 重試迴圈會在 3 秒後把提示洗掉
        while (isHintOpen) yield return null;

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

    /// <summary>
    /// 增強版本的文字檢查方法，支援數值範圍驗證
    /// </summary>
    /// <param name="mode">
    /// EQ  = 必須等於期望值（溫度這類「要調到剛好」的設定，預設）
    /// LTE = 不可超過期望值（速率這類「有上限」的設定）
    /// GTE = 不可低於期望值
    /// </param>
    private bool CheckSpecificText(string objectName, string targetText, string mode = "EQ")
    {
        GameObject targetGO = FindSceneObjectByName(objectName);

        if (targetGO != null)
        {
            // 2. 獲取文字組件
            TextMeshProUGUI tmp = targetGO.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                string currentText = tmp.text.Trim();
                
                Debug.Log($"<color=cyan>[文字檢查]</color> 物件: '{objectName}' | 當前文字: '{currentText}' | 期望值: '{targetText}'");

                // ===== 數值檢查邏輯 =====
                // 嘗試從當前文字提取數值
                if (TryExtractNumericValue(currentText, out float currentValue))
                {
                    // 嘗試解析期望值為數值
                    if (float.TryParse(targetText, out float expected))
                    {
                        const float eps = 0.01f;   // 小數精度誤差容許
                        bool pass;
                        string symbol;

                        if (mode == "LTE")      { pass = currentValue <= expected + eps; symbol = "≤"; }
                        else if (mode == "GTE") { pass = currentValue >= expected - eps; symbol = "≥"; }
                        else                    { pass = Mathf.Abs(currentValue - expected) <= eps; symbol = "="; }

                        if (pass)
                        {
                            Debug.Log($"<color=green>[✓ 通過]</color> 數值檢查: {currentValue} {symbol} {expected}（模式 {mode}）");
                            return true;
                        }
                        else
                        {
                            Debug.LogWarning($"<color=red>[✗ 失敗]</color> 數值不符: 目前 {currentValue}，需要 {symbol} {expected}（模式 {mode}）");
                            return false;
                        }
                    }
                }

                // ===== 字符串精確檢查 =====
                // 如果不是數值比較，使用字符串包含檢查
                if (currentText.Contains(targetText))
                {
                    Debug.Log($"<color=green>[✓ 通過]</color> 文字包含檢查通過");
                    return true;
                }
                else
                {
                    Debug.LogWarning($"<color=red>[✗ 失敗]</color> 文字不包含期望內容");
                    return false;
                }
            }
            else
            {
                Debug.LogWarning($"<color=red>[錯誤]</color> 物件 '{objectName}' 沒有 TextMeshProUGUI 組件！");
                return false;
            }
        }
        else
        {
            Debug.LogWarning($"<color=red>[錯誤]</color> 找不到名為 '{objectName}' 的文字物件！");
            return false;
        }
    }

    /// <summary>
    /// 從文字中提取第一個數值
    /// 例如："33" → 33, "33.5°C" → 33.5, "0.25°C/hr" → 0.25
    /// </summary>
    private bool TryExtractNumericValue(string text, out float value)
    {
        value = 0f;
        
        if (string.IsNullOrEmpty(text))
            return false;

        // 移除特殊字元，只保留數字和小數點
        string cleaned = "";
        bool hasDecimalPoint = false;

        foreach (char c in text)
        {
            if (char.IsDigit(c))
            {
                cleaned += c;
            }
            else if (c == '.' && !hasDecimalPoint)
            {
                cleaned += c;
                hasDecimalPoint = true;
            }
            else if (c == '-' && cleaned.Length == 0) // 只在開頭允許負號
            {
                cleaned += c;
            }
        }

        // 嘗試解析為浮點數
        return float.TryParse(cleaned, out value);
    }
}
