using UnityEngine;

public class Nurse01_controller : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;
    public float moveSpeed = 2f;
    public bool isTalking = false; // 外部可切換

    private Vector3 target;
    private Animator animator;

    void Start()
    {
        target = pointB.position;
        animator = GetComponent<Animator>();

        animator.SetFloat("speed", 1f); // 預設原地踏步動畫
    }

    void Update()
    {
        // 狀態切換
        animator.SetBool("isTalking", isTalking);

        if (isTalking)
        {
            // 講話時不移動
            animator.SetFloat("speed", 0f); // 停止原地踏步動畫
            return;
        }
        else
        {
            animator.SetFloat("speed", 1f); // 移動時播放走路動畫
        }

        // 方向與距離
        Vector3 direction = (target - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, target);

        // 實際移動角色
        transform.position += direction * moveSpeed * Time.deltaTime;

        // 到達目標時切換方向
        if (distance < 0.1f)
        {
            target = (target == pointA.position) ? pointB.position : pointA.position;

            // 翻轉角色面朝方向
            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;
        }
    }
}