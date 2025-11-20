using UnityEngine;

public class ReplaceOnCollision : MonoBehaviour
{
    [Tooltip("碰到此 Tag 的物體時觸發替換 (例: A)")]
    public string triggerTag = "toTrigger";

    [Tooltip("要生成的替代 Prefab (C)")]
    public GameObject replacementPrefab;

    [Tooltip("是否把原本 Rigidbody 的速度複製到新物件")]
    public bool copyVelocity = true;

    // 防止重複替換
    private bool replaced = false;

    void Start()
    {
        Debug.Log($"[ReplaceOnCollision] 啟用在 {gameObject.name}, tag={gameObject.tag}");
    }

    void OnCollisionEnter(Collision collision)
    {
        // 已經替換過了就跳過
        if (replaced) return;

        // 只在碰到指定 tag 的物件時才做替換
        if (!collision.gameObject.CompareTag(triggerTag))
        {
            Debug.Log($"[ReplaceOnCollision] 碰撞到 {collision.gameObject.name}，tag 不符合 ({triggerTag})，忽略");
            return;
        }

        if (replacementPrefab == null)
        {
            Debug.LogError("[ReplaceOnCollision] replacementPrefab 未設！");
            return;
        }

        // 確保這個腳本只會由被替換的物件 (B) 執行
        // (如果你把腳本掛在 A，也可以調整邏輯改為用 collision.gameObject)
        GameObject b = this.gameObject;
        Transform parent = b.transform.parent;
        Vector3 worldPos = b.transform.position;
        Quaternion worldRot = b.transform.rotation;

        // 設置為已替換（避免雙重執行）
        replaced = true;

        // 使用帶 parent 的 Instantiate，並給定世界座標
        GameObject c = Instantiate(replacementPrefab, worldPos, worldRot, parent);

        // 如果 prefab 內部有本身偏移且你想以 local 對齊，可以改用下面兩行
        // c.transform.SetParent(parent, false);
        // c.transform.localPosition = b.transform.localPosition;

        // 複製 Rigidbody 的速度（如果需要）
        if (copyVelocity)
        {
            Rigidbody rbB = b.GetComponent<Rigidbody>();
            Rigidbody rbC = c.GetComponent<Rigidbody>();
            if (rbB != null && rbC != null)
            {
                rbC.velocity = rbB.velocity;
                rbC.angularVelocity = rbB.angularVelocity;
            }
        }

        // 保留 tag / layer（視需求）
        c.tag = b.tag;
        c.layer = b.layer;

        // 刪除或停用原本的 b
        Destroy(b);

        Debug.Log($"[ReplaceOnCollision] 已替換 {b.name} -> {c.name} at {worldPos}");
    }
}

