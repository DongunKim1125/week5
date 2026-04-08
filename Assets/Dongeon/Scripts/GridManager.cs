using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 5x4 그리드 시스템을 관리하며 타일의 위치 데이터를 관리하는 싱글톤 매니저
/// </summary>
public class GridManager : MonoBehaviour
{
    private static GridManager _instance;
    public static GridManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<GridManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("GridManager");
                    _instance = go.AddComponent<GridManager>();
                }
            }
            return _instance;
        }
    }

    [Header("Grid Settings")]
    [SerializeField] private int width = 5;
    [SerializeField] private int height = 4;
    [SerializeField] private float cellSize = 1.0f; // 타일 한 칸의 크기

    // 그리드 좌표별 타일 정보를 담는 딕셔너리
    private Dictionary<Vector2Int, Tile> tileGrid = new Dictionary<Vector2Int, Tile>();

    [Header("Visualization")]
    [SerializeField] private bool showGrid = true;
    [SerializeField] private bool showGridInGame = true; // 게임 뷰 표시 여부
    [SerializeField] private Color gridColor = Color.yellow;
    [SerializeField] private float lineWidth = 0.05f;    // 선 두께

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    private void Start()
    {
        if (showGridInGame)
        {
            CreateGameGrid();
        }
    }

    /// <summary>
    /// 게임 실행 중(Game 뷰)에 보일 그리드 선을 생성함
    /// </summary>
    private void CreateGameGrid()
    {
        GameObject gridContainer = new GameObject("InGameGrid");
        gridContainer.transform.SetParent(transform);
        gridContainer.transform.localPosition = Vector3.zero;

        Vector3 origin = transform.position;
        float startX = origin.x - cellSize * 0.5f;
        float startY = origin.y - cellSize * 0.5f;
        float endX = origin.x + (width - 0.5f) * cellSize;
        float endY = origin.y + (height - 0.5f) * cellSize;

        // 세로선 생성
        for (int x = 0; x <= width; x++)
        {
            float posX = startX + (x * cellSize);
            CreateLine(gridContainer.transform, 
                new Vector3(posX, startY, 0.1f), 
                new Vector3(posX, endY, 0.1f));
        }

        // 가로선 생성
        for (int y = 0; y <= height; y++)
        {
            float posY = startY + (y * cellSize);
            CreateLine(gridContainer.transform, 
                new Vector3(startX, posY, 0.1f), 
                new Vector3(endX, posY, 0.1f));
        }
    }

    private void CreateLine(Transform parent, Vector3 start, Vector3 end)
    {
        GameObject lineObj = new GameObject("GridLine");
        lineObj.transform.SetParent(parent);
        
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = new Material(Shader.Find("Sprites/Default")); // 기본 스프라이트 쉐이더 사용
        lr.startColor = lr.endColor = gridColor;
        lr.startWidth = lr.endWidth = lineWidth;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.useWorldSpace = true;
        lr.sortingOrder = -10; // 타일보다 뒤에 그려지도록 설정
    }

    /// <summary>
    /// 에디터 Scene 뷰에서 그리드 경계선을 시각적으로 표시함
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showGrid) return;

        Gizmos.color = gridColor;

        Vector3 origin = transform.position;

        // 타일의 중심이 기준이므로 경계선은 -0.5칸 위치부터 시작함
        float startX = origin.x - cellSize * 0.5f;
        float startY = origin.y - cellSize * 0.5f;
        float endX = origin.x + (width - 0.5f) * cellSize;
        float endY = origin.y + (height - 0.5f) * cellSize;

        // 세로선 그리기
        for (int x = 0; x <= width; x++)
        {
            float posX = startX + (x * cellSize);
            Gizmos.DrawLine(new Vector3(posX, startY, 0), new Vector3(posX, endY, 0));
        }

        // 가로선 그리기
        for (int y = 0; y <= height; y++)
        {
            float posY = startY + (y * cellSize);
            Gizmos.DrawLine(new Vector3(startX, posY, 0), new Vector3(endX, posY, 0));
        }
    }

    /// <summary>
    /// 특정 좌표에 타일이 있는지 확인하고 해당 타일을 반환함 (없으면 null)
    /// </summary>
    public Tile GetTileAt(Vector2Int pos)
    {
        if (tileGrid.TryGetValue(pos, out Tile tile))
        {
            return tile;
        }
        return null;
    }

    /// <summary>
    /// 타일이 그리드 범위 내에 있는지 확인
    /// </summary>
    public bool IsInBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
    }

    /// <summary>
    /// 월드 좌표를 그리드 좌표로 변환함 (매니저 위치 기준)
    /// </summary>
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        Vector3 localPos = worldPos - transform.position;
        int x = Mathf.RoundToInt(localPos.x / cellSize);
        int y = Mathf.RoundToInt(localPos.y / cellSize);
        return new Vector2Int(x, y);
    }

    /// <summary>
    /// 그리드 좌표를 월드 좌표로 변환함 (매니저 위치 기준)
    /// </summary>
    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        Vector3 localOffset = new Vector3(gridPos.x * cellSize, gridPos.y * cellSize, 0);
        return transform.position + localOffset;
    }

    /// <summary>
    /// 특정 그리드 칸이 비어있는지(타일이 없는지) 확인
    /// </summary>
    public bool IsEmpty(Vector2Int pos)
    {
        return IsInBounds(pos) && !tileGrid.ContainsKey(pos);
    }

    /// <summary>
    /// 타일을 특정 좌표에 등록하거나 위치를 변경함
    /// </summary>
    public void UpdateTilePosition(Vector2Int oldPos, Vector2Int newPos, Tile tile)
    {
        if (tileGrid.ContainsKey(oldPos) && tileGrid[oldPos] == tile)
        {
            tileGrid.Remove(oldPos);
        }
        
        tileGrid[newPos] = tile;
        tile.SetGridPosition(newPos);
    }

    /// <summary>
    /// 타일을 그리드 시스템에 최초 등록함
    /// </summary>
    public void RegisterTile(Tile tile)
    {
        if (IsInBounds(tile.GridPosition))
        {
            tileGrid[tile.GridPosition] = tile;
            tile.SetGridPosition(tile.GridPosition); // 실제 월드 위치 동기화
        }
        else
        {
            Debug.LogWarning($"타일 ({tile.name})이 그리드 범위를 벗어났습니다: {tile.GridPosition}");
        }
    }
}
