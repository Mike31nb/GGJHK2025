using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PlayerController : MonoBehaviour
{
    [Header("状态 (可在Inspector调试)")]
    public MaskType currentMask = MaskType.None;

    [Header("位置追踪 (不要手动改)")]
    public Vector2Int currentGridPos; // <--- 这就是之前报错缺少的变量
    
    [Header("视觉组件")] // --- 新增 ---
    private SpriteRenderer myRenderer; // 自己的渲染器
    private Sprite defaultSprite;      // 自己原本的图片 (没面具时的样子)

    [Header("移动表现")]
    public float moveAnimationDuration = 0.22f;
    public bool freeMovementMode = false;
    public float freeMoveSpeed = 4f;
    private Vector3 moveStartWorldPos;
    private Vector3 moveTargetWorldPos;
    private float moveAnimationTimer;
    private bool isMoveAnimating;
    private TickMoveVisuals moveVisuals;
    
    // [Header("配置 (请把做好的面具Prefab拖到这里)")]
    // public List<MaskPrefabMapping> maskPrefabs;
    
    // 乌龟的休息标记
    private bool isTurtleResting = false;
    private MoveTargetMarker targetMarker;

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
        moveStartWorldPos = transform.position;
        moveTargetWorldPos = transform.position;
        moveVisuals = GetComponent<TickMoveVisuals>();
        if (moveVisuals == null) moveVisuals = gameObject.AddComponent<TickMoveVisuals>();
        targetMarker = MoveTargetMarker.Create($"{name}_InputTarget", 6);
        
        // 3. 注册占用
        if (!freeMovementMode)
        {
            UpdateMapOccupancy(currentGridPos, currentGridPos);
        }

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
        
        myRenderer = GetComponentInChildren<SpriteRenderer>(); // 获取身上的渲染器
        if (myRenderer != null)
        {
            defaultSprite = myRenderer.sprite; // 记住现在的样子，作为默认皮肤
        }
    }

    void OnDestroy()
    {
        if(TickManager.Instance != null)
            TickManager.Instance.OnPlayerTick -= HandleTickMovement;

        if (targetMarker != null) Destroy(targetMarker.gameObject);
    }

    void Update()
    {
        UpdateMoveAnimation();

        PlayerInputSignal inputSignal = PlayerInputReader.Read();
        if (inputSignal.RuaaaPressed)
        {
            RuaaaBroadcast.Broadcast(transform.position);
        }

        if (GameManager.Instance != null && GameManager.Instance.DebugObserverMode) return;

        // --- Debug: 数字键切换面具 ---
        if (Input.GetKeyDown(KeyCode.Alpha1)) ChangeMask(MaskType.None);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ChangeMask(MaskType.Turtle);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ChangeMask(MaskType.Ox);
        if (Input.GetKeyDown(KeyCode.Alpha4)) ChangeMask(MaskType.Hawk);
        if (Input.GetKeyDown(KeyCode.Alpha5)) ChangeMask(MaskType.Fox);

        if (Input.GetKeyDown(KeyCode.G))
        {
            DropCurrentMask();
        }

        if (freeMovementMode)
        {
            HandleFreeMovement(inputSignal);
            return;
        }

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
        
        // Debug Log (可选)
        // if (rawInputStack.Count > 0) Debug.Log($"Input: {rawInputStack.Count}, Path: {finalPredictedPath.Count}");
    }

    void HandleFreeMovement(PlayerInputSignal inputSignal)
    {
        HidePredictedTarget();
        rawInputStack.Clear();
        finalPredictedPath.Clear();

        Vector3 moveDir = GetFreeMovementDirection(inputSignal);
        float speed = freeMoveSpeed * GetFreeMovementSpeedMultiplier();
        Vector3 nextPosition = transform.position + moveDir * speed * Time.deltaTime;
        if (CanFreeMoveTo(nextPosition))
        {
            transform.position = nextPosition;
        }

        UpdateFreeGridPosition();
    }

    Vector3 GetFreeMovementDirection(PlayerInputSignal inputSignal)
    {
        if (!inputSignal.HasMove)
        {
            return Vector3.zero;
        }

        switch (currentMask)
        {
            case MaskType.Fox:
                return GetFoxFreeDirection(inputSignal);
            case MaskType.Ox:
                return GetOxFreeDirection(inputSignal.DigitalMove);
            default:
                return new Vector3(inputSignal.AnalogMove.x, inputSignal.AnalogMove.y, 0f);
        }
    }

    Vector3 GetFoxFreeDirection(PlayerInputSignal inputSignal)
    {
        Vector2Int first = inputSignal.FirstHeldMove;
        Vector2Int second = inputSignal.SecondHeldMove;

        if (first == Vector2Int.zero)
        {
            return Vector3.zero;
        }

        if (second == Vector2Int.zero || first.x == second.x || first.y == second.y)
        {
            second = GetCounterClockwiseDir(first);
        }

        Vector3 dir = new Vector3(first.x * 2 + second.x, first.y * 2 + second.y, 0f);
        return dir.normalized;
    }

    Vector3 GetOxFreeDirection(Vector2Int input)
    {
        float x = input.x;
        float y = input.y;

        if (x == 0 && y != 0)
        {
            x = y;
        }
        else if (y == 0 && x != 0)
        {
            y = x;
        }

        return new Vector3(x, y, 0f).normalized;
    }

    float GetFreeMovementSpeedMultiplier()
    {
        switch (currentMask)
        {
            case MaskType.Hawk:
                return 2f;
            case MaskType.Turtle:
                return 0.5f;
            default:
                return 1f;
        }
    }

    bool CanFreeMoveTo(Vector3 worldPos)
    {
        if (TileManager.Instance == null || TileManager.Instance.GameMap == null) return true;

        var map = TileManager.Instance.GameMap;
        Vector2Int gridPos = map.WorldToGridPos(worldPos);
        if (!map.IsValid(gridPos)) return false;

        GridNode node = map.GetNode(gridPos);
        if (currentMask != MaskType.Hawk && (node.Type == TileType.Wall || node.Type == TileType.Void)) return false;
        if (node.IsOccupied && node.Occupant != gameObject) return false;
        return true;
    }

    void UpdateFreeGridPosition()
    {
        if (TileManager.Instance == null || TileManager.Instance.GameMap == null) return;

        Vector2Int newGridPos = TileManager.Instance.GameMap.WorldToGridPos(transform.position);
        if (newGridPos != currentGridPos)
        {
            currentGridPos = newGridPos;
        }

        TryCollectMaskAt(currentGridPos);
    }

    // --- 核心：处理输入堆栈逻辑 (已修改) ---
    void AddInput(Vector2Int dir)
    {
        // --- 修改点 1：把 Hawk 移到这里 ---
        // Hawk 虽然走两步，但它是直线的，不需要像 Fox/Ox 那样搞 "W+D" 这种组合键。
        // 所以让它和 Turtle/None 一样：新输入直接清空旧输入，瞬间响应，不需要按两次 D 才能顶掉 W。
        if (currentMask == MaskType.Turtle || currentMask == MaskType.None || currentMask == MaskType.Hawk)
        {
            rawInputStack.Clear();
            rawInputStack.Add(dir);
            RecalculatePath(); // 直接计算
            return;
        }

        // --- 复杂面具 (Ox, Fox) 进入投票池 ---
        Debug.Log($"Input: {dir}");
        
        // --- 修改点 2：反向键逻辑 ---
        // 你的需求：不要点反方向就取消了，而是要“取消旧的+显示新的”
        // 也就是：如果栈里有 W，我按 S，应该把 W 删干净，然后把 S 加进去。
        
        // 这一行代码会把栈里所有和当前按键相反的方向全删掉
        // (例如：输入 Down，把所有的 Up 删掉)
        rawInputStack.RemoveAll(x => x == -dir);
        
        // 注意：这里不再 return 了！删完旧的，继续往下把新的加进去！
        
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
            HidePredictedTarget();
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
        ShowPredictedTarget();
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
        if (freeMovementMode) return;

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
        HidePredictedTarget(); 
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
        TryCollectMaskAt(targetPos);

        // 4. 执行移动
        UpdateMapOccupancy(currentGridPos, targetPos);
        currentGridPos = targetPos;
        
        // 视觉优化：如果是鹰停在墙上，稍微抬高一点点，感觉像是站在墙头
        Vector3 worldPos = map.GridToWorldPos(targetPos);
        if (currentMask == MaskType.Hawk && targetNode.Type == TileType.Wall)
        {
            worldPos.y += 0.2f; // 视觉上站高一点
        }
        StartMoveAnimation(worldPos);
        if (moveVisuals != null)
        {
            moveVisuals.Play(currentMask, worldPos - moveStartWorldPos);
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

    void ChangeMask(MaskType newMask)
    {
        currentMask = newMask;
        isTurtleResting = false;
        // 如果输入栈里还有东西，因为换面具了，可能需要重新计算一下路径合法性，或者直接清空
        rawInputStack.Clear();
        RecalculatePath();
        Debug.Log($"Mask Switched: {newMask}");
    }

    void TryCollectMaskAt(Vector2Int gridPos)
    {
        if (TileManager.Instance == null || TileManager.Instance.GameMap == null) return;

        var map = TileManager.Instance.GameMap;
        if (!map.IsValid(gridPos)) return;

        var node = map.GetNode(gridPos);
        if (node.Collectible == null) return;

        var itemScript = node.Collectible.GetComponent<Mask>();
        if (itemScript == null) return;

        var itemRenderer = node.Collectible.GetComponentInChildren<SpriteRenderer>();
        if (itemRenderer != null && myRenderer != null)
        {
            myRenderer.sprite = itemRenderer.sprite;
            myRenderer.color = itemRenderer.color;
        }

        ChangeMask(itemScript.maskType);
        Destroy(node.Collectible);

        node.Collectible = null;
        map.SetNode(gridPos, node);
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
    void HidePredictedTarget()
    {
        if (targetMarker != null) targetMarker.Hide();
    }

    void ShowPredictedTarget()
    {
        if (targetMarker == null || finalPredictedPath.Count == 0 || TileManager.Instance == null) return;

        Vector2Int totalDelta = Vector2Int.zero;
        foreach (var step in finalPredictedPath)
        {
            totalDelta += step;
        }

        Vector2Int targetPos = currentGridPos + totalDelta;
        var map = TileManager.Instance.GameMap;
        if (!map.IsValid(targetPos))
        {
            HidePredictedTarget();
            return;
        }

        targetMarker.Show(map.GridToWorldPos(targetPos), new Color(0.25f, 1f, 0.35f, 0.85f), 0.8f);
    }
    
    void DropCurrentMask()
    {
        if (currentMask == MaskType.None) return; // 没面具不能丢
        
        // Masks are destroyed. you cannot leave them on ground.
        ChangeMask(MaskType.None);
    }
    
    // 在 PlayerController 类里加入这个 getter
    public List<Vector2Int> GetCurrentPath()
    {
        return finalPredictedPath;
    }
}