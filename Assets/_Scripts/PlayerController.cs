using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created 
    private bool CanMove;

    private void Start()
    {
        // 对齐到最近的格子
        transform.position = TileManager.Instance.GameMap.GridToWorldPos(TileManager.Instance.GameMap.WorldToGridPos(transform.position));
    }

    void Update()
    {
        Vector2Int input = Vector2Int.zero;
        
        if (Input.GetKeyDown(KeyCode.W)) input = Vector2Int.up;
        else if (Input.GetKeyDown(KeyCode.S)) input = Vector2Int.down;
        else if (Input.GetKeyDown(KeyCode.A)) input = Vector2Int.left;
        else if (Input.GetKeyDown(KeyCode.D)) input = Vector2Int.right;
        
        if (input != Vector2Int.zero)
        {
            TryMove(input);
        }
    }
    
    void TryMove(Vector2Int direction)
    {
        Vector2Int startGridPos = TileManager.Instance.GameMap.WorldToGridPos(transform.position);
        
        Vector2Int targetGridPos = startGridPos + direction;

        // Fetch data from GameMap
        var targetNode = TileManager.Instance.GameMap.GetNode(targetGridPos);
        
        // Judge if able to move
        if (targetNode.Type == TileType.Wall) return;
        
        // move
        transform.position = TileManager.Instance.GameMap.GridToWorldPos(targetGridPos);
    }
}
