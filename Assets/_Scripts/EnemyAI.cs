using UnityEngine;
using System.Collections.Generic;

public class EnemyAI : MonoBehaviour
{
    [Header("类型")]
    public MaskType enemyType; 
    
    [Header("状态")]
    public Vector2Int currentGridPos;
    private bool isTurtleResting = false; 

    [Header("Sprite设置")]
    // 假设你的美术素材默认是头朝上的，如果默认朝右，这里填 0
    public float spriteDefaultAngle = 90f; 

    void Start()
    {
        // 自动吸附
        if (TileManager.Instance != null)
        {
            currentGridPos = TileManager.Instance.GameMap.WorldToGridPos(transform.position);
            transform.position = TileManager.Instance.GameMap.GridToWorldPos(currentGridPos);
            RegisterPosition(currentGridPos);
        }

        if (TickManager.Instance != null)
            TickManager.Instance.OnEnemyTick += OnTick;
    }

    void OnDestroy()
    {
        if (TickManager.Instance != null)
            TickManager.Instance.OnEnemyTick -= OnTick;
        
        UnregisterPosition(currentGridPos);
    }

    void OnTick()
    {
        // 乌龟休息逻辑
        if (enemyType == MaskType.Turtle)
        {
            if (isTurtleResting) { isTurtleResting = false; return; }
        }

        // 1. 移动逻辑
        PerformMove();

        // 2. 移动完后，立刻检测周围有没有倒霉蛋 (AOE击杀)
        CheckSurroundingKills();

        // 乌龟状态更新
        if (enemyType == MaskType.Turtle) isTurtleResting = true;
    }

    void PerformMove()
    {
        List<Vector2Int> candidates = GetMovePattern();
        List<Vector2Int> validMoves = new List<Vector2Int>();
        
        foreach (var moveVec in candidates)
        {
            Vector2Int targetPos = currentGridPos + moveVec;
            // 现在的 CanMoveTo 只负责检查能不能走（墙、出界、同类）
            // 不再负责击杀，因为击杀逻辑独立出来了
            if (CanMoveTo(targetPos))
            {
                validMoves.Add(targetPos);
            }
        }

        if (validMoves.Count > 0)
        {
            int rnd = Random.Range(0, validMoves.Count);
            MoveTo(validMoves[rnd]);
        }
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
        transform.position = map.GridToWorldPos(currentGridPos);
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