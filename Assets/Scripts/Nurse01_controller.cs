using UnityEngine;

public class Nurse01_controller : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;
    public float moveSpeed = 2f;
    public bool isTalking = false; // 外部可切換
    public float talkInterval = 1f; // 每幾秒觸發一次

    private Vector3 target;
    private Animator animator;
    private float talkTimer = 0f;

    void Start()
    {
        target = pointB.position;
        animator = GetComponent<Animator>();

        animator.SetFloat("speed", 1f); // 預設原地踏步動畫
    }

    void Update()
    {
        // --- 自動偵測場上 Dialog 是否存在且啟用 ---
        GameObject dialog = GameObject.Find("DefaultDialogPrefab"); // 根據你的 prefab 名稱或 tag 調整
        if (dialog != null && dialog.activeInHierarchy)
        {
            isTalking = true;
        }
        else
        {
            isTalking = false;
        }
        // --- END ---

        // 狀態切換
        animator.SetBool("isTalking", isTalking);

        if (isTalking)
        {
            // 講話時不移動
            talkTimer += Time.deltaTime;
            if (talkTimer >= talkInterval)
            {
                animator.SetTrigger("talk");
                talkTimer = 0f;
            }
            animator.SetFloat("speed", 0f);
            return;
        }
        else
        {
            animator.SetFloat("speed", 1f); // 移動時播放走路動畫
        }

        // 方向與距離
        Vector3 direction = (target - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, target);

        // 角色面向移動方向
        if (direction != Vector3.zero)
        {
            // 僅旋轉 Y 軸（2D 請改用 Z 軸）
            Quaternion toRotation = Quaternion.LookRotation(direction, Vector3.up);
            transform.rotation = Quaternion.Lerp(transform.rotation, toRotation, 0.2f); // 平滑旋轉
        }

        // 實際移動角色
        transform.position += direction * moveSpeed * Time.deltaTime;

        // 到達目標時切換方向
        if (distance < 0.1f)
        {
            target = (target == pointA.position) ? pointB.position : pointA.position;
            // 不再用 localScale 翻轉
        }
    }
}