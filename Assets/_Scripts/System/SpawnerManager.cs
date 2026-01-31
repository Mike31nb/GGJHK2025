using UnityEngine;
using System.Collections.Generic;

// 定义一个生成事件类，用来在 Inspector 里配置
[System.Serializable]
public class SpawnEvent
{
    [Header("时间：第几个 Tick 生成")]
    public int spawnTick;

    [Header("物品：拖入 Enemy 或 Mask 的 Prefab")]
    public GameObject prefab;

    [Header("地点：世界坐标 (大概位置即可，怪会自动吸附)")]
    public Vector3 globalPosition;

    [Header("预警：提前多少 Tick 显示红框 (0=不预警)")]
    public int warningTicks = 3; 

    // 内部标记，防止重复生成
    [HideInInspector] public bool hasWarned = false;
    [HideInInspector] public bool hasSpawned = false;
}

public class SpawnerManager : MonoBehaviour
{
    public static SpawnerManager Instance;

    [Header("策划配置表 (在这里填时间、地点、怪)")]
    public List<SpawnEvent> levelScript = new List<SpawnEvent>();

    [Header("配置")]
    public GameObject warningPrefab; // 拖入红色的 Warning 方块

    private int currentTick = 0;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // 连接 Tick 系统
        if (TickManager.Instance != null)
            TickManager.Instance.OnEnemyTick += OnTick;
            
        // 游戏开始时，自动按时间排序，这样乱填也不怕
        levelScript.Sort((a, b) => a.spawnTick.CompareTo(b.spawnTick));
    }

    void OnDestroy()
    {
        if (TickManager.Instance != null)
            TickManager.Instance.OnEnemyTick -= OnTick;
    }

    void OnTick()
    {
        currentTick++;

        // 遍历清单
        foreach (var evt in levelScript)
        {
            // 1. 处理预警 (红框)
            if (evt.warningTicks > 0 && !evt.hasWarned)
            {
                // 如果当前时间 >= (生成时间 - 预警时间)，就开始报警
                if (currentTick >= evt.spawnTick - evt.warningTicks)
                {
                    ShowWarning(evt);
                    evt.hasWarned = true;
                }
            }

            // 2. 处理生成 (实体)
            if (!evt.hasSpawned && currentTick >= evt.spawnTick)
            {
                SpawnObject(evt);
                evt.hasSpawned = true;
            }
        }
    }

    void ShowWarning(SpawnEvent evt)
    {
        if (warningPrefab == null) return;

        // 【关键逻辑】红框是个“死物体”，没有挂脚本，不会自己吸附网格
        // 所以我们必须在这里帮它算一下居中位置，否则它会歪
        Vector3 alignedPos = evt.globalPosition;
        if (TileManager.Instance != null)
        {
            Vector2Int gridPos = TileManager.Instance.GameMap.WorldToGridPos(evt.globalPosition);
            alignedPos = TileManager.Instance.GameMap.GridToWorldPos(gridPos);
        }

        // 稍微调整一下 Z 或 Y，防止和地板 Z-Fighting (闪烁)
        // 假设地板是 Z=0，红框设为 Z=-0.1 浮在地板上
        alignedPos.z = -0.1f; 

        // 生成红框
        GameObject warning = Instantiate(warningPrefab, alignedPos, Quaternion.identity);
        
        // 计算红框的寿命：(生成Tick - 当前Tick) * 每个Tick的秒数
        // 这样刚好在怪刷出来的那一瞬间，红框消失
        float secondsLeft = (evt.spawnTick - currentTick) * TickManager.Instance.timeBetweenTicks;
        Destroy(warning, secondsLeft);
    }

    void SpawnObject(SpawnEvent evt)
    {
        if (evt.prefab == null) return;

        // 【关键逻辑】生成怪的时候，直接用你填的坐标
        // 因为你的 EnemyAI 和 Mask 脚本里写了 Start() -> 自动吸附
        Instantiate(evt.prefab, evt.globalPosition, Quaternion.identity);
        
        Debug.Log($"[Spawner] Tick {currentTick}: 生成了 {evt.prefab.name}");
    }

    // 辅助功能：选中这个物体时，在 Scene 窗口画出所有生成点的位置
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        foreach (var evt in levelScript)
        {
            if (evt.prefab != null)
            {
                Gizmos.DrawWireSphere(evt.globalPosition, 0.4f);
            }
        }
    }
}