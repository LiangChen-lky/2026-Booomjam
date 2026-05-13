using UnityEngine;
using System.Collections.Generic;
using Pathfinding;

public enum MonsterState
{
    Tracking,
    Wandering
}

[RequireComponent(typeof(Seeker))]
[RequireComponent(typeof(AIPath))]
public class MonsterController : MonoBehaviour
{
    [Header("配置")]
    [SerializeField] private MonsterInfo data;
    
    [Header("引用")]
    [SerializeField] private Transform player;
    [SerializeField] private PlayerController playerController;
    
    [Header("调试")]
    [SerializeField] private MonsterState currentState = MonsterState.Wandering;
    
    [Header("开门设置")]
    [SerializeField, Tooltip("怪物前方检测门的距离")]
    private float doorDetectionRange = 1.5f;

    [Header("怪物音效")]
    [SerializeField, Tooltip("脚步声播放间隔（秒）")]
    private float monsterFootstepInterval = 0.5f;

    private float lastMonsterFootstepTime;

    [Header("BGM 切换")]
    [SerializeField, Tooltip("怪物接近时切换 BGM 的距离")]
    private float bgmNearDistance = 15f;

    private AIPath aiPath;
    private float lastAttackTime;
    private float lastWanderTime;
    private Vector2 wanderTarget;
    private bool hasWanderTarget = false;
    private HashSet<int> openedDoorIds = new HashSet<int>();
    private BGM lastBGMState = BGM.None;
    
    private void Awake()
    {
        aiPath = GetComponent<AIPath>();
    }
    
    private void Start()
    {
        if (data == null)
        {
            // Debug.LogError("[MonsterController] MonsterInfo data is not assigned!");
            enabled = false;
            return;
        }

        aiPath.maxSpeed = data.moveSpeed;
        aiPath.enableRotation = false;
        aiPath.simulateMovement = true;
        aiPath.canSearch = true;
        
// #if UNITY_EDITOR
//         Debug.Log($"[MonsterController] 初始化完成 - maxSpeed: {aiPath.maxSpeed}, canMove: {aiPath.canMove}, canSearch: {aiPath.canSearch}");
//         Debug.Log($"[MonsterController] 怪物位置: {transform.position}");
//         
//         // 检查是否在寻路网格上
//         var node = AstarPath.active.GetNearest(transform.position);
//         if (node.node != null)
//         {
//             Debug.Log($"[MonsterController] 最近节点位置: {(Vector3)node.node.position}");
//         }
//         else
//         {
//             Debug.LogError("[MonsterController] 找不到附近的寻路节点！怪物不在寻路网格上！");
//         }
// #endif
        
        SwitchToWandering();
    }
    
    private void Update()
    {
        UpdateBGMState();

        switch (currentState)
        {
            case MonsterState.Tracking:
                TrackingUpdate();
                break;
            case MonsterState.Wandering:
                WanderingUpdate();
                break;
        }
    }
    
    private void CheckPlayerState()
    {
        if (playerController == null || player == null) return;
        
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        if (playerController.IsInteracting || distanceToPlayer > data.trackingRange)
        {
            if (currentState == MonsterState.Tracking)
            {
                SwitchToWandering();
            }
        }
        else
        {
            if (currentState == MonsterState.Wandering && distanceToPlayer <= data.trackingRange)
            {
                SwitchToTracking();
            }
        }
    }
    
    private void SwitchToTracking()
    {
        currentState = MonsterState.Tracking;
        hasWanderTarget = false;

        // 播放发现玩家咆哮音效
        AudioManager.Instance.PlayAtPosition(SFX.MonsterGrowl, transform.position);
        AudioManager.Instance.PlayAtPosition(SFX.MonsterChase, transform.position);
    }
    
    private void SwitchToWandering()
    {
        currentState = MonsterState.Wandering;
        hasWanderTarget = false;
        lastWanderTime = Time.time;
        SetNewWanderPoint();
// #if UNITY_EDITOR
//         Debug.Log("怪物切换到漫游状态");
// #endif
    }
    
    private void TrackingUpdate()
    {
        // 追踪状态脚步声
        if (Time.time - lastMonsterFootstepTime >= monsterFootstepInterval)
        {
            lastMonsterFootstepTime = Time.time;
            AudioManager.Instance.PlayAtPosition(SFX.MonsterFootstep, transform.position);
        }

        CheckPlayerState();
        
        if (currentState != MonsterState.Tracking) return;
        if (player == null) return;
        
        // 检测前方是否有门，自动开门
        CheckForDoor();
        
        // 设置寻路目标为玩家位置
        aiPath.destination = player.position;
        
// #if UNITY_EDITOR
//         // 调试信息
//         if (Time.frameCount % 60 == 0) // 每60帧输出一次
//         {
//             Debug.Log($"[Monster] State: {currentState}, Pos: {transform.position}, Dest: {aiPath.destination}, Velocity: {aiPath.velocity}, Remaining: {aiPath.remainingDistance}");
//         }
// #endif
        
        // 如果寻路失败（Remaining为Infinity），使用直接移动
        if (float.IsInfinity(aiPath.remainingDistance))
        {
            // 直接朝玩家方向移动
            Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;
            transform.position += (Vector3)(direction * data.moveSpeed * Time.deltaTime);
        }
        
        // 检测是否到达攻击范围
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        if (distanceToPlayer <= data.attackRange)
        {
            TryAttackPlayer();
        }
    }
    
