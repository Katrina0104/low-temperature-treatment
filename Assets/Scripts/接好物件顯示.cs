using System;
using UnityEngine;

public class ReplaceCompletePipe : MonoBehaviour
{
    [Header("要啟用的物件清單")]
    public GameObject[] objectsToActivate;

    [Header("要關閉的物件清單")]
    public GameObject[] objectsToUnActivate;

    [Header("偵測模式")]
    public bool useTrigger = true;

    private bool activated = false;

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
        if (activated) return;

        activated = true;
        //Debug.Log($"<color=green>[觸發成功]</color> 確認碰到目標物件: {other.name}，準備啟用清單物件。");

        if (objectsToActivate != null)
        {
            foreach (var obj in objectsToActivate)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                    Debug.Log($"<color=white> >> 執行指令: {obj.name}.SetActive(true)</color>");
                }
            }
        }
    }

    public void TryUnActivate(GameObject other)
    {
        //if (activated) return;

        activated = false;
        //Debug.Log($"<color=green>[觸發成功]</color> 確認碰到目標物件: {other.name}，準備啟用清單物件。");

        if (objectsToUnActivate != null)
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
    }

}

