// using UnityEngine;

// public class WorkflowCheck : MonoBehaviour
// {
//     void Start()
//     {
//         // ===== START → 病患核對 =====
//         RegisterTransition("::START", "::Check_Patient", 1.0f);
//         RegisterTransition("::Check_Patient", "::Patient_START", 0.9f);
//         RegisterTransition("::Check_Patient", "::Patient_WRONG", 0.1f);
//         RegisterTransition("::Patient_WRONG", "::Check_Patient", 0.1f); // 迴圈重試
//         RegisterTransition("::Patient_START", "::BREATH_CHECK", 1.0f);

//         // ===== 生命徵象確認 =====
//         RegisterTransition("::BREATH_CHECK", "::GCS_CHECK", 0.9f);
//         RegisterTransition("::BREATH_CHECK", "::BREATH_WRONG", 0.1f);
//         RegisterTransition("::BREATH_WRONG", "::BREATH_CHECK", 0.1f);
//         RegisterTransition("::GCS_CHECK", "::TEMP_CHOICE", 1.0f);
//         RegisterTransition("::TEMP_CHOICE", "::Temperature_control_transfer_pad", 1.0f);

//         // ===== 溫控傳遞墊穿戴 =====
//         RegisterTransition("::Temperature_control_transfer_pad", "::Check_Patch", 1.0f);
//         RegisterTransition("::Check_Patch", "::Water_Supply_Pipe", 0.9f);   // 貼片已穿戴
//         RegisterTransition("::Check_Patch", "::Check_Patch", 0.1f);         // 尚未穿戴，原地等待

//         // ===== 輸水管連接 =====
//         RegisterTransition("::Water_Supply_Pipe", "::Check_Button", 1.0f);
//         RegisterTransition("::Check_Button", "::Treatment", 0.9f);
//         RegisterTransition("::Check_Button", "::Check_Button", 0.1f);

//         // ===== 療程設定（降溫33度） =====
//         RegisterTransition("::Treatment", "::Check_Setup", 1.0f);
//         RegisterTransition("::Check_Setup", "::Next_Step", 0.9f);
//         RegisterTransition("::Check_Setup", "::Check_Setup", 0.1f);

//         // ===== 療程注意事項（純線性） =====
//         RegisterTransition("::Next_Step", "::Note5", 1.0f);
//         RegisterTransition("::Note5", "::Note6", 1.0f);
//         RegisterTransition("::Note6", "::Note7", 1.0f);
//         RegisterTransition("::Note7", "::Note8", 1.0f);
//         RegisterTransition("::Note8", "::Note9", 1.0f);
//         RegisterTransition("::Note9", "::Note10", 1.0f);
//         RegisterTransition("::Note10", "::Day_1", 1.0f);

//         // ===== Day 1 =====
//         RegisterTransition("::Day_1", "::Event1", 1.0f);
//         RegisterTransition("::Event1", "::Event1_No", 0.9f);   // 不需重接輸水管才是正解
//         RegisterTransition("::Event1", "::Event1_Yes", 0.1f);
//         RegisterTransition("::Event1_Yes", "::Event1", 0.1f);
//         RegisterTransition("::Event1_No", "::Event2", 0.9f);

//         RegisterTransition("::Event2", "::Event2_Yes", 0.9f);  // 需塗抹乳液才是正解
//         RegisterTransition("::Event2", "::Event2_No", 0.1f);
//         RegisterTransition("::Event2_No", "::Event2", 0.1f);
//         RegisterTransition("::Event2_Yes", "::Event3", 0.9f);

//         // Event3：三個 ROLL_* 隨機事件，走完後接 Day2
//         RegisterTransition("::Event3", "::EVENT_EQUIPMENT", 0.5f);  // ROLL_EQUIP 觸發時
//         RegisterTransition("::Event3", "::EVENT_SHIVERING", 0.5f);  // ROLL_SHIVER 觸發時
//         RegisterTransition("::Event3", "::EVENT_BP_UNSTABLE", 0.5f);// ROLL_BP 觸發時
//         RegisterTransition("::Event3", "::Day2", 0.9f);             // 全部通過後接下一天

//         // ===== Day 2 =====
//         RegisterTransition("::Day2", "::Event4", 1.0f);
//         RegisterTransition("::Event4", "::Event4_Yes", 0.9f);  // 鼓膜溫度正常
//         RegisterTransition("::Event4", "::Event4_No", 0.1f);
//         RegisterTransition("::Event4_No", "::Event4", 0.1f);
//         RegisterTransition("::Event4_Yes", "::Event5", 0.9f);

//         RegisterTransition("::Event5", "::Event5_Yes", 0.9f);
//         RegisterTransition("::Event5", "::Event5_No", 0.1f);
//         RegisterTransition("::Event5_No", "::Event5", 0.1f);
//         RegisterTransition("::Event5_Yes", "::Event6", 0.9f);

