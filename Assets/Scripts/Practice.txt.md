::START
按“右鍵A”開始測驗 [w]
[talk] 這次換你自己來，我只在旁邊看 [lr]
[talk] 想不起來的話，可以按提示按鈕 [w]
[NpcMove,2.45,0,-0.3] 跟我來 [w]
“病人情況確認”[w]
「左邊牆面有患者情況」[lr]
[talk]先核對是否為患者[w]
"病人姓名張小名，生日62年7月12日"[w] 
::Check_Patient
[CHOICE:::Patient_START,::Patient_WRONG]
::Patient_START
確認為患者，接下來我們先觀察生命徵象[lr]
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
[talk] 判讀不正確，再想一下！ [lr]
JumpTo::BREATH_CHECK


::GCS_CHECK
[talk] 接下來，我們來確認格拉斯哥昏迷評分（GCS）是否小於等於12 [lr]
"張小名，請睜開眼睛"[w]
「雙眼緊閉沒有睜開」[lr]
[talk] GCS 5T，E1VTM4 [lr]
[talk] 現在，我們來確認顱內壓(ICP)數值是否大於20mmHg [lr]
[talk] 觀察顱內壓(ICP)儀器 [w]
[talk] "ICP數值，顱內壓30mmHg" [lr]
[talk] 現在我們使用食道溫探頭核心體溫 [w]
（我們可以將探頭連接到食道、膀胱或直腸）[lr]


::TEMP_CHOICE
[talk] 請依正確的方式置放探頭 [w]
「患者目前鼓膜溫度為 36.8°C。」 [w]
[talk] 一切準備就緒準備開始低溫治療 [w]


::Temperature_control_transfer_pad
[talk] 接著選擇溫控傳遞墊，要根據患者體重選擇適當型號 [lr]
「病人65公斤，選擇對應型號的貼片並貼上在病人身上」[w]
" 請將地上的溫控傳遞墊拿起進行穿戴 "[lr]
::Check_Patch
[IF:IS_ACTIVE:右胸貼片切分]
        JumpTo::Water_Supply_Pipe
[ELSE]
        [WAIT:3]地板上的藍色貼片為溫控傳遞墊，請放到病人身上進行穿戴[w]
        JumpTo::Check_Patch


::Water_Supply_Pipe
[talk] 非常好，最後只需要接上輸水管就可以了 [w]
[talk] 請依正確的手法連接，並注意流速與管路狀態 [lr]
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
"請調整降溫數值至正確的目標溫度" [w]
::Check_Setup
[IF:TEXT_CHECK:Cooling_temperature:33:EQ]
    [talk] 溫度設定正確，做得好。 [w]
    JumpTo::Next_Step
[ELSE]
    [WAIT:2]溫度還沒對喔，請重新調整[w]
    JumpTo::Check_Setup
[ENDIF]
::Next_Step
[talk]很好，接下來確認你對療程注意事項的掌握[w]
::Note5
低溫治療期間，病患較可能出現高血糖而需要胰島素輸注嗎？[lr]
[CHOICE:::Note5_Yes,::Note5_No]
::Note5_Yes
[talk] 正確 [lr]
JumpTo::Note6
::Note5_No
[talk] 不正確，請再想一下 [lr]
JumpTo::Note5
::Note6
體溫低於 30°C 時，出血風險會顯著增加嗎？[lr]
[CHOICE:::Note6_Yes,::Note6_No]
::Note6_Yes
[talk] 正確 [lr]
JumpTo::Note7
::Note6_No
[talk] 不正確，請再想一下 [lr]
JumpTo::Note6
::Note7
低溫治療會降低病患的感染風險嗎？[lr]
[CHOICE:::Note7_Yes,::Note7_No]
::Note7_No
[talk] 正確 [lr]
JumpTo::Note8
::Note7_Yes
[talk] 不正確，請再想一下 [lr]
JumpTo::Note7
::Note8
心跳過緩若尚未影響病患的血流動力學，需要立即治療嗎？[lr]
[CHOICE:::Note8_Yes,::Note8_No]
::Note8_No
[talk] 正確 [lr]
JumpTo::Note9
::Note8_Yes
[talk] 不正確，請再想一下 [lr]
JumpTo::Note8
::Note9
若病患在療程結束前恢復意識、不再昏迷，可能需要結束療程嗎？[lr]
[CHOICE:::Note9_Yes,::Note9_No]
::Note9_Yes
[talk] 正確 [lr]
JumpTo::Note10
::Note9_No
[talk] 不正確，請再想一下 [lr]
JumpTo::Note9
::Note10
復溫前 4 小時應暫停鉀離子補充嗎？[lr]
[CHOICE:::Note10_Yes,::Note10_No]
::Note10_Yes
[talk] 正確 [lr]
JumpTo::Day_1
::Note10_No
[talk] 不正確，請再想一下 [lr]
JumpTo::Note10








