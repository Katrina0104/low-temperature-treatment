using UnityEngine;
using UnityEngine.Events;
public class ReplaceActivateChildren : MonoBehaviour
{
    [Header("設定觸發標籤")]
    public string triggerTag = "toTrigger";
    [Header("要啟用的物件清單")]
    public GameObject[] objectsToActivate;
    [Header("要關閉的物件清單")]
    public GameObject[] objectsToDeactivate;
    [Header("偵測模式")]
    public bool useTrigger = true;
    [Header("劇本連動設定")]
    public UnityEvent OnActivatedEvent;
    private bool activated = false;

    void OnCollisionEnter(Collision collision)
    {
        if (useTrigger) return;
        Debug.Log($"<color=cyan>[物理碰撞偵測]</color> 撞到了物件: <b>{collision.gameObject.name}</b>，標籤為: <b>{collision.gameObject.tag}</b>");
        TryActivate(collision.gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!useTrigger) return;
        Debug.Log($"<color=cyan>[Trigger觸發偵測]</color> <b>{other.gameObject.name}</b> 進入了偵測範圍，其標籤為: <b>{other.gameObject.tag}</b>");
        TryActivate(other.gameObject);
    }

    private void TryActivate(GameObject other)
    {
        if (activated) return;
        if (!string.IsNullOrEmpty(triggerTag))
        {
            if (!other.CompareTag(triggerTag))
            {
                Debug.LogWarning($"<color=orange>[Tag不符]</color> 碰到的物件是 {other.name}，但它的 Tag 是 {other.tag}，不是 {triggerTag}");
                return;
            }
        }
        activated = true;

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

        if (objectsToDeactivate != null)
        {
            foreach (var obj in objectsToDeactivate)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                    Debug.Log($"<color=white> >> 執行指令: {obj.name}.SetActive(false)</color>");
                }
            }
        }

        Debug.Log($"<color=green>[觸發成功]</color> GameObject '{this.gameObject.name}' 的 activated 設為 true");
        Debug.Log($"<color=green>[系統]</color> 物品正確連接，觸發劇本事件。");
        OnActivatedEvent?.Invoke();
    }

    public bool CheckIfActivated()
    {
        return activated;
    }
}