//         RegisterTransition("::Event6", "::EVENT_EQUIPMENT", 0.5f);
//         RegisterTransition("::Event6", "::EVENT_SHIVERING", 0.5f);
//         RegisterTransition("::Event6", "::EVENT_BP_UNSTABLE", 0.5f);
//         RegisterTransition("::Event6", "::Day3", 0.9f);

//         // ===== Day 3 =====
//         RegisterTransition("::Day3", "::Event7", 1.0f);
//         RegisterTransition("::Event7", "::Event7_Yes", 0.9f);
//         RegisterTransition("::Event7", "::Event7_No", 0.1f);
//         RegisterTransition("::Event7_No", "::Event7", 0.1f);
//         RegisterTransition("::Event7_Yes", "::Event8", 0.9f);

//         RegisterTransition("::Event8", "::Event8_Yes", 0.9f);
//         RegisterTransition("::Event8", "::Event8_No", 0.1f);
//         RegisterTransition("::Event8_No", "::Event8", 0.1f);
//         RegisterTransition("::Event8_Yes", "::Event9", 0.9f);

//         RegisterTransition("::Event9", "::EVENT_EQUIPMENT", 0.5f);
//         RegisterTransition("::Event9", "::EVENT_SHIVERING", 0.5f);
//         RegisterTransition("::Event9", "::EVENT_BP_UNSTABLE", 0.5f);
//         RegisterTransition("::Event9", "::End", 0.9f);

//         // ===== 結束流程（拆管、收尾） =====
//         RegisterTransition("::End", "::Check_Button_stop", 1.0f);
//         RegisterTransition("::Check_Button_stop", "::Check_Button_empty", 0.9f); // stop 已按下
//         RegisterTransition("::Check_Button_stop", "::Check_Button_stop", 0.1f);
//         RegisterTransition("::Check_Button_empty", "::Continue", 0.9f);         // 排空鍵已按下
//         RegisterTransition("::Check_Button_empty", "::Check_Button_empty", 0.1f);
//         RegisterTransition("::Continue", "::Check_pipeline_end", 1.0f);
//         RegisterTransition("::Check_pipeline_end", "::Remove_patch", 0.9f);     // 機台端管路已切斷
//         RegisterTransition("::Check_pipeline_end", "::Check_pipeline_end", 0.1f);
//         RegisterTransition("::Remove_patch", "::Remove_YES", 0.9f);            // 確認移除
//         RegisterTransition("::Remove_patch", "::Remove_No", 0.1f);
//         RegisterTransition("::Remove_No", "::Remove_patch", 0.1f);
//         RegisterTransition("::Remove_YES", "[END]", 1.0f);                     // 終止狀態

//         // ===== 支線事件：設備缺水/脫落 =====
//         RegisterTransition("::EVENT_EQUIPMENT", "::Check_Empty", 1.0f);
//         RegisterTransition("::Check_Empty", "::Check_EQUIPMENT", 0.9f);        // Empty鍵已按
//         RegisterTransition("::Check_Empty", "::Check_Empty", 0.1f);
//         RegisterTransition("::Check_EQUIPMENT", "::EQUIPMENT_Connected", 0.9f);// 已斷開機台端
//         RegisterTransition("::Check_EQUIPMENT", "::Check_EQUIPMENT", 0.1f);
//         RegisterTransition("::EQUIPMENT_Connected", "::Check_EQUIPMENT_back", 1.0f);
//         RegisterTransition("::Check_EQUIPMENT_back", "[RETURN]", 0.9f);        // 已接回，回到主流程
//         RegisterTransition("::Check_EQUIPMENT_back", "::Check_EQUIPMENT_back", 0.1f);

//         // ===== 支線事件：顫抖 =====
//         RegisterTransition("::EVENT_SHIVERING", "::Check_MgSO4", 1.0f);
//         RegisterTransition("::Check_MgSO4", "::SHIVERING_Yes", 0.9f);   // 給予硫酸鎂
//         RegisterTransition("::Check_MgSO4", "::SHIVERING_No", 0.9f);    // 兩個都是合理臨床選項
//         RegisterTransition("::SHIVERING_Yes", "[RETURN]", 0.9f);
//         RegisterTransition("::SHIVERING_No", "::SHIVERING1_Yes", 0.9f); // 改給止痛藥
//         RegisterTransition("::SHIVERING_No", "::SHIVERING1_No", 0.1f);
//         RegisterTransition("::SHIVERING1_Yes", "[RETURN]", 0.9f);
//         RegisterTransition("::SHIVERING1_No", "::Check_MgSO4", 0.1f);

//         // ===== 支線事件：血壓不穩（復溫調整） =====
//         RegisterTransition("::EVENT_BP_UNSTABLE", "::Temperature", 1.0f);
//         RegisterTransition("::Temperature", "::Speed", 0.9f);       // 溫度設37度正確
//         RegisterTransition("::Temperature", "::Temperature", 0.1f);
//         RegisterTransition("::Speed", "::Speed_Check", 1.0f);
//         RegisterTransition("::Speed_Check", "[RETURN]", 0.9f);      // 速率設0.25°C/hr正確
//         RegisterTransition("::Speed_Check", "::Speed_Check", 0.1f);
//     }
// }