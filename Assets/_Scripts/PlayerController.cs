using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlayerController : MonoBehaviour
{
    [Header("状态 (可在Inspector调试)")]
    public MaskType currentMask = MaskType.None;

    [Header("位置追踪 (不要手动改)")]
    public Vector2Int currentGridPos; // <--- 这就是之前报错缺少的变量
    
    // [Header("配置 (请把做好的面具Prefab拖到这里)")]
    // public List<MaskPrefabMapping> maskPrefabs;
    
    // 乌龟的休息标记
    private bool isTurtleResting = false;

    // --- 新架构核心 ---
    // 1. 玩家实际按下的键（例如：只按了W，这里就是 [Up]）
    private List<Vector2Int> rawInputStack = new List<Vector2Int>();
    
    // 2. 经过面具逻辑自动补全后的最终路径
    private List<Vector2Int> finalPredictedPath = new List<Vector2Int>();

    void Start()
    {
        // 1. 确保 TileManager 存在
        if (TileManager.Instance == null)
        {
            Debug.LogError("【严重错误】场景里没有 TileManager！请创建一个空物体挂上 TileManager 脚本。");
            return;
        }

        // 2. 初始化位置
        currentGridPos = TileManager.Instance.GameMap.WorldToGridPos(transform.position);
        transform.position = TileManager.Instance.GameMap.GridToWorldPos(currentGridPos);
        
        // 3. 注册占用
        UpdateMapOccupancy(currentGridPos, currentGridPos);

        // 4. 【修复】更安全的订阅 Tick
        if (TickManager.Instance == null)
        {
            // 尝试去场景里找一下，防止 Instance 还没赋值
            var foundManager = FindAnyObjectByType<TickManager>();
            if (foundManager != null)
            {
                foundManager.OnPlayerTick += HandleTickMovement;
                Debug.Log("成功连接到 TickManager (通过 Find)");
            }
            else
            {
                Debug.LogError("【严重错误】场景里没有 TickManager！请创建一个空物体挂上 TickManager 脚本，否则无法移动！");
            }
        }
        else
        {
            TickManager.Instance.OnPlayerTick += HandleTickMovement;
            Debug.Log("成功连接到 TickManager (通过 Instance)");
        }
    }

    void OnDestroy()
    {
        if(TickManager.Instance != null)
            TickManager.Instance.OnPlayerTick -= HandleTickMovement;
    }

    void Update()
    {
        // --- Debug: 数字键切换面具 ---
        if (Input.GetKeyDown(KeyCode.Alpha1)) ChangeMask(MaskType.None);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ChangeMask(MaskType.Turtle);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ChangeMask(MaskType.Ox);
        if (Input.GetKeyDown(KeyCode.Alpha4)) ChangeMask(MaskType.Hawk);
        if (Input.GetKeyDown(KeyCode.Alpha5)) ChangeMask(MaskType.Fox);

        // --- 捕获输入 ---
        if (Input.GetKeyDown(KeyCode.W)) AddInput(Vector2Int.up);
        if (Input.GetKeyDown(KeyCode.S)) AddInput(Vector2Int.down);
        if (Input.GetKeyDown(KeyCode.A)) AddInput(Vector2Int.left);
        if (Input.GetKeyDown(KeyCode.D)) AddInput(Vector2Int.right);
        
        // 重置/清除输入
        if (Input.GetKeyDown(KeyCode.Space)) 
        {
            rawInputStack.Clear();
            RecalculatePath();
        }

        // --- 1. 丢弃面具 (按 G) ---
        if (Input.GetKeyDown(KeyCode.G))
        {
            DropCurrentMask();
        }
        
        // Debug Log (可选)
        // if (rawInputStack.Count > 0) Debug.Log($"Input: {rawInputStack.Count}, Path: {finalPredictedPath.Count}");
    }

    // --- 核心：处理输入堆栈逻辑 ---
    void AddInput(Vector2Int dir)
    {
        // --- 安全隔离区 ---
        // 简单面具完全不参与投票逻辑，保证绝对的原汁原味
        if (currentMask == MaskType.Turtle || currentMask == MaskType.None)
        {
            rawInputStack.Clear();
            rawInputStack.Add(dir);
            RecalculatePath(); // 直接计算，不走复杂的投票
            return;
        }

        // --- 复杂面具 (Ox, Fox, Hawk) 进入投票池 ---
        Debug.Log($"Input: {dir}");
        
        // 1. 反向抵消检查 (W + S = 0)
        int cancelIndex = rawInputStack.LastIndexOf(-dir);
        if (cancelIndex != -1)
        {
            rawInputStack.RemoveAt(cancelIndex);
            RecalculatePath();
            return;
        }
        
        // 2. 堆叠逻辑 (最大容量 3)
        if (rawInputStack.Count < 3) 
        {
            rawInputStack.Add(dir);
        }
        else 
        {
            rawInputStack.RemoveAt(0); // 挤掉最早的一个
            rawInputStack.Add(dir);
        }

        RecalculatePath();
    }

    // --- 核心：自动补全与预测 ---
    // 记得在文件最上面加： using System.Linq;

    // --- 核心：民主投票算出主方向 (最终版) ---
    void RecalculatePath()
    {
        finalPredictedPath.Clear();
        
        // 如果没有输入，直接返回
        if (rawInputStack.Count == 0) 
        {
            UpdateUIDirection(finalPredictedPath);
            return;
        }

        // --- 🗳️ 1. 投票统计环节 ---
        
        // 统计每个方向按了几次
        var groups = rawInputStack
            .GroupBy(x => x)
            .Select(g => new { Dir = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count) // 票数多的排前面
            .ToList();

        Vector2Int primary = Vector2Int.zero;
        Vector2Int secondary = Vector2Int.zero;

        // 确定主方向 (票王)
        if (groups.Count > 0)
        {
            int maxVotes = groups[0].Count;
            
            // 如果存在平票 (比如 W:1, A:1)，找出所有的平票候选人
            var topCandidates = groups.Where(g => g.Count == maxVotes).Select(g => g.Dir).ToList();
            
            // 规则：票数一样时，谁最早出现在输入里，谁就是老大 (先入为主)
            // 这样保证 W+A = 上上左，而 A+W = 左左上
            primary = rawInputStack.First(d => topCandidates.Contains(d));
            
            // 确定副方向
            // 如果有第二种按键 (比如 W+A+A，主=A，副=W)，就用它
            var otherGroups = groups.Where(g => g.Dir != primary).ToList();
            if (otherGroups.Count > 0)
            {
                secondary = otherGroups[0].Dir;
            }
            else
            {
                // 如果只按了一种键 (比如 W)，副方向自动补全为逆时针
                secondary = GetCounterClockwiseDir(primary);
            }
        }

        // --- 2. 路径生成环节 ---

        switch (currentMask)
        {
            case MaskType.None:
            case MaskType.Turtle:
                // 简单面具直接走主方向 (其实AddInput里已经拦截了，这里是双重保险)
                finalPredictedPath.Add(primary);
                break;

            case MaskType.Ox: // 斜线 (1+1)
                finalPredictedPath.Add(primary);
                finalPredictedPath.Add(secondary);
                break;

            case MaskType.Hawk: // 直线 (2格)
                finalPredictedPath.Add(primary);
                finalPredictedPath.Add(primary);
                break;

            case MaskType.Fox: // 马步 (2+1)
                // 票王走两步 (长边)
                finalPredictedPath.Add(primary);
                finalPredictedPath.Add(primary);
                // 副手走一步 (短边)
                finalPredictedPath.Add(secondary);
                break;
        }

        UpdateUIDirection(finalPredictedPath);
    }

    // --- 辅助：逆时针计算 (Counter-Clockwise) ---
    // 上 -> 左 -> 下 -> 右 -> 上
    Vector2Int GetCounterClockwiseDir(Vector2Int dir)
    {
        if (dir == Vector2Int.up) return Vector2Int.left;    // W -> A
        if (dir == Vector2Int.left) return Vector2Int.down;  // A -> S
        if (dir == Vector2Int.down) return Vector2Int.right; // S -> D
        if (dir == Vector2Int.right) return Vector2Int.up;   // D -> W
        return Vector2Int.zero;
    }

    // --- Tick 执行 ---
    void HandleTickMovement()
    {
        // 乌龟休息逻辑
        if (currentMask == MaskType.Turtle && isTurtleResting)
        {
            isTurtleResting = false; 
            RecalculatePath();
            return;
        }

        if (finalPredictedPath.Count == 0) return;

        // 计算总位移
        Vector2Int totalDelta = Vector2Int.zero;
        foreach (var step in finalPredictedPath)
        {
            totalDelta += step;
        }

        TryMove(totalDelta);

        // 如果是乌龟，移动完要休息
        if (currentMask == MaskType.Turtle)
        {
            isTurtleResting = true;
        }

        // 移动结束，清空输入
        rawInputStack.Clear();
        finalPredictedPath.Clear();
        UpdateUIDirection(finalPredictedPath); 
    }
    
    // --- 移动与判定 ---
    // --- 更加超模的移动逻辑 ---
    void TryMove(Vector2Int moveVec)
    {
        Vector2Int targetPos = currentGridPos + moveVec;
        var map = TileManager.Instance.GameMap;

        // 0. 首先检查是否飞出地图边界 (就算是鹰也不能飞出世界)
        if (!map.IsValid(targetPos))
        {
            Debug.Log("边界之外！");
            return;
        }

        var targetNode = map.GetNode(targetPos);

        // 1. 核心修改：如果是 Hawk，直接跳过地形检查！
        // 也就是说：鹰可以落在墙上，也可以飞在虚空上
        if (currentMask != MaskType.Hawk)
        {
            if (targetNode.Type == TileType.Wall || targetNode.Type == TileType.Void) 
            {
                Debug.Log("Bonk! (撞墙)");
                return; 
            }
        }

        // 2. 敌人/障碍物碰撞逻辑 (鹰虽然能飞，但如果那位置已经有人了，还是不能重叠)
        if (targetNode.IsOccupied)
        {
            Debug.Log("Blocked by: " + targetNode.Occupant.name);
            // 这里以后可以加：如果是鹰，直接把敌人踩死？(GameJam思路)
            return;
        }

        // 3. 捡面具
        if (targetNode.Collectible != null)
        {
            var itemScript = targetNode.Collectible.GetComponent<Mask>(); // 假设这里改名叫ItemPickup了，如果是Mask请自行修正
            if (itemScript != null)
            {
                ChangeMask(itemScript.maskType); 
                Destroy(targetNode.Collectible);
                
                var node = map.GetNode(targetPos);
                node.Collectible = null;
                map.SetNode(targetPos, node);
            }
        }

        // 4. 执行移动
        UpdateMapOccupancy(currentGridPos, targetPos);
        currentGridPos = targetPos;
        
        // 视觉优化：如果是鹰停在墙上，稍微抬高一点点，感觉像是站在墙头
        Vector3 worldPos = map.GridToWorldPos(targetPos);
        if (currentMask == MaskType.Hawk && targetNode.Type == TileType.Wall)
        {
            worldPos.y += 0.2f; // 视觉上站高一点
        }
        transform.position = worldPos;
    }

    void ChangeMask(MaskType newMask)
    {
        currentMask = newMask;
        isTurtleResting = false;
        // 如果输入栈里还有东西，因为换面具了，可能需要重新计算一下路径合法性，或者直接清空
        rawInputStack.Clear();
        RecalculatePath();
        Debug.Log($"Mask Switched: {newMask}");
    }

    void UpdateMapOccupancy(Vector2Int oldPos, Vector2Int newPos)
    {
        var map = TileManager.Instance.GameMap;

        var oldNode = map.GetNode(oldPos);
        oldNode.IsOccupied = false;
        oldNode.Occupant = null;
        map.SetNode(oldPos, oldNode);

        var newNode = map.GetNode(newPos);
        newNode.IsOccupied = true;
        newNode.Occupant = this.gameObject;
        map.SetNode(newPos, newNode);
    }

    // --- UI 接口 ---
    void UpdateUIDirection(List<Vector2Int> path)
    {
        if (path.Count == 0) return;

        // Debug 画线
        Vector3 start = transform.position;
        foreach(var dir in path)
        {
            Debug.DrawRay(start, new Vector3(dir.x, dir.y, 0), Color.green, 1.0f); // 持续1秒方便看
            start += new Vector3(dir.x, dir.y, 0);
        }
    }
    
    
    void DropCurrentMask()
    {
        if (currentMask == MaskType.None) return; // 没面具不能丢
        
        // Masks are destroyed. you cannot leave them on ground.
        ChangeMask(MaskType.None);
    }
}