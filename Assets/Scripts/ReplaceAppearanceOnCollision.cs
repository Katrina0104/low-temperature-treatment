using UnityEngine;

/// <summary>
/// 當與指定 tag 的物體碰撞/觸發時，只替換外觀（不 Destroy 原本物件的 Rigidbody）：
/// - 會將本物件的 MeshRenderer / SkinnedMeshRenderer 停用（隱藏原外觀）
/// - Instantiate replacementPrefab 作為本物件的 child 並把 transform 對齊(本地位置/旋轉歸零)
/// - 預設會移除 replacementPrefab（實例）上所有的 Rigidbody 與 Collider，以避免重複物理元件
/// 使用方式：
/// - 把此腳本掛在 b（要被替換外觀的物件）上
/// - 指定 replacementPrefab（c 的外觀 prefab），並設定 triggerTag（a 的 tag）
/// - 若使用 Trigger，請勾選 useTrigger，否則用物理碰撞 (OnCollisionEnter)
/// 注意：
/// - replacementPrefab 若有 SkinnedMeshRenderer 與骨骼(Bones)，父子化可能需要特別處理（若遇到骨骼錯位，可考慮只放靜態 Mesh）。
/// </summary>
public class ReplaceAppearanceOnCollision : MonoBehaviour
{
    [Header("偵測設定")]
    public string triggerTag = "A"; // 與擁有此 tag 的物件發生碰撞時觸發
    public bool useTrigger = false; // true -> OnTriggerEnter / false -> OnCollisionEnter

    [Header("替換用 Prefab")]
    public GameObject replacementPrefab; // c 的外觀 prefab（建議不要有 Rigidbody/Collider）

    [Header("行為選項")]
    public bool disableOriginalRenderers = true; // 停用本物件的 renderer（true 建議）
    public bool removePrefabPhysics = true; // 從實例移除 Rigidbody / Collider（避免重複物理）
    public bool keepReplacementWorldScale = false; // 若 true 保持實例的世界 scale，否則會設為 localScale = Vector3.one

    GameObject instantiatedReplacement;
    bool replaced = false;

    void OnCollisionEnter(Collision collision)
    {
        if (useTrigger) return;
        HandleCollision(collision.gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!useTrigger) return;
        HandleCollision(other.gameObject);
    }

    void HandleCollision(GameObject other)
    {
        if (replaced) return;
        if (!other.CompareTag(triggerTag)) return;
        if (replacementPrefab == null)
        {
            Debug.LogWarning($"[{nameof(ReplaceAppearanceOnCollision)}] replacementPrefab 未設定。");
            return;
        }
        Debug.LogWarning($"[設定完成]");

        ReplaceAppearance();
    }

    public void ReplaceAppearance()
    {
        if (replaced) return;
        replaced = true;

        if (disableOriginalRenderers)
            SetRenderersEnabled(gameObject, false);

        // Instantiate replacementPrefab 作為 child，並對齊 transform
        instantiatedReplacement = Instantiate(replacementPrefab, transform.position, transform.rotation, transform);

        // 對齊到本地 transform（確保位於 b 的本地原點）
        instantiatedReplacement.transform.localPosition = Vector3.zero;
        instantiatedReplacement.transform.localRotation = Quaternion.identity;
        if (!keepReplacementWorldScale)
            instantiatedReplacement.transform.localScale = Vector3.one;

        // 移除實例上的 Rigidbody 與 Collider（若選項開）
        if (removePrefabPhysics)
        {
            Debug.Log($"[{nameof(ReplaceAppearanceOnCollision)}] 移除實例上的 Rigidbody 與 Collider。");
            var rbs = instantiatedReplacement.GetComponentsInChildren<Rigidbody>(true);
            foreach (var rb in rbs)
            {
                Destroy(rb);
            }

            var cols = instantiatedReplacement.GetComponentsInChildren<Collider>(true);
            foreach (var col in cols)
            {
                Destroy(col);
            }
        }

        // 將實例設定為與 b 同 layer / tag（視需求）
        SetLayerRecursively(instantiatedReplacement, gameObject.layer);
        instantiatedReplacement.tag = gameObject.tag;
    }

    // 若需要回復外觀（測試用），可呼叫此函式
    public void RevertAppearance()
    {
        if (!replaced) return;
        if (instantiatedReplacement != null)
            Destroy(instantiatedReplacement);

        SetRenderersEnabled(gameObject, true);
        replaced = false;
    }

    void SetRenderersEnabled(GameObject root, bool enabled)
    {
        var meshRenderers = root.GetComponentsInChildren<MeshRenderer>(true);
        foreach (var mr in meshRenderers) mr.enabled = enabled;

        var skinned = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var smr in skinned) smr.enabled = enabled;
    }

    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform t in obj.transform)
            SetLayerRecursively(t.gameObject, layer);
    }
}
