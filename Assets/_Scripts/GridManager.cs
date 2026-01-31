using UnityEngine;
using UnityEngine.Tilemaps;

public class TileManager : MonoBehaviour
{
    public static TileManager Instance; // 单例方便调用

    [Header("Remember to give value to these")]
    public Tilemap floorMap; // 地面图层
    public Tilemap wallMap;  // 墙壁图层
    public float GridLength = 1;
    public GameMap GameMap;  // 你的纯数据地图

    void Awake() 
    { 
        Instance = this; 
    }

    void Start()
    {
        // 1. Get size
        floorMap.CompressBounds(); 
        BoundsInt bounds = floorMap.cellBounds;

        // 2. create the GameMap
        Vector2Int offset = new Vector2Int(bounds.xMin, bounds.yMin);
        GameMap = new GameMap(bounds.size.x, bounds.size.y, offset);

        GameMap.GridLength = GridLength;
        // 3. Iteration
        foreach (var pos in bounds.allPositionsWithin)
        {
            Vector2Int gridPos = new Vector2Int(pos.x, pos.y);
            GridNode node = new GridNode();

            // 4. Assign value
            if (wallMap.HasTile(pos))
            {
                node.Type = TileType.Wall;
            }
            else if (floorMap.HasTile(pos))
            {
                node.Type = TileType.Floor;
            }
            else
            {
                node.Type = TileType.Void;
            }

            // Set
            GameMap.SetNode(gridPos, node);
        }

        Debug.Log("Map array generated!");
    }
    
    [Header("Debug View")]
    public bool showDebugGrid = true; // 在Inspector里勾选/取消

    void OnDrawGizmos()
    {
        // 1. 如果没运行，或者开关没开，直接跳过，不费性能
        if (!Application.isPlaying || !showDebugGrid) return;
        if (GameMap == null) return;

        // 如果还没运行或者地图没生成，就不画
        if (GameMap == null || floorMap == null) return;

        // 直接用 floorMap 的边界来遍历（因为我们约定了以它为准）
        BoundsInt bounds = floorMap.cellBounds;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                // 1. 从你的【虚拟数组】里取数据！
                // 这一步很关键：我们不读Tilemap，只读 GameMap，确保看到的是“转化后”的结果
                GridNode node = GameMap.GetNode(new Vector2Int(x, y));

                // 2. 根据类型决定颜色
                if (node.Type == TileType.Void) continue; // 空气不画

                if (node.Type == TileType.Wall) 
                    Gizmos.color = new Color(1f, 0f, 0f, 0.6f); // 红色半透明 = 墙
                else if (node.Type == TileType.Floor) 
                    Gizmos.color = new Color(0f, 1f, 0f, 0.3f); // 绿色半透明 = 地
                else if (node.Type == TileType.Explosion)
                    Gizmos.color = Color.yellow; // 黄色 = 炸弹

                // 3. 画出来
                // 注意：Tilemap坐标 (0,0) 的中心点其实在世界坐标 (0.5, 0.5)
                // 所以我们要 +0.5 让他对齐格子中心
                Vector3 center = new Vector3(x + 0.5f, y + 0.5f, 0);
                
                // 画一个比格子稍微小一点的方块 (0.9)，方便看到缝隙
                Gizmos.DrawCube(center, Vector3.one * 0.9f);
                
                // 如果你想看格子边框，可以解开下面这行
                // Gizmos.DrawWireCube(center, Vector3.one);
            }
        }
    }
}