using UnityEngine;
using System.Collections.Generic;

public class EnemyAI : MonoBehaviour, IRuaaaReceiver
{
    [Header("类型")]
    public MaskType enemyType; 
    
    [Header("状态")]
    public Vector2Int currentGridPos;
    public bool enraged;
    private Vector2Int plannedTargetPos;
    private bool hasPlannedMove;

    [Header("行动节奏")]
    public int normalMoveInterval = 2;
    public int turtleMoveInterval = 5;
    private int nextPlanTick = 1;
    private int plannedMoveTick;

    [Header("Sprite设置")]
    // 假设你的美术素材默认是头朝上的，如果默认朝右，这里填 0
    public float spriteDefaultAngle = 90f;

    [Header("移动表现")]
    public float moveAnimationDuration = 0.22f;
    private Vector3 moveStartWorldPos;
    private Vector3 moveTargetWorldPos;
    private float moveAnimationTimer;
    private bool isMoveAnimating;
    private TickMoveVisuals moveVisuals;
    private MoveTargetMarker targetMarker;
    private SpriteRenderer[] spriteRenderers;
    private Color[] normalRendererColors;
    private bool hasCachedRendererColors;
    private float enragedUntilTime;
    private bool hasRoared; 

    void Start()
    {
        // 自动吸附
        if (TileManager.Instance != null)
        {
            currentGridPos = TileManager.Instance.GameMap.WorldToGridPos(transform.position);
            transform.position = TileManager.Instance.GameMap.GridToWorldPos(currentGridPos);
            moveStartWorldPos = transform.position;
            moveTargetWorldPos = transform.position;
            moveVisuals = GetComponent<TickMoveVisuals>();
            if (moveVisuals == null) moveVisuals = gameObject.AddComponent<TickMoveVisuals>();
            targetMarker = MoveTargetMarker.Create($"{name}_PlannedTarget", 5);
            ApplyEnragedVisual();
            RegisterPosition(currentGridPos);
        }

        if (TickManager.Instance != null)
        {
            TickManager.Instance.OnEnemyPlanTick += OnPlanTick;
            TickManager.Instance.OnEnemyMoveTick += OnMoveTick;
            nextPlanTick = TickManager.Instance.CurrentTick + 1;
        }
    }

    void OnDestroy()
    {
        if (TickManager.Instance != null)
        {
            TickManager.Instance.OnEnemyPlanTick -= OnPlanTick;
            TickManager.Instance.OnEnemyMoveTick -= OnMoveTick;
        }
        
        UnregisterPosition(currentGridPos);
        if (targetMarker != null) Destroy(targetMarker.gameObject);
    }

    void Update()
    {
        UpdateMoveAnimation();
        UpdateEnragedTimer();
    }

    void OnPlanTick(int tick)
    {
        if (tick != nextPlanTick) return;

        if (enemyType == MaskType.Dragon && !hasRoared)
        {
            RuaaaBroadcast.Broadcast(transform.position);
            hasRoared = true;
        }

        hasPlannedMove = TryPlanMove(out plannedTargetPos);
        plannedMoveTick = tick + 1;
        nextPlanTick = tick + GetMoveInterval();

        if (hasPlannedMove)
        {
            ShowPlannedTarget(plannedTargetPos);
        }
        else if (targetMarker != null)
        {
            targetMarker.Hide();
        }
    }

    void OnMoveTick(int tick)
    {
        if (!hasPlannedMove || tick != plannedMoveTick) return;

        hasPlannedMove = false;
        if (targetMarker != null) targetMarker.Hide();

        if (CanMoveTo(plannedTargetPos))
        {
            MoveTo(plannedTargetPos);
        }

        CheckSurroundingKills();
    }

    int GetMoveInterval()
    {
        return enemyType == MaskType.Turtle ? turtleMoveInterval : normalMoveInterval;
    }

    bool TryPlanMove(out Vector2Int targetPos)
    {
        List<Vector2Int> candidates = GetMovePattern();
        targetPos = currentGridPos;

        if (enraged && TryFindChaseMove(candidates, out targetPos))
        {
            return true;
        }

        List<Vector2Int> validMoves = new List<Vector2Int>();
        foreach (var moveVec in candidates)
        {
            Vector2Int candidate = currentGridPos + moveVec;
            if (CanMoveTo(candidate))
            {
                validMoves.Add(candidate);
            }
        }

        if (validMoves.Count == 0) return false;

        int rnd = Random.Range(0, validMoves.Count);
        targetPos = validMoves[rnd];
        return true;
    }

