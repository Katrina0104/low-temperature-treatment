::START
按“右鍵trigger”繼續 [w]
[talk] 菜鳥，這是你第一次實習吧？ [lr]
[talk] 別擔心，我會照顧你的 [w]
[NpcMove,2.45,0,-0.3] 跟我來 [w]
“病人情況確認”[w]
「左邊牆面有患者情況」[lr]
[talk]先核對是否為患者[w]
"病人姓名張小名，生日62年7月12日"[w] 
::Check_Patient
[CHOICE:::Patient_START,::Patient_WRONG]
::Patient_START
確認為患者，接下來我們先觀察生命體徵[lr]
JumpTo::BREATH_CHECK
::Patient_WRONG
請再核對一次身份訊息[lr]
JumpTo::Check_Patient




::BREATH_CHECK
[talk] "體溫37.8℃" [w]
"脈搏52次/分"[w]
"呼吸頻率：15次/分鐘" [w]
"血壓178/88mmHg"[w]
是否可以進行治療？[lr]
[CHOICE:::GCS_CHECK,::BREATH_WRONG] 
::BREATH_WRONG
[talk] 生命體徵皆在正常範圍內。再想一下！ [lr]
JumpTo::BREATH_CHECK


::GCS_CHECK
[talk] 接下來，我們來確認格拉斯哥昏迷評分（GCS）是否小於等於12 [lr]
"張小名，請睜開眼睛"[w]
「雙眼緊閉沒有睜開」[lr]
[talk] GCS 5T，E1VTM4 [lr]
[talk] 她需要治療…[w]
[talk] 現在，我們來確認顱內壓(ICP)數值是否大於20mmHg [lr]
[talk] 觀察顱內壓(ICP)儀器 [w]
[talk] "ICP數值，顱內壓30mmHg" [lr]
[talk] 現在我們使用食道溫探頭核心體溫 [w]
（我們可以將探頭連接到食道、膀胱或直腸）[lr]


::TEMP_CHOICE
[talk] 注意事項！ [w]
「如果進入食道」 [lr]
請將探頭推進至食道中下三分之一處。此位置可準確測量核心體溫。 [w]
「如果進入膀胱」 [lr]
請使用與插入標準導尿管相同的無菌技術插入探頭。 [w]
「如果進入直腸」 [lr]
若患者為成人要插入至少6.5cm、新生兒至少要插入3cm、早產兒至少要插入2cm [w]
「患者目前鼓膜溫度為 36.8°C。」 [w]
[talk] 一切準備就緒準備開始低溫治療 [w]


::Temperature_control_transfer_pad
[talk] 接著選擇溫控傳遞墊，要根據患者體重選擇不同型號，目標是覆蓋百分之四十趴的身體表面積 [lr]
「病人65公斤，選擇對應M型號的貼片並貼上在病人身上」[w]
" 請將地上的溫控傳遞墊拿起進行穿戴 "[lr]
::Check_Patch
[IF:IS_ACTIVE:右胸貼片切分]
        JumpTo::Water_Supply_Pipe
[ELSE]
        [WAIT:3]地板上的藍色貼片為溫控傳遞墊，請放到病人身上進行穿戴[w]
        JumpTo::Check_Patch


::Water_Supply_Pipe
[talk] 非常好，最後只需要接上輸水管就可以了 [w]
[talk] 連接的時候記得連接的時候記得手握藍色管並對準雙孔洞，然後向前推進緊扣兩側卡榫 [lr]
[talk] 注意流速不能低於2L/min，還有管路不可以有曲折 [w]
" 請按下牆壁上的按鈕進行輸水管的連接 "[w]
::Check_Button
[IF:IS_ACTIVE:右邊胸口貼片管路部分(1)]
        [talk] 做得不錯，現在可以開始在儀器上設定療程了 [w]
        JumpTo::Treatment
[ELSE]
        [WAIT:3]牆壁上的按鈕為輸水管的連接方式，請按下繼續療程[w]
        JumpTo::Check_Button


::Treatment
[talk]幫我找到低物治療儀器，並開始降溫療程 [lr]
"調整降溫數值至33度" 請記得做儲存 [w]
::Check_Setup
[IF:TEXT_CHECK:降溫目標溫度:33]
    [talk] 溫度已達到 33.0 度，做得好。 [w]
    JumpTo::Next_Step
[ELSE]
    [talk] 溫度還沒對喔，目前顯示的不是 33.0 度 [w]
    JumpTo::Check_Setup
