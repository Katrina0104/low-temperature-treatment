using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 當碰到指定物件 (triggerTag) 時，啟用指定物件群組或該物件的整個子樹 (所有 descendant)。
/// 解決碰撞 "飛出" 問題的幾種策略（可在 Inspector 設定）：
/// - temporaryUseTriggerDuringActivate: 啟用後暫時把子物件的 Collider 設為 isTrigger = true（避免物理推擠），一個 FixedUpdate 後恢復。
/// - temporaryDisableCollidersDuringActivate: 啟用後暫時把子物件的 Collider.enabled = false，之後恢復（效果類似但會完全關 collider）。
/// - useKinematicDuringActivate: 啟用時把子物件 Rigidbody 設為 isKinematic = true（或暫時保留 kinematic），之後恢復並可把速度複製給非 kinematic rb。
/// - ignoreParentCollisionDuringActivate: 啟用時讓子物件與父物件 Collider 相互忽略碰撞，之後恢復（避免父與子互相解算）。
///
/// 使用：
//— 將此腳本掛在 b（要被觸發的物件）上；若 objectsToActivate 為空，預設會把 b 的 direct children 當作啟用群組。
//— 設定 triggerTag（或留空以接受任何碰撞者）、useTrigger（若使用 Trigger）等參數。
/// </summary>
public class ReplaceActivateGroup : MonoBehaviour
{
    [Tooltip("碰到此 Tag 的物件時觸發（留空表示接受任何物件）")]
    public string triggerTag = "toTrigger";

    [Tooltip("要在碰撞後啟用的物件（可包含多個 root；若留空，將預設啟用此物件的 direct children）")]
    public GameObject[] objectsToActivate;

    [Tooltip("若為 true，會啟用指定物件以及其所有子物件（遞迴）")]
    public bool activateRecursively = true;

    [Tooltip("是否把此 (B) 的 Rigidbody 速度複製給所有啟用後的 Rigidbody（若有）")]
    public bool copyVelocityToActivated = true;

    [Tooltip("使用 trigger 模式 (OnTriggerEnter)")]
    public bool useTrigger = false;

    [Header("Collision overlap handling (choose one or multiple)")]
    [Tooltip("啟用後暫時把子物件的 Collider 設為 isTrigger = true（避免瞬間物理推擠），等待一個 FixedUpdate 後恢復")]
    public bool temporaryUseTriggerDuringActivate = true;

    [Tooltip("啟用後暫時把子物件的 Collider.enabled = false（完全關閉 collider），等待一個 FixedUpdate 後恢復")]
    public bool temporaryDisableCollidersDuringActivate = false;

    [Tooltip("啟用時暫時把子物件 Rigidbody 設為 isKinematic = true，等待一個 FixedUpdate 後恢復（可避免物理即時影響）")]
    public bool useKinematicDuringActivate = false;

    [Tooltip("啟用時暫時讓子物件與父物件的 Collider 忽略碰撞，等待一個 FixedUpdate 後恢復")]
    public bool ignoreParentCollisionDuringActivate = true;

    // 非公開：防止重複啟用
    private bool activated = false;

    void Awake()
    {
        // 若 objectsToActivate 未手動指定，預設為 B 的所有 direct children
        if (objectsToActivate == null || objectsToActivate.Length == 0)
        {
            List<GameObject> list = new List<GameObject>();
            for (int i = 0; i < transform.childCount; i++)
            {
                list.Add(transform.GetChild(i).gameObject);
            }
            objectsToActivate = list.ToArray();

            if (objectsToActivate.Length == 0)
                Debug.LogWarning($"[ReplaceActivateGroup] {gameObject.name} 沒有 child，且 objectsToActivate 未設定。");
            else
                Debug.Log($"[ReplaceActivateGroup] 未設定 objectsToActivate，預設收集 {objectsToActivate.Length} 個 direct children 作為啟用群組。");
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
            if (other == null || other.tag != triggerTag)
                return;
        }

        activated = true;
        StartCoroutine(ActivateGroupCoroutine(other));
    }

