using UnityEngine;

public class Mask : MonoBehaviour
{
    public MaskType maskType; // 在Inspector里选，比如选 Fox

    void Start()
    {
        // 游戏开始时，自动把自己注册进地图数据里
        Vector2Int gridPos = TileManager.Instance.GameMap.WorldToGridPos(transform.position);
        
        // 修正位置对齐格子
        transform.position = TileManager.Instance.GameMap.GridToWorldPos(gridPos);

        // 写入数据层
        var node = TileManager.Instance.GameMap.GetNode(gridPos);
        node.Collectible = this.gameObject;
        TileManager.Instance.GameMap.SetNode(gridPos, node);
    }
}