::Day_1
[talk]療程尚未結束，請多關注患者情況[w]
"請完成每日例行行程"[lr]
::Event1
需要重新接輸水管嗎？[lr]
[CHOICE:::Event1_Yes,::Event1_No] 
::Event1_No
[talk] 非常好~重新接輸水管不是每日例行公事 [lr]
JumpTo::Event2
::Event1_Yes
[talk] 不正確，請再想一下 [lr]
JumpTo::Event1
::Event2
需要塗抹乳液嗎?[lr]
[CHOICE:::Event2_Yes,::Event2_No] 
::Event2_Yes
[talk] 是的!特別是使用外部冷卻貼片時，由於表面微血管血流減少，病患皮膚破損的風險增加[w]
需要頻繁評估貼片下的皮膚，保護骨突處，並使用適當的乳液，定期翻身以降低風險。 [lr]
JumpTo::Event3
::Event2_No
[talk] 不正確，請再思考一下! [w]
JumpTo::Event2
::Event3
[WAIT:5]可以在照顧患者時，觀看治療手冊![w]
[ROLL_EQUIP]
[ALARM_OFF]
[WAIT:3]可以在照顧患者時，觀看治療手冊![w]
[ROLL_SHIVER]
[ALARM_OFF]
[WAIT:7]可以在照顧患者時，觀看治療手冊![w]
[ROLL_BP]
[ALARM_OFF]
[talk] 恭喜今日任務完成。 [w]








::Day2
[INC_DAY]
[talk]來測量今日患者體溫[w]
[talk]患者目前鼓膜溫度為 37.3°C[w]
::Event4
請問是否正常?[lr]
[CHOICE:::Event4_Yes,::Event4_No]
::Event4_No
[talk] 判讀不正確，請再想一下 [w]
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
[talk] 不正確，請再思考一下! [w]
JumpTo::Event5
::Event6
[WAIT:3]可以在照顧患者時，觀看治療手冊![w]
[ROLL_SHIVER]
[ALARM_OFF]
[WAIT:5]可以在照顧患者時，觀看治療手冊![w]
[ROLL_BP]
[ALARM_OFF]
[WAIT:7]可以在照顧患者時，觀看治療手冊![w]
[ROLL_EQUIP]
[ALARM_OFF]
[talk] 恭喜今日任務完成。 [w]







::Day3
[INC_DAY]
[talk]來測量今日患者體溫[w]
[talk]患者目前鼓膜溫度為 36.5°C[w]
::Event7
請問是否正常?
[CHOICE:::Event7_Yes,::Event7_No] 
::Event7_No
[talk] 判讀不正確，請再想一下 [w]
JumpTo::Event7
::Event7_Yes
"請完成每日例行行程"[lr]
::Event8
需要塗抹乳液嗎?
[CHOICE:::Event8_Yes,::Event8_No]
::Event8_Yes
[talk] 是的!特別是使用外部冷卻貼片時，由於表面微血管血流減少，病患皮膚破損的風險增加。需要頻繁評估貼片下的皮膚，保護骨突處，並使用適當的乳液，定期翻身以降低風險。 [lr]
JumpTo::Event9
::Event8_No
[talk] 不正確，請再思考一下! [w]
JumpTo::Event8
::Event9
[WAIT:4]可以在照顧患者時，觀看治療手冊![lr]
[ROLL_SHIVER]
[ALARM_OFF]
[WAIT:5]可以在照顧患者時，觀看治療手冊![lr]
[ROLL_BP]
[ALARM_OFF]
[WAIT:6]可以在照顧患者時，觀看治療手冊![lr]
[ROLL_EQUIP]
[ALARM_OFF]
[talk] 恭喜今日任務完成。 [lr]



