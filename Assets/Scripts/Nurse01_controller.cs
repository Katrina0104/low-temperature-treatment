using UnityEngine;

public class Nurse01_controller : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;
    public float moveSpeed = 2f;

    private Vector3 target;
    private Animator animator;

    void Start()
    {
        target = pointB.position;
        animator = GetComponent<Animator>();

        // 設定為原地踏步動畫速度
        animator.SetFloat("speed", 1f); // 這是動畫參數，不是角色移動速度
    }

    void Update()
    {
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