    private void WanderingUpdate()
    {
        // 漫游状态脚步声（间隔更长）
        if (aiPath.velocity.sqrMagnitude > 0.1f && Time.time - lastMonsterFootstepTime >= monsterFootstepInterval * 1.5f)
        {
            lastMonsterFootstepTime = Time.time;
            AudioManager.Instance.PlayAtPosition(SFX.MonsterFootstep, transform.position);
        }

        CheckPlayerState();
        
        if (currentState != MonsterState.Wandering) return;
        
        // 检查是否需要设置新的漫游点
        bool needNewTarget = !hasWanderTarget || aiPath.reachedDestination || float.IsInfinity(aiPath.remainingDistance);
        
        if (needNewTarget)
        {
            if (Time.time - lastWanderTime >= data.wanderInterval)
            {
                SetNewWanderPoint();
                lastWanderTime = Time.time;
            }
        }
        
        // 如果寻路失败（Remaining为Infinity），使用直接移动
        if (float.IsInfinity(aiPath.remainingDistance) && hasWanderTarget)
        {
            Vector2 direction = (wanderTarget - (Vector2)transform.position).normalized;
            transform.position += (Vector3)(direction * data.moveSpeed * Time.deltaTime);
            
            // 检查是否到达目标点
            float distanceToTarget = Vector2.Distance(transform.position, wanderTarget);
            if (distanceToTarget < 0.5f)
            {
                hasWanderTarget = false;
            }
        }
    }
    
    private void SetNewWanderPoint()
    {
        // 在当前位置周围随机选择一个点
        Vector2 randomDirection = Random.insideUnitCircle.normalized;
        wanderTarget = (Vector2)transform.position + randomDirection * data.wanderRadius;
        
        // 设置寻路目标
        aiPath.destination = wanderTarget;
        hasWanderTarget = true;
    }
    
    // 检测前方是否有门
    private void CheckForDoor()
    {
        if (player == null) return;
        
        Vector2 direction = ((Vector2)player.position - (Vector2)transform.position).normalized;
        
        // 使用 OverlapCircle 检测门，因为门可能只有 Trigger 碰撞体
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            doorDetectionRange,
            LayerMask.GetMask("Default")
        );
        
        foreach (var hit in hits)
        {
            if (hit != null && hit.CompareTag("Door"))
            {
                var doorComponent = hit.GetComponent<Door>();
                if (doorComponent != null && (!doorComponent.CanMonsterOpen || doorComponent.IsOpen))
                {
                    continue;
                }

                // 检查门是否在玩家方向
                Vector2 doorDir = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
                float dot = Vector2.Dot(direction, doorDir);
                
                // 只处理前方的门（dot > 0.5 表示在前方约 60 度范围内）
                if (dot > 0.5f)
                {
                    int doorId = hit.GetInstanceID();
                    // 只打开未开的门
                    if (!openedDoorIds.Contains(doorId))
                    {
                        OpenDoor(hit.gameObject);
                    }
                }
            }
        }
    }
    
    // 开门
    private void OpenDoor(GameObject door)
    {
        int doorId = door.GetInstanceID();

        var doorComponent = door.GetComponent<Door>();
        if (doorComponent != null)
        {
            if (!doorComponent.CanMonsterOpen || doorComponent.IsOpen) return;
            doorComponent.ApplyOpenState(true, 0.7f);
        }
        else
        {
            // 更新 SpriteRenderer 透明度
            var sr = door.GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null)
            {
                Color c = sr.color;
                c.a = 0.7f; // 与 PlayerController.doorOpenedSpriteAlpha 一致
                sr.color = c;
            }

            // 禁用物理碰撞体（保留 Trigger 用于交互检测）
            foreach (var col in door.GetComponentsInChildren<Collider2D>(true))
            {
                if (!col.isTrigger)
                    col.enabled = false;
            }
        }

        // 记录已开门 ID
        openedDoorIds.Add(doorId);

        // 播放开门音效
        AudioManager.Instance.PlayAtPosition(SFX.DoorOpen, transform.position);
        AudioManager.Instance.PlayAtPosition(SFX.MonsterWallHit, transform.position);
    }
    
    private void TryAttackPlayer()
    {
        if (playerController == null) return;
        if (Time.time - lastAttackTime < data.attackCooldown) return;
        
        lastAttackTime = Time.time;
        playerController.TakeDamage((int)data.attackDamage);
        AudioManager.Instance.Play(SFX.PlayerHit);
// #if UNITY_EDITOR
//         Debug.Log("怪物攻击玩家");
// #endif
    }
    
    private void UpdateBGMState()
    {
        if (player == null) return;

        float distance = Vector2.Distance(transform.position, player.position);
        BGM desiredBGM = distance <= bgmNearDistance ? BGM.MonsterNear : BGM.Exploration;

        if (desiredBGM != lastBGMState)
        {
            lastBGMState = desiredBGM;
            AudioManager.Instance.PlayBGM(desiredBGM);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 绘制追踪范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, data.trackingRange);
        
        // 绘制攻击范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, data.attackRange);
        
        // 绘制漫游范围
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, data.wanderRadius);
        
        // 绘制门检测范围
        Gizmos.color = Color.green;
        Vector2 dir = player != null ? 
            ((Vector2)player.position - (Vector2)transform.position).normalized : 
            (Vector2)transform.right;
        Gizmos.DrawRay(transform.position, dir * doorDetectionRange);
    }
}
