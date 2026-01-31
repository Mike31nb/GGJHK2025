using UnityEngine;

public enum MaskType
{
    None,   // 初始状态，可能只能走普通步
    Turtle, // 只能走直线 (1格/2tick)
    Ox,     // 只能走斜线 (1格)
    Hawk,   // 车：直线走 (2格)
    Fox,    // 马：L型走位 (2+1)
    Dragon, // the overpowered
}

public enum TileType 
{   
    Void = 0,      // Air/nothing
    Floor = 1,     // Floor that pawn can walk on
    Wall = 2,      // pawn cannot pass
    Explosion = 3  // Explosions
}
    
public struct GridNode 
{
    public TileType Type;
    public bool IsOccupied; 
    public GameObject Occupant; // 玩家或敌人
    public GameObject Collectible; // 地上的面具 (Mask)
}

public class GameMap
{
    public float GridLength;
    private GridNode[,] _nodes;
    private Vector2Int _offset;
    public int Width { get; private set; }
    public int Height { get; private set; }
        
        
    // constructor
    public GameMap(int width, int height, Vector2Int originOffset)
    {
        Width = width;
        Height = height;
        _offset = originOffset;
        _nodes = new GridNode[width, height];
    }
        
    public GridNode GetNode(Vector2Int gridPos)
    {
        if (!IsValid(gridPos)) return new GridNode { Type = TileType.Void };
            
        return _nodes[gridPos.x - _offset.x, gridPos.y - _offset.y];
    }
        
    public void SetNode(Vector2Int gridPos, GridNode newNode)
    {
        if (!IsValid(gridPos)) return;
        _nodes[gridPos.x - _offset.x, gridPos.y - _offset.y] = newNode;
        // todo: 向tilemap同步
    }
        
    public bool IsValid(Vector2Int gridPos)
    {
        int arrayX = gridPos.x - _offset.x;
        int arrayY = gridPos.y - _offset.y;
        return arrayX >= 0 && arrayX < Width && arrayY >= 0 && arrayY < Height;
    }
    
    // 只负责把 World Vector3 变成 Grid Vector2Int
    public Vector2Int WorldToGridPos(Vector3 worldPos)
    {
        // 强烈建议用 RoundToInt 而不是 FloorToInt
        // 防止 lerp 导致的 2.99999 被截断成 2
        return new Vector2Int(
            Mathf.RoundToInt(worldPos.x-0.5f), 
            Mathf.RoundToInt(worldPos.y-0.5f)
        );
    }

    public Vector3 GridToWorldPos(Vector2Int gridPos)
    {
        
        // Can handle logic
        return new Vector3(Mathf.RoundToInt(gridPos.x)+0.5f, Mathf.RoundToInt(gridPos.y)+0.5f, 0);
    }
}