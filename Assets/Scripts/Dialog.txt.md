::START
按“右鍵trigger”繼續 [w]
[talk] 菜鳥，這是你第一次實習吧？ [lr]
[talk] 別擔心，我會照顧你的 [w]
[NpcMove,2.45,0,-0.3] 跟我來 [w]
“病人情況確認”[w]


::BREATH_CHECK
[talk] 我們先觀察生命體徵，從呼吸開始 [w]
"呼吸頻率：15次/分鐘" [w]
[CHOICE:::HEART_START,::BREATH_WRONG] 正常嗎？
::BREATH_WRONG
[talk] 呼吸頻率 15 次/分在正常範圍內 (12-20次)。再想一下！ [lr]
::RETRY_BREATH
[talk] 我們重新確認一次。 [w]
JumpTo::BREATH_CHECK


::HEART_START
[talk] 現在我們來檢查一下心率 [lr]
「心率：70BPM」 [w]
[CHOICE:::VITALS_CHECK,::HEART_WRONG] 這樣正常嗎？
::HEART_WRONG
[talk] 成人心率 70BPM 是正常的 (60-100BPM)，請再確認。 [lr]
JumpTo::HEART_START


::VITALS_CHECK
[talk] 讓我們確認生命徵象。 [w]
“血壓 138/88 mmHg” [lr]
“血氧飽和度 96%” [lr]
“血糖 102 mg/dL” [lr]
「血脂顯示總膽固醇 188 mg/dL」 [w]
[CHOICE:::GCS_CHECK,::VITALS_WRONG] 這樣正常嗎？
::VITALS_WRONG
[talk] 雖然血壓偏高，但在老年患者的預期範圍內。請再考慮一下。 [lr]
JumpTo::VITALS_CHECK


::GCS_CHECK
[talk] 接下來，我們來確認格拉斯哥昏迷評分（GCS）。 [lr]
[talk] 她的分數是5分。 [lr]
[talk] 她需要治療…[w]
[talk] 現在，我們來檢查一下代謝指標。 [lr]
[talk] 電解質正常，腎功能顯示肌酸酐值為1.1 mg/dL。 [w]
[talk] 所有參數均在老年患者的預期範圍內。我們現在可以開始治療了，首先進行準備工作。 [lr]
[talk] 先連接體溫探頭。 [w]
[talk] 我們需要測量核心體溫。 [lr]
（我們可以將探頭連接到食道、膀胱或直腸。您會選擇哪個部位？）[w]


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
[talk] 接著選擇溫控傳遞墊，要根據患者體重選擇不同型號，目標是覆蓋百分之四十趴的身體表面積 [w]
" 請將地上的溫控傳遞墊拿起進行穿戴 "[w]
::Check
[IF:ITEM_ON_SOCKET:大腿端輸水管]
        [talk] 非常好，最後只需要接上輸水管就可以了 [w]
        JumpTo::Water_Supply_Pipe
[ELSE]
        [WAIT:3]地板上的藍色貼片為溫控傳遞墊，請放到病人身上進行穿戴[w]
        JumpTo::Check


::Water_Supply_Pipe
[talk]連接的時候記得連接的時候記得手握藍色管並對準雙孔洞，然後向前推進緊扣兩側卡榫 [lr]
[talk] 注意流速不能低於2L/min，還有管路不可以有曲折 [w]
" 請按下牆壁上的按鈕進行輸水管的連接 "[w]
[talk] 做得不錯，現在可以開始在儀器上設定療程了 [w]

::treatment