[ENDIF]
::Next_Step
[talk]很好，療程進行的這幾天要注意以下事項[w]
::Note5
[talk]血糖管理 (Blood Glucose Management)：[w]
[talk]低溫會減少胰島素分泌並降低胰島素敏感性[w]
[talk]可能導致高血糖[lr] 
[talk]或許需要使用胰島素輸注[lr]
[talk]護理規約中通常包含胰島素規約。[w]
::Note6
[talk]凝血功能與出血風險 (Coagulation and Bleeding Risk)：[w]
[talk]低溫會損害凝血功能和血小板功能[lr]
[talk]體溫低於 35°C 可能會導致輕度血小板功能受損和血小板計數下降[lr]
[talk]體溫低於 33°C 影響更大。體溫低於 30°C 時出血風險顯著增加[w]
[talk]對於有凝血功能障礙或出血風險的病患，需要密切監測。[lr]
::Note7
[talk]免疫系統與感染風險 (Immune System and Infection Risk)：[w]
[talk]低溫可能導致免疫系統功能下降，增加感染風險，特別是呼吸器相關性肺炎 (VAP)[lr] 
[talk]預防性的抗生素療程可能會有益[w]
::Note8
[talk]心律不整 (Arrhythmias)：[w]
[talk]在 TTM 期間相對常見。心跳過緩 (Bradycardia) 是最常見的[lr]
[talk]通常只有在影響病患的血流動力學時才需要治療。也可能出現心房顫動 (Af) 或較少見的心室頻脈 (VT) 。[lr]
::Note9
[talk]意識狀態 (Consciousness Level)：[w]
[talk]如果病患在療程結束前恢復意識且不再昏迷，可能需要結束療程。不應對沒有昏迷的病患進行低溫治療。[lr]
::Note10
[talk]電解質與液體平衡 (Electrolytes and Fluid Balance)：[w]
[talk]低溫可能導致輕度的利尿和電解質流失，例如低血鉀、低血鎂和低血鈣[lr]
[talk]這可能導致低血容量[w]
[talk]因此，需要抽血檢查並根據需要補充電解質[lr]
[talk]特別注意，復溫階段會逆轉鉀離子流動，可能導致高血鉀，因此在復溫前 4 小時，特別是腎功能不全的病患，應暫停鉀離子補充。[lr]







::Day_1
[talk]療程尚未結束，請多關注患者情況[w]
"請完成每日例行行程"[lr]
::Event1
不需要重新接輸水管嗎？[lr]
[CHOICE:::Event1_Yes,::Event1_No] 
::Event1_No
[talk] 非常好~重新接輸水管不是每日例行公事 [lr]
JumpTo::Event2
::Event1_Yes
[talk] 除非有連接不良！否則不需要 [lr]
JumpTo::Event1
::Event2
需要塗抹乳液嗎?[lr]
[CHOICE:::Event2_Yes,::Event2_No] 
::Event2_Yes
[talk] 是的!特別是使用外部冷卻貼片時，由於表面微血管血流減少，病患皮膚破損的風險增加[w]
需要頻繁評估貼片下的皮膚，保護骨突處，並使用適當的乳液，定期翻身以降低風險。 [lr]
JumpTo::Event3
::Event2_No
[talk] 使用外部冷卻貼片時，由於表面微血管血流減少，病患皮膚破損的風險增加。請再思考一下! [w]
JumpTo::Event2
::Event3
[WAIT:5]可以在照顧患者時，觀看治療手冊![w]
[ROLL_EQUIP]
[ALARM_OFF]
[WAIT:9]可以在照顧患者時，觀看治療手冊![w]
[ROLL_SHIVER]
[ALARM_OFF]
[WAIT:7]可以在照顧患者時，觀看治療手冊![w]
[ROLL_BP]
[talk] 恭喜今日任務完成。 [w]








::Day2
[INC_DAY]
[talk]來測量今日患者體溫[w]
[talk]患者目前鼓膜溫度為 37.3°C[w]
::Event4
請問是否正常?[lr]
[CHOICE:::Event4_Yes,::Event4_No]
::Event4_No
[talk] 鼓膜溫度36.5℃至37.8皆為正常 [w]
JumpTo::Event4
::Event4_Yes
"請完成每日例行行程"[w]
::Event5
需要塗抹乳液嗎?[lr]
[CHOICE:::Event5_Yes,::Event5_No] 
::Event5_Yes
[talk] 是的!特別是使用外部冷卻貼片時，由於表面微血管血流減少，病患皮膚破損的風險增加。需要頻繁評估貼片下的皮膚，保護骨突處，並使用適當的乳液，定期翻身以降低風險。 [lr]
JumpTo::Event6
::Event5_No
[talk] 使用外部冷卻貼片時，由於表面微血管血流減少，病患皮膚破損的風險增加。請再思考一下! [w]
JumpTo::Event5
::Event6
[WAIT:8]可以在照顧患者時，觀看治療手冊![w]
[ROLL_SHIVER]
[ALARM_OFF]
[WAIT:5]可以在照顧患者時，觀看治療手冊![w]
[ROLL_BP]
[ALARM_OFF]
[WAIT:7]可以在照顧患者時，觀看治療手冊![w]
[ROLL_EQUIP]
[talk] 恭喜今日任務完成。 [w]







