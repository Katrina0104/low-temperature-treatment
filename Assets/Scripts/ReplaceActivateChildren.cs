using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 當碰到指定物件 (triggerTag) 時啟用 b 的子物件 (c, d)。
/// 使用方式：
/// - 最簡單：在 Inspector 把要啟用的子物件拖到 objectsToActivate 陣列（即使子物件為 inactive 也能拖）。
/// - 或者：不手動指派時，腳本會自動依 childNamesToFind 找到同名子物件；若 childNamesToFind 未設定，則會自動收集所有 inactive 的直接子物件。
/// </summary>
public class ReplaceActivateChildren : MonoBehaviour
{
    [Tooltip("碰到此 Tag 的物件時啟用子物件 (例: A)")]
    public string triggerTag = "toTrigger";

    [Tooltip("要在碰撞後啟用的子物件（可在 Inspector 拖入 c,d；若留空，腳本會嘗試自動尋找）")]
    public GameObject[] objectsToActivate;

    [Tooltip("若想由名稱自動尋找子物件，填入名稱（例如 \"c\", \"d\"），腳本會以 transform.Find(name) 嘗試尋找")]
    public string[] childNamesToFind;

    [Tooltip("是否在啟用後停用父物件的 Collider")]
    public bool disableParentCollider = false;

    [Tooltip("是否在啟用後隱藏父物件的 Renderer（若有）")]
    public bool hideParentRenderer = false;

    [Tooltip("是否把父物件 Rigidbody 的速度複製給啟用的子物件（若子物件有 Rigidbody）")]
    public bool copyVelocityToChildren = true;

    [Tooltip("若使用 trigger 模式，請勾選並將其中一個 Collider 的 Is Trigger 設為 true")]
    public bool useTrigger = false;

    private bool activated = false;

    void Awake()
    {
        // 如果 inspector 未手動設定 objectsToActivate，嘗試自動填充
        if (objectsToActivate == null || objectsToActivate.Length == 0)
        {
            List<GameObject> found = new List<GameObject>();

            // 優先使用 childNamesToFind（若有設定）
            if (childNamesToFind != null && childNamesToFind.Length > 0)
            {
                foreach (var name in childNamesToFind)
                {
                    if (string.IsNullOrEmpty(name)) continue;
                    Transform t = transform.Find(name);
                    if (t != null)
                    {
                        found.Add(t.gameObject);
                        Debug.Log($"[ReplaceActivateChildren] 自動找到子物件: {name}");
                    }
                    else
                    {
                        Debug.LogWarning($"[ReplaceActivateChildren] 未找到子物件 (transform.Find) 名稱: {name}");
                    }
                }
            }

            // 若還沒找到任何，預設收集所有 direct children 中目前為 inactive 的項目
            if (found.Count == 0)
            {
                foreach (Transform child in transform)
                {
                    if (!child.gameObject.activeSelf)
                        found.Add(child.gameObject);
                }

                if (found.Count > 0)
                    Debug.Log($"[ReplaceActivateChildren] 自動收集到 {found.Count} 個 inactive 子物件作為待啟用列表。");
                else
                    Debug.LogWarning("[ReplaceActivateChildren] 未在 Inspector 設定 objectsToActivate，且未自動找到任何 inactive 子物件。");
            }

            objectsToActivate = found.ToArray();
        }
    }

    void Start()
    {
        // 顯示目前會被啟用的項目（方便除錯）
        if (objectsToActivate != null && objectsToActivate.Length > 0)
        {
            string names = string.Join(", ", System.Array.ConvertAll(objectsToActivate, g => g != null ? g.name : "null"));
            Debug.Log($"[ReplaceActivateChildren] 目前 objectsToActivate: {names}");
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (useTrigger) return;
        TryActivate(collision.gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!useTrigger) return;
        TryActivate(other.gameObject);
    }

    private void TryActivate(GameObject other)
    {
        if (activated) return;

        if (!string.IsNullOrEmpty(triggerTag))
        {
            // 使用 CompareTag 比較快，但要確保 Tag 已在 Tags 設定裡建立
            if (!other.CompareTag(triggerTag))
                return;
        }

        // 啟用子物件
        activated = true;
        Rigidbody parentRb = GetComponent<Rigidbody>();

        foreach (var obj in objectsToActivate)
        {
            if (obj == null) continue;

            obj.SetActive(true);

            // 複製速度（SetActive 之後再存取 child Rigidbody）
            if (copyVelocityToChildren && parentRb != null)
            {
                Rigidbody childRb = obj.GetComponent<Rigidbody>();
                if (childRb != null)
                {
                    childRb.velocity = parentRb.velocity;
                    childRb.angularVelocity = parentRb.angularVelocity;
                }
            }
        }

        if (disableParentCollider)
        {
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        if (hideParentRenderer)
        {
            var rend = GetComponent<Renderer>();
            if (rend != null) rend.enabled = false;
        }

        Debug.Log($"[ReplaceActivateChildren] {gameObject.name} 被 {other.name} 觸發，已啟用 {objectsToActivate.Length} 個子物件。");
    }
}