    bool TryFindChaseMove(List<Vector2Int> candidates, out Vector2Int targetPos)
    {
        targetPos = currentGridPos;
        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player == null) return false;

        int bestDistance = int.MaxValue;
        bool foundMove = false;

        foreach (var moveVec in candidates)
        {
            Vector2Int candidate = currentGridPos + moveVec;
            if (!CanMoveTo(candidate)) continue;

            int distance = Mathf.Abs(candidate.x - player.currentGridPos.x) + Mathf.Abs(candidate.y - player.currentGridPos.y);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                targetPos = candidate;
                foundMove = true;
            }
        }

        return foundMove;
    }

    void ShowPlannedTarget(Vector2Int targetPos)
    {
        if (targetMarker == null) return;

        Color color = TickMoveVisuals.GetColor(enemyType);
        if (enraged) color = Color.red;
        targetMarker.Show(TileManager.Instance.GameMap.GridToWorldPos(targetPos), color, 0.92f);
    }

    public void ReceiveRuaaaBroadcast(Vector3 origin, float duration)
    {
        SetEnragedFor(duration);
    }

    public void SetEnragedFor(float duration)
    {
        enragedUntilTime = Mathf.Max(enragedUntilTime, Time.time + duration);
        SetEnraged(true);
    }

    void UpdateEnragedTimer()
    {
        if (enraged && Time.time >= enragedUntilTime)
        {
            SetEnraged(false);
        }
    }

    public void SetEnraged(bool value)
    {
        enraged = value;
        if (!value) enragedUntilTime = 0f;
        ApplyEnragedVisual();
    }

    void ApplyEnragedVisual()
    {
        CacheRendererColors();

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            if (spriteRenderers[i] == null) continue;
            spriteRenderers[i].color = enraged
                ? Color.Lerp(normalRendererColors[i], Color.red, 0.55f)
                : normalRendererColors[i];
        }
    }

    void CacheRendererColors()
    {
        if (hasCachedRendererColors) return;

        spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        normalRendererColors = new Color[spriteRenderers.Length];
        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            normalRendererColors[i] = Color.white;
        }

        hasCachedRendererColors = true;
    }

    // --- 核心新功能：向量旋转 ---
    void UpdateRotation(Vector2Int moveDir)
    {
        // Atan2 返回的是弧度，(y, x) 注意顺序
        // 结果是：右=0度, 上=90度, 左=180度, 下=-90度
        float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;

        // 减去 spriteDefaultAngle 是为了修正素材本身的朝向
        // 比如素材头朝上(90度)，你想让它朝右(0度)，就需要旋转 -90度
        transform.rotation = Quaternion.Euler(0, 0, angle - spriteDefaultAngle);
    }

    // --- 核心新功能：周围击杀 (九宫格检测) ---
    void CheckSurroundingKills()
    {
        PlayerController trackedPlayer = FindAnyObjectByType<PlayerController>();
        if (trackedPlayer != null && trackedPlayer.currentMask != this.enemyType)
        {
            int dx = Mathf.Abs(trackedPlayer.currentGridPos.x - currentGridPos.x);
            int dy = Mathf.Abs(trackedPlayer.currentGridPos.y - currentGridPos.y);
            if (dx <= 1 && dy <= 1)
            {
                Debug.Log($"Caught by {enemyType}");
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.TriggerGameOver(enemyType.ToString());
                }
                return;
            }
        }

        var map = TileManager.Instance.GameMap;

        // 遍历 x: -1 to 1, y: -1 to 1 (包括自己脚下)
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector2Int checkPos = currentGridPos + new Vector2Int(x, y);

                if (!map.IsValid(checkPos)) continue;

                var node = map.GetNode(checkPos);
                
                // 如果格子里有人
                if (node.IsOccupied && node.Occupant != null)
                {
                    PlayerController player = node.Occupant.GetComponent<PlayerController>();
                    if (player != null)
                    {
                        // 依然遵守游戏规则：如果面具一样，看不见，就不杀
                        if (player.currentMask != this.enemyType)
                        {
                            // todo: GameOver
                            Debug.Log($"Caught by {enemyType}");
                            if (GameManager.Instance != null)
                            {
                                GameManager.Instance.TriggerGameOver(enemyType.ToString());
                            }
                            // player.gameObject.SetActive(false);
                        }
                    }
                }
            }
        }
    }

    // --- 辅助逻辑 ---
    
    // (这里是你之前的 GetMovePattern，保持不变)
    List<Vector2Int> GetMovePattern()
    {
        List<Vector2Int> moves = new List<Vector2Int>();
        switch (enemyType)
        {
            case MaskType.Ox: 
                moves.Add(new Vector2Int(1, 1)); moves.Add(new Vector2Int(1, -1));
                moves.Add(new Vector2Int(-1, 1)); moves.Add(new Vector2Int(-1, -1));
                break;
            case MaskType.Fox: 
                moves.Add(new Vector2Int(1, 2)); moves.Add(new Vector2Int(2, 1));
                moves.Add(new Vector2Int(2, -1)); moves.Add(new Vector2Int(1, -2));
                moves.Add(new Vector2Int(-1, -2)); moves.Add(new Vector2Int(-2, -1));
                moves.Add(new Vector2Int(-2, 1)); moves.Add(new Vector2Int(-1, 2));
                break;
            case MaskType.Hawk: 
                moves.Add(new Vector2Int(0, 2)); moves.Add(new Vector2Int(0, -2));
                moves.Add(new Vector2Int(-2, 0)); moves.Add(new Vector2Int(2, 0));
                break;
            case MaskType.Dragon:
                moves.Add(Vector2Int.up); moves.Add(Vector2Int.down);
                moves.Add(Vector2Int.left); moves.Add(Vector2Int.right);
                moves.Add(new Vector2Int(1, 1)); moves.Add(new Vector2Int(1, -1));
                moves.Add(new Vector2Int(-1, 1)); moves.Add(new Vector2Int(-1, -1));
                break;
            default: // Turtle/Normal
                moves.Add(Vector2Int.up); moves.Add(Vector2Int.down);
                moves.Add(Vector2Int.left); moves.Add(Vector2Int.right);
                break;
        }
        return moves;
    }

    bool CanMoveTo(Vector2Int targetPos)
    {
        var map = TileManager.Instance.GameMap;
        if (!map.IsValid(targetPos)) return false;
        var node = map.GetNode(targetPos);

        // 撞墙/撞深渊
        if (node.Type == TileType.Wall || node.Type == TileType.Void) return false;

        // 撞人/撞怪
        if (node.IsOccupied)
        {
            // 在新的逻辑里，即使是可以杀的玩家，我们也不走过去“踩”他
            // 而是走到他旁边把他“砍”死，或者单纯把玩家当障碍物
            // 这样避免两人重叠在一个格子的渲染问题
            return false; 
        }

        return true;
    }

    void MoveTo(Vector2Int targetPos)
    {
        var map = TileManager.Instance.GameMap;

        // 1. 计算方向并旋转
        Vector2Int dir = targetPos - currentGridPos;
        UpdateRotation(dir);

        // 2. 移动数据更新
        UnregisterPosition(currentGridPos);
        currentGridPos = targetPos;
        RegisterPosition(currentGridPos);

        // 3. 物理位移
        Vector3 targetWorldPos = map.GridToWorldPos(currentGridPos);
        StartMoveAnimation(targetWorldPos);
        if (moveVisuals != null)
        {
            moveVisuals.Play(enemyType, targetWorldPos - moveStartWorldPos);
        }
    }

    void StartMoveAnimation(Vector3 targetWorldPos)
    {
        if (moveAnimationDuration <= 0f)
        {
            transform.position = targetWorldPos;
            isMoveAnimating = false;
            return;
        }

        moveStartWorldPos = transform.position;
        moveTargetWorldPos = targetWorldPos;
        moveAnimationTimer = 0f;
        isMoveAnimating = true;
    }

    void UpdateMoveAnimation()
    {
        if (!isMoveAnimating) return;

        moveAnimationTimer += Time.deltaTime;
        float t = Mathf.Clamp01(moveAnimationTimer / moveAnimationDuration);
        t = t * t * (3f - 2f * t);
        transform.position = Vector3.LerpUnclamped(moveStartWorldPos, moveTargetWorldPos, t);

        if (t >= 1f)
        {
            transform.position = moveTargetWorldPos;
            isMoveAnimating = false;
        }
    }

    void RegisterPosition(Vector2Int pos)
    {
        var map = TileManager.Instance.GameMap;
        var node = map.GetNode(pos);
        node.IsOccupied = true;
        node.Occupant = this.gameObject;
        map.SetNode(pos, node);
    }

    void UnregisterPosition(Vector2Int pos)
    {
        var map = TileManager.Instance.GameMap;
        var node = map.GetNode(pos);
        if (node.Occupant == this.gameObject)
        {
            node.IsOccupied = false;
            node.Occupant = null;
            map.SetNode(pos, node);
        }
    }
}