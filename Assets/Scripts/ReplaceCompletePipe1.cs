using System;
using Unity.VisualScripting.Antlr3.Runtime;
using UnityEngine;

public class ReplaceObjects : MonoBehaviour
{
    [Header("要啟用的物件清單")]
    public GameObject[] objectsToActivate;

    [Header("要關閉的物件清單")]
    public GameObject[] objectsToUnActivate;

    [Header("偵測模式")]
    public bool useTrigger = true;

    public bool activated = true;

    // --- Trigger 觸發 (Is Trigger 勾選時) ---
    void OnTriggerEnter(Collider other)
    {
        if (!useTrigger) return;
        // 這裡會顯示進入 Trigger 範圍的物件名稱
        Debug.Log($"<color=cyan>[Trigger觸發偵測]</color> <b>{other.gameObject.name}</b> 進入了偵測範圍");
        TryActivate(other.gameObject);
    }


    public void TryActivate(GameObject other)
    {
        if (activated)
        {
            foreach (var obj in objectsToActivate)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                    Debug.Log($"<color=white> >> 執行指令: {obj.name}.SetActive(true)</color>");
                }
            }

            foreach (var obj in objectsToUnActivate)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                    Debug.Log($"<color=white> >> 執行指令: {obj.name}.SetActive(false)</color>");
                }
            }
        }
        else
        {
            foreach (var obj in objectsToActivate)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                    Debug.Log($"<color=white> >> 執行指令: {obj.name}.SetActive(false)</color>");
                }
            }

            foreach (var obj in objectsToUnActivate)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                    Debug.Log($"<color=white> >> 執行指令: {obj.name}.SetActive(true)</color>");
                }
            }
        }

        activated = !activated;
        //Debug.Log($"<color=green>[觸發成功]</color> 確認碰到目標物件: {other.name}，準備啟用清單物件。");

        
            /*foreach (var obj in objectsToActivate)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                    Debug.Log($"<color=white> >> 執行指令: {obj.name}.SetActive(true)</color>");
                }
            }*/
        
    }

    public void TryUnActivate(GameObject other)
    {
        if (activated)
        {
            foreach (var obj in objectsToUnActivate)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                    Debug.Log($"<color=white> >> 執行指令: {obj.name}.SetActive(false)</color>");
                }
            }
        }
        else
        {
            foreach (var obj in objectsToUnActivate)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                    Debug.Log($"<color=white> >> 執行指令: {obj.name}.SetActive(true)</color>");
                }
            }
        }

        //activated = !activated;
    }

}

