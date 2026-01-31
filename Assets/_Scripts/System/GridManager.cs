using UnityEngine;
using UnityEngine.Tilemaps;

public class TileManager : MonoBehaviour
{
    public static TileManager Instance; 

    [Header("Ref")]
    public Tilemap floorMap; 
    public Tilemap wallMap;  
    public float GridLength = 1;
    public GameMap GameMap;

    // --- 把原来的 Start 逻辑全部搬到这里 ---
    void Awake() 
    { 
        Instance = this; 

        // 1. 确保 Tilemap 引用存在
        if (floorMap == null) 
        {
            Debug.LogError("【严重错误】TileManager 上没有绑定 Floor Map！请拖拽 Tilemap。");
            return;
        }

        // 2. Get size
        floorMap.CompressBounds(); 
        BoundsInt bounds = floorMap.cellBounds;

        // 3. create the GameMap
        Vector2Int offset = new Vector2Int(bounds.xMin, bounds.yMin);
        GameMap = new GameMap(bounds.size.x, bounds.size.y, offset);

        GameMap.GridLength = GridLength;
        
        // 4. Iteration
        foreach (var pos in bounds.allPositionsWithin)
        {
            Vector2Int gridPos = new Vector2Int(pos.x, pos.y);
            GridNode node = new GridNode();

            // 5. Assign value
            if (wallMap != null && wallMap.HasTile(pos))
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

        Debug.Log($"地图生成完毕! 大小: {bounds.size.x}x{bounds.size.y}");
    }
    
    // Start 可以删掉，或者留空
    void Start()
    {
    }

    [Header("Debug View")]
    public bool showDebugGrid = true; 

    void OnDrawGizmos()
    {
        if (!Application.isPlaying || !showDebugGrid) return;
        if (GameMap == null) return;

        BoundsInt bounds = floorMap.cellBounds;

        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                GridNode node = GameMap.GetNode(new Vector2Int(x, y));

                if (node.Type == TileType.Void) continue; 

                if (node.Type == TileType.Wall) 
                    Gizmos.color = new Color(1f, 0f, 0f, 0.6f); 
                else if (node.Type == TileType.Floor) 
                    Gizmos.color = new Color(0f, 1f, 0f, 0.3f); 
                else if (node.Type == TileType.Explosion)
                    Gizmos.color = Color.yellow; 

                Vector3 center = new Vector3(x + 0.5f, y + 0.5f, 0);
                Gizmos.DrawCube(center, Vector3.one * 0.9f);
            }
        }
    }
}