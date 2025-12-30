using UnityEngine;

public class ReplaceActivateChildren : MonoBehaviour
{
    [Header("設定觸發標籤")]
    public string triggerTag = "toTrigger";

    [Header("要啟用的物件清單")]
    public GameObject[] objectsToActivate;

    [Header("偵測模式")]
    public bool useTrigger = true;

    private bool activated = false;

    // --- 物理碰撞 (Non-Trigger) ---
    void OnCollisionEnter(Collision collision)
    {
        if (useTrigger) return;
        // 這裡會顯示具體撞到的物件名稱
        Debug.Log($"<color=cyan>[物理碰撞偵測]</color> 撞到了物件: <b>{collision.gameObject.name}</b>，標籤為: <b>{collision.gameObject.tag}</b>");
        TryActivate(collision.gameObject);
    }

    // --- Trigger 觸發 (Is Trigger 勾選時) ---
    void OnTriggerEnter(Collider other)
    {
        if (!useTrigger) return;
        // 這裡會顯示進入 Trigger 範圍的物件名稱
        Debug.Log($"<color=cyan>[Trigger觸發偵測]</color> <b>{other.gameObject.name}</b> 進入了偵測範圍，其標籤為: <b>{other.gameObject.tag}</b>");
        TryActivate(other.gameObject);
    }

    private void TryActivate(GameObject other)
    {
        if (activated) return;

        // 1. 檢查 Tag
        if (!string.IsNullOrEmpty(triggerTag))
        {
            if (!other.CompareTag(triggerTag))
            {
                // 如果 Tag 不符，這行會告訴你原因
                Debug.LogWarning($"<color=orange>[Tag不符]</color> 碰到的物件是 {other.name}，但它的 Tag 是 {other.tag}，不是 {triggerTag}");
                return;
            }
        }

        activated = true;
        Debug.Log($"<color=green>[觸發成功]</color> 確認碰到目標物件: {other.name}，準備啟用清單物件。");

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
}