::Day3
[INC_DAY]
[talk]來測量今日患者體溫[w]
[talk]患者目前鼓膜溫度為 36.5°C[w]
::Event7
[CHOICE:::Event7_Yes,::Event7_No] 請問是否正常?
::Event7_No
[talk] 鼓膜溫度36.5℃至37.8皆為正常 [w]
JumpTo::Event7
::Event7_Yes
"請完成每日例行行程"[lr]
::Event8
[CHOICE:::Event8_Yes,::Event8_No] 需要塗抹乳液嗎?
::Event8_Yes
[talk] 是的!特別是使用外部冷卻貼片時，由於表面微血管血流減少，病患皮膚破損的風險增加。需要頻繁評估貼片下的皮膚，保護骨突處，並使用適當的乳液，定期翻身以降低風險。 [lr]
JumpTo::Event9
::Event8_No
[talk] 使用外部冷卻貼片時，由於表面微血管血流減少，病患皮膚破損的風險增加。請再思考一下! [w]
JumpTo::Event8
::Event9
[WAIT:8]可以在照顧患者時，觀看治療手冊![lr]
[ROLL_SHIVER]
[ALARM_OFF]
[WAIT:5]可以在照顧患者時，觀看治療手冊![lr]
[ROLL_BP]
[ALARM_OFF]
[WAIT:7]可以在照顧患者時，觀看治療手冊![lr]
[ROLL_EQUIP]
[talk] 恭喜今日任務完成。 [lr]



::End
[talk]你表現的很棒！現在療程結束需要移除輸水管，首先停止機器的治療程序[w]
[talk]按下螢幕上的stop按鈕就可以了[lr]

[talk]再來把傳遞墊排空，按下螢幕上的排空貼片按鈕就可以了[w]

[talk]接著斷開體溫探頭和食道溫探頭[w]

[talk]然後要把輸水管從跟機器連接的端頭移除，首先抓住連接處的翼狀結構 (wings)[w]

[talk]然後捏住 (pinch)，推入 (push)，接著再拉出 (pull) 以移除凝膠貼片」，記得需要對所有連接的貼片重複這個過程。[w]

[talk]最後移除患者身上的貼片就完成了。完成了。要小心地移除避免撕裂患者的皮膚[w]






::EVENT_EQUIPMENT
[talk] 【警告】偵測到貼片脫落或設備缺水，請檢查連線！ [lr]
請先使用"empty pads" (清空貼片) 按鈕來清空貼片內的液體[w]
現在請「斷開貼片與機器連接」[w]
::Check_EQUIPMENT
[IF:IS_ACTIVE:機器端書水管路]
        [talk] 事件已解決 [w]
        "如果機器顯示缺水，需要使用 "fill reservoir" (填充水箱) 功能補充無菌水" [w]
        現在請「接回貼片與機器連接」[w]
        ::Check_EQUIPMENT_back
        [IF:IS_ACTIVE:右邊胸口貼片管路部分(1)]
                [RETURN]
        [ELSE]
                [WAIT:3]牆壁上的按鈕為輸水管的連接方式，請按下繼續療程[w]
                JumpTo::Check_EQUIPMENT_back
[ELSE]
        [WAIT:3]牆壁上的按鈕為切斷輸水管的連接方式，請按下進行檢查[w]
        JumpTo::Check_EQUIPMENT





::EVENT_SHIVERING
[talk] 【警告】患者身體出現颤抖的情況 [lr]
"可以採取策略處理，包括：對臉部、手、腳進行皮膚溫暖（用溫毯包裹）" [lr]
"給予硫酸鎂（藥物）以提高顫抖閾值；使用止痛藥和鎮靜劑（推注或持續輸注）" [w]
"如果上述方法無效，可能需要使用麻醉藥物" [lr]
::Check_MgSO4
是否給予硫酸鎂?[lr]
[CHOICE:::SHIVERING_Yes,::SHIVERING_No] 
::SHIVERING_Yes
[talk]已選擇提高顫抖閾值 [lr]
[RETURN]
::SHIVERING_No
是否給予止痛藥?[lr]
[CHOICE:::SHIVERING1_Yes,::SHIVERING1_No]
::SHIVERING1_Yes
[talk] 已選擇止痛藥穩定患者 [lr]
[RETURN]
::SHIVERING1_No
[talk] 請在兩種方式中進行選擇 [w]
JumpTo::Check_MgSO4





::EVENT_BP_UNSTABLE
[talk] 【警告】偵測到血壓不穩的狀況！ [lr]
請調整復溫數值[w]
請將復溫溫度選為37度[lr]
::Temperature
[IF:TEXT_CHECK:復溫目標溫度:37]
    [talk] 溫度已設置為 37.0 度，做得好。 [w]
    JumpTo::Speed
[ELSE]
    [talk] 溫度還沒對喔，目前顯示的不是 37.0 度 [w]
    JumpTo::Temperature
[ENDIF]
::Speed
再將復溫速率選為0.25[w]
記得調整正下方時間[w]
"請儲存所有調整"[lr]
::Speed_Check
[IF:TEXT_CHECK:復溫時間:0:25]
    [talk] 復溫速率已設置為 0.25，做得好。 [w]
    [RETURN]
[ELSE]
    [talk] 復溫速率還沒對喔，目前顯示的不是 0.25 [w]
    JumpTo::Speed_Check
[ENDIF]