::End
[talk]你表現的很棒！現在療程結束需要移除輸水管，首先停止機器的治療程序[w]
[talk]按下螢幕上的stop按鈕就可以了[lr]
::Check_Button_stop
[IF:BUTTON_PRESSED:stop]
    [talk] 按鈕已確認按下，請繼續！ [w]
    [talk] 再來把傳遞墊排空，按下螢幕上的排空貼片按鈕就可以了 [w]
    JumpTo::Check_Button_empty
[ELSE]
    請按下螢幕上的按鈕繼續 [w]
    JumpTo::Check_Button_stop
::Check_Button_empty
[IF:BUTTON_PRESSED:EmptyButton]
    [talk] 按鈕已確認按下，請繼續！ [w]
    JumpTo::Continue
[ELSE]
    [WAIT:3] 請按下螢幕上的按鈕繼續 [w]
    JumpTo::Check_Button_empty
::Continue
[talk]接著我們斷開體溫探頭和食道溫探頭[lr]
[talk]然後依正確的手法把輸水管從機器端移除，記得對所有連接的貼片重複這個過程[lr]
[talk]請按下牆上的按鈕[w]
::Check_pipeline_end
[IF:IS_ACTIVE:機器端書水管路]
        JumpTo::Remove_patch
[ELSE]
        [WAIT:3]牆壁上的按鈕為切斷輸水管的連接方式，請按下進行檢查[w]
        JumpTo::Check_pipeline_end

::Remove_patch
[talk]最後移除患者身上的貼片就完成了![w]
[CHOICE:::Remove_YES,::Remove_No]
::Remove_YES
[talk]測驗結束，辛苦了![lr]
[REPORT]
[END]
::Remove_No
請再確認一次[lr]
JumpTo::Remove_patch








::EVENT_EQUIPMENT
[talk] 【警告】偵測到貼片脫落或設備缺水，請檢查連線！ [lr]
請依正確順序排除此狀況[w]
::Check_Empty
[IF:BUTTON_PRESSED:EmptyButton]
    現在請「斷開貼片與機器連接」[w]
    JumpTo::Check_EQUIPMENT
[ELSE]
    [WAIT:3]請先按下螢幕上傳遞墊清空按鈕，清空貼片[w]
    JumpTo::Check_Empty
[ENDIF]

::Check_EQUIPMENT
[IF:IS_ACTIVE:機器端書水管路]
    JumpTo::EQUIPMENT_Connected
[ELSE]
    [WAIT:3]牆壁上的按鈕為切斷輸水管的連接方式，請按下進行檢查[w]
    JumpTo::Check_EQUIPMENT
[ENDIF]

::EQUIPMENT_Connected
[talk] 事件已解決 [w]
現在請「接回貼片與機器連接」[w]
::Check_EQUIPMENT_back
[IF:IS_ACTIVE:機器端書水管路(1)]
    [RETURN]
[ELSE]
    [WAIT:3]牆壁上的按鈕為輸水管的連接方式，請按下繼續療程[w]
    JumpTo::Check_EQUIPMENT_back
[ENDIF]





::EVENT_SHIVERING
[talk] 【警告】患者身體出現颤抖的情況 [lr]
請採取適當的處置[w]
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
請將復溫溫度調整為正確的數值[lr]
::Temperature
[IF:TEXT_CHECK:Reheating_temperature:37:EQ]
    [talk] 溫度設置正確，做得好。 [w]
    JumpTo::Speed
[ELSE]
    [WAIT:2]溫度還沒對喔，請重新調整[w]
    JumpTo::Temperature
[ENDIF]
::Speed
再將復溫速率調慢至規定的上限以下[w]
記得調整正下方時間[w]
"請儲存所有調整"[lr]
::Speed_Check
[IF:TEXT_CHECK:reheat_rate:0.25:LTE]
    [talk] 復溫速率已設置好，做得好。 [w]
    [RETURN]
[ELSE]
    [WAIT:2]復溫速率還沒對喔，請重新調整[w]
    JumpTo::Speed_Check
[ENDIF]
