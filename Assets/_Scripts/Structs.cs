using UnityEngine;

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
    public bool IsOccupied; // If there is enemy/pawn on top
    public GameObject Occupant; // Who is on top
}

public class GameMap
{
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
}