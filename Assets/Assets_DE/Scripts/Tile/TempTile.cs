using UnityEngine;
using System.Collections.Generic;

// 타일 팀 코드가 완성되면 이 스크립트는 제거됨
public class TempTile : MonoBehaviour
{
    public int gridX = 0;
    public int gridY = 0;
    
    [SerializeField] private bool isLocked = false;
    [SerializeField] private bool hasInvertedGravity = false;
    
    public bool IsLocked => isLocked;
    public bool HasInvertedGravity => hasInvertedGravity;
    
    public Bounds GetBounds()
    {
        var collider = GetComponent<BoxCollider2D>();
        if (collider == null) return new Bounds();
        
        return collider.bounds;
    }
    
    public bool IsPositionInside(Vector2 pos)
    {
        var collider = GetComponent<BoxCollider2D>();
        if (collider == null) return false;
        
        return collider.bounds.Contains(pos);
    }
    
    public TempTile[] GetAdjacentTiles()
    {
        TempTile[] allTiles = FindObjectsOfType<TempTile>();
        List<TempTile> adjacent = new();
        
        foreach (var tile in allTiles)
        {
            if (tile == this) continue;
            
            int distX = Mathf.Abs(tile.gridX - gridX);
            int distY = Mathf.Abs(tile.gridY - gridY);
            
            // 상하좌우 인접만 (대각선 제외)
            if ((distX == 1 && distY == 0) || (distX == 0 && distY == 1))
            {
                adjacent.Add(tile);
            }
        }
        
        return adjacent.ToArray();
    }
    
    // 디버그용
    void OnDrawGizmosSelected()
    {
        var collider = GetComponent<BoxCollider2D>();
        if (collider == null) return;
        
        Gizmos.color = Color.green;
        Vector2 size = collider.bounds.size;
        Vector2 center = collider.bounds.center;
        
        Gizmos.DrawWireCube(center, size);
    }
}