    private IEnumerator ActivateGroupCoroutine(GameObject other)
    {
        Rigidbody parentRb = GetComponent<Rigidbody>();
        Collider parentCollider = GetComponent<Collider>();

        // 收集要啟用的 roots（可能是 direct children 或手動設定的 roots）
        var roots = objectsToActivate;

        // 為每個 root，先收集其所有 descendant 的 Collider / Rigidbody（包含 inactive）
        List<Collider> childColliders = new List<Collider>();
        List<Rigidbody> childRigidbodies = new List<Rigidbody>();
        List<(Collider, Collider)> ignoredPairs = new List<(Collider, Collider)>();
        List<bool> rbOriginalKinematic = new List<bool>(); // 備份狀態

        foreach (var root in roots)
        {
            if (root == null) continue;

            // collect colliders (including inactive)
            var cols = root.GetComponentsInChildren<Collider>(true);
            foreach (var c in cols)
            {
                if (c == null) continue;
                childColliders.Add(c);
            }

            // collect rigidbodies (including inactive)
            var rbs = root.GetComponentsInChildren<Rigidbody>(true);
            foreach (var r in rbs)
            {
                if (r == null) continue;
                childRigidbodies.Add(r);
                rbOriginalKinematic.Add(r.isKinematic);
            }
        }

        // If requested, set parent-child ignore collisions (store pairs)
        if (ignoreParentCollisionDuringActivate && parentCollider != null)
        {
            foreach (var cc in childColliders)
            {
                if (cc == null) continue;
                // store pair for later re-enable
                ignoredPairs.Add((parentCollider, cc));
                Physics.IgnoreCollision(parentCollider, cc, true);
            }
        }

        // If requested, temporarily set child rigidbodies to kinematic
        if (useKinematicDuringActivate)
        {
            foreach (var rb in childRigidbodies)
            {
                if (rb == null) continue;
                rb.isKinematic = true;
            }
        }

        // Activate roots & their descendants
        foreach (var root in roots)
        {
            if (root == null) continue;

            if (activateRecursively)
            {
                ActivateAllRecursively(root);
            }
            else
            {
                root.SetActive(true);
            }
        }

        // Before enabling colliders, optionally make them triggers or disable them to avoid instant physics resolution
        if (temporaryUseTriggerDuringActivate)
        {
            foreach (var col in childColliders)
            {
                if (col == null) continue;
                // Only modify non-trigger colliders; remember previous state in a temporary place? We'll assume restore to non-trigger.
                col.isTrigger = true;
            }
        }
        else if (temporaryDisableCollidersDuringActivate)
        {
            foreach (var col in childColliders)
            {
                if (col == null) continue;
                col.enabled = false;
            }
        }

        // Ensure transforms are in sync before physics step
        Physics.SyncTransforms();

        // Wait one physics step to allow engine to settle without resolving overlaps as collisions
        yield return new WaitForFixedUpdate();

        // Restore colliders (make them non-trigger / enabled) and restore rigidbody kinematic state
        if (temporaryUseTriggerDuringActivate)
        {
            foreach (var col in childColliders)
            {
                if (col == null) continue;
                try { col.isTrigger = false; } catch { }
            }
        }
        else if (temporaryDisableCollidersDuringActivate)
        {
            foreach (var col in childColliders)
            {
                if (col == null) continue;
                try { col.enabled = true; } catch { }
            }
        }

        if (useKinematicDuringActivate)
        {
            // restore original kinematic values (we saved only order-aligned values)
            int idx = 0;
            foreach (var rb in childRigidbodies)
            {
                if (rb == null) { idx++; continue; }
                // set back to original stored state; if original was non-kinematic, we may want to re-enable physics
                bool original = rbOriginalKinematic[Mathf.Clamp(idx, 0, rbOriginalKinematic.Count - 1)];
                rb.isKinematic = original;
                idx++;
            }
        }

        // If requested, copy parent velocity to children (for non-kinematic rbs)
        if (copyVelocityToActivated && parentRb != null)
        {
            foreach (var root in roots)
            {
                if (root == null) continue;
                var rbs = root.GetComponentsInChildren<Rigidbody>(true);
                foreach (var rb in rbs)
                {
                    if (rb == null) continue;
                    // Only assign to non-kinematic rigidbodies
                    if (!rb.isKinematic)
                    {
                        rb.velocity = parentRb.velocity;
                        rb.angularVelocity = parentRb.angularVelocity;
                    }
                }
            }
        }

        // Re-enable parent-child collisions if we set ignore before
        if (ignoreParentCollisionDuringActivate && parentCollider != null)
        {
            foreach (var pair in ignoredPairs)
            {
                try { Physics.IgnoreCollision(pair.Item1, pair.Item2, false); } catch { }
            }
        }

        // Final sync
        Physics.SyncTransforms();

        Debug.Log($"[ReplaceActivateGroup] {gameObject.name} 被觸發，啟用 {roots.Length} 個物件（handled overlaps: trigger={temporaryUseTriggerDuringActivate}, disableColliders={temporaryDisableCollidersDuringActivate}, kinematic={useKinematicDuringActivate}, ignoreParentCollision={ignoreParentCollisionDuringActivate}）");
    }

    // 將 root 與其所有 descendants 設為 active
    private void ActivateAllRecursively(GameObject root)
    {
        if (root == null) return;

        root.SetActive(true);

        var transforms = root.GetComponentsInChildren<Transform>(true);
        foreach (var t in transforms)
        {
            if (t == null || t.gameObject == null) continue;
            t.gameObject.SetActive(true);
        }
    }
}