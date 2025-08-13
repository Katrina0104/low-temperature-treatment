using UnityEngine;
using UnityEngine.AI;
using Flower;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
public class Nurse01_controller : MonoBehaviour
{
    private Animator animator;
    private NavMeshAgent agent;

    [Header("Flower 設定")]
    public string flowerSystemName = "FlowerSample";  // FlowerSystem 名稱
    public string talkResourceName = "NPC_nurse01";   // 對話資源名稱

    void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();

        if (animator == null) Debug.LogError("找不到 Animator 元件！");
        if (agent == null) Debug.LogError("找不到 NavMeshAgent 元件！");
    }

    void Start()
    {
        SetIdle(); // 初始 idle

        // 取得 FlowerSystem
        FlowerSystem flowerSys = FlowerManager.Instance.GetFlowerSystem(flowerSystemName);
        if (flowerSys != null)
        {
            flowerSys.RegisterCommand("NpcAnim", PlayAnimCommand);

            flowerSys.RegisterCommand("NpcMove", (List<string> args) => {
                if (args.Count >= 4)
                {
                    string npcName = args[0];
                    float x = float.Parse(args[1]);
                    float y = float.Parse(args[2]);
                    float z = float.Parse(args[3]);

                    GameObject npcObj = GameObject.Find(npcName);
                    if (npcObj != null)
                    {
                        Nurse01_controller npc = npcObj.GetComponent<Nurse01_controller>();
                        if (npc != null)
                        {
                            npc.MoveTo(new Vector3(x, y, z)); // **只移動，不觸發文字**
                        }
                    }
                }
            });
        }
    }

    void Update()
    {
        // 用 agent.velocity 控制走路動畫速度
        if (agent.velocity.magnitude > 0.1f)
        {
            animator.SetFloat("speed", agent.velocity.magnitude);
        }
        else
        {
            SetIdle();
        }
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
            Debug.Log($"NPC 播放動畫 Trigger: {animTrigger}");
        }
    }

    private void SetIdle()
    {
        animator.SetFloat("speed", 0f); // 停下來就 idle
    }

    /// <summary>
    /// 移動 NPC 到目標點
    /// </summary>
    public void MoveTo(Vector3 destination)
    {
        if (agent != null)
        {
            agent.SetDestination(destination);
            StartCoroutine(WaitUntilReach(destination));
        }
    }

    private System.Collections.IEnumerator WaitUntilReach(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > agent.stoppingDistance + 0.05f)
        {
            yield return null;
        }

        agent.ResetPath();
        SetIdle(); // 到達後 idle
    }

    // 外部事件觸發動畫
    public void OnTalkEvent() => PlayAnim("talk");
    public void OnPickupEvent() => PlayAnim("pickup");
}
