using UnityEngine;
using UnityEngine.Tilemaps;

public class TileManager : MonoBehaviour
{
    public Tilemap myTilemap;
    void ScanMap()
    {
        BoundsInt bounds = myTilemap.cellBounds;
        Debug.Log($"--- 开始扫描地图 ---");
        Debug.Log($"地图范围 X: {bounds.xMin} 到 {bounds.xMax}");
        Debug.Log($"地图范围 Y: {bounds.yMin} 到 {bounds.yMax}");
        
        
        for (int x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (int y = bounds.yMin; y < bounds.yMax; y++)
            {
                // 3. 构建当前坐标
                Vector3Int pos = new Vector3Int(x, y, 0);

                // 4. 获取这一格的 Tile
                if (myTilemap.HasTile(pos))
                {
                    TileBase tile = myTilemap.GetTile(pos);
                    
                    // --- 打印数据 ---
                    // 这里你直接能看到 (3, 5) 是 "Block_Red" 还是 "Block_White"
                    Debug.Log($"坐标 [{x}, {y}] : {tile.name}");
                }
            }
        }
    }
    
        
    void Start()
    {
        ScanMap();
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
