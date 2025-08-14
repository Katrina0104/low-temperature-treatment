using UnityEngine;
using UnityEngine.AI;
using Flower;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class Nurse01_controller : MonoBehaviour
{
    private Animator animator;
    private NavMeshAgent agent;
    private FlowerSystem flowerSys; // 注入使用

    [Header("走路動畫設定")]
    public float walkAnimSpeedMultiplier = 1f;

    void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        if (animator == null) Debug.LogError("找不到 Animator 元件！");
        if (agent == null) Debug.LogError("找不到 NavMeshAgent 元件！");
    }

    void Start()
    {
        SetIdle();
    }

    void Update()
    {
        // 用 agent.velocity 控制走路動畫速度
        if (agent.velocity.magnitude > 0.1f)
        {
            animator.SetFloat("speed", agent.velocity.magnitude * walkAnimSpeedMultiplier);
        }
        else
        {
            SetIdle();
        }
    }

    private void SetIdle()
    {
        animator.SetFloat("speed", 0f);
    }

    // 給 UsageCase 注入 FlowerSystem
    public void SetFlowerSystem(FlowerSystem fs)
    {
        flowerSys = fs;

        // 註冊 Flower 指令
        flowerSys.RegisterCommand("NpcAnim", PlayAnimCommand);
        flowerSys.RegisterCommand("NpcMove", MoveCommand);
    }

    private void PlayAnimCommand(List<string> args)
    {
        if (args.Count == 0) return;
        PlayAnim(args[0].ToLower());
    }

    public void PlayAnim(string animTrigger)
    {
        if (animator != null)
        {
            animator.SetTrigger(animTrigger);
        }
    }

    private void MoveCommand(List<string> args)
    {
        if (args.Count < 4) return;

        string npcName = args[0];
        float x = float.Parse(args[1]);
        float y = float.Parse(args[2]);
        float z = float.Parse(args[3]);

        if (npcName != gameObject.name) return;

        MoveTo(new Vector3(x, y, z));
    }

    public void MoveTo(Vector3 destination)
    {
        if (agent != null)
        {
            agent.SetDestination(destination);
            StopAllCoroutines();
            StartCoroutine(WaitUntilReach(destination));
        }
    }

    private IEnumerator WaitUntilReach(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > agent.stoppingDistance + 0.05f)
        {
            yield return null;
        }

        agent.ResetPath();
        SetIdle();
    }

    // 可額外外部觸發動畫
    public void OnTalkEvent() => PlayAnim("talk");
    public void OnPickupEvent() => PlayAnim("pickup");
}
