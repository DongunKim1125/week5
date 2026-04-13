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
    public int Width => width;
    [SerializeField] private int height = 4;
    public int Height => height;
    [SerializeField] private float cellSize = 1.0f; // 타일 한 칸의 크기
    
    [Header("Backdrop Settings")]
    [SerializeField] private bool showBackdrop = true; // 배경 표시 여부
    
    [Tooltip("Assets에서 생성한 Square 스프라이트를 여기에 끌어다 넣으세요.")]
    [SerializeField] private Sprite squareSprite;
    
    [Tooltip("바깥쪽 여백 배경의 색상과 투명도 (기본값: 매우 옅은 흰색)")]
    [SerializeField] private Color backdropColor = new Color(1f, 1f, 1f, 0.05f);


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
        
        if (showBackdrop) 
        {
            CreateGridBackdrop();
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
    /// 세이브 포인트를 위해 전체 타일을 지우고 새로운 상태로 복구합니다.
    /// </summary>
    public void RestoreTileState(Dictionary<Tile, Vector2Int> savedState)
    {
        tileGrid.Clear();
        foreach (var kvp in savedState)
        {
            Tile tile = kvp.Key;
            Vector2Int pos = kvp.Value;
            tileGrid[pos] = tile;
            tile.SetGridPosition(pos);
        }
    }

    /// <summary>
    /// 두 좌표에 있는 타일의 위치를 서로 맞바꿈 (데이터 및 월드 좌표 동기화)
    /// </summary>
    public void SwapTiles(Vector2Int posA, Vector2Int posB)
    {
        Tile tileA = GetTileAt(posA);
        Tile tileB = GetTileAt(posB);

        // 그리드 데이터 교체
        if (tileA != null) tileGrid[posB] = tileA;
        else tileGrid.Remove(posB);

        if (tileB != null) tileGrid[posA] = tileB;
        else tileGrid.Remove(posA);

        // 실제 타일 객체의 데이터 및 위치 업데이트
        tileA?.SetGridPosition(posB);
        tileB?.SetGridPosition(posA);
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
    
    private void CreateGridBackdrop()
    {
        if (squareSprite == null)
        {
            Debug.LogWarning("GridManager: squareSprite가 할당되지 않아 배경을 생성할 수 없습니다.");
            return;
        }

        // 1. 스프라이트의 실제 월드 단위 크기 및 피벗(Pivot) 오프셋 구하기
        // 이렇게 하면 이미지가 어떤 PPU나 Pivot 설정을 가지고 있든 정확한 보정값을 얻을 수 있습니다.
        float spriteWidth = squareSprite.bounds.size.x;
        float spriteHeight = squareSprite.bounds.size.y;
        Vector3 pivotOffset = squareSprite.bounds.center; // 피벗 위치와 실제 중심점의 차이

        if (spriteWidth == 0 || spriteHeight == 0) return;

        // 2. 그리드의 정중앙 로컬 좌표 계산
        float gridCenterX = (width - 1) * cellSize * 0.5f;
        float gridCenterY = (height - 1) * cellSize * 0.5f;

        // ==========================================
        // 3. 마스크 생성 (정확히 그리드 크기만큼)
        // ==========================================
        GameObject maskObj = new GameObject("GridMask_Auto");
        maskObj.transform.SetParent(transform);

        // 목표 크기 (그리드 전체 가로/세로 길이)
        float targetWidth = width * cellSize;
        float targetHeight = height * cellSize;
        
        // 스프라이트 고유 크기에 맞춰 스케일 보정
        Vector3 maskScale = new Vector3(targetWidth / spriteWidth, targetHeight / spriteHeight, 1f);
        
        // 피벗이 Center가 아니더라도 정확히 그리드 중앙에 오도록 위치 보정 적용
        maskObj.transform.localPosition = new Vector3(
            gridCenterX - (pivotOffset.x * maskScale.x),
            gridCenterY - (pivotOffset.y * maskScale.y),
            5f
        );
        maskObj.transform.localScale = maskScale;
        
        SpriteMask mask = maskObj.AddComponent<SpriteMask>();
        mask.sprite = squareSprite;

        // ==========================================
        // 4. 거대한 반투명 배경 생성 (화면 전체 덮기)
        // ==========================================
        GameObject backdropObj = new GameObject("GridBackdrop_Auto");
        backdropObj.transform.SetParent(transform);

        // 화면을 덮을 만큼 거대하게 스케일 보정 (마찬가지로 스프라이트 크기에 비례하여 맞춤)
        Vector3 backdropScale = new Vector3(100f / spriteWidth, 100f / spriteHeight, 1f);
        
        backdropObj.transform.localPosition = new Vector3(
            gridCenterX - (pivotOffset.x * backdropScale.x),
            gridCenterY - (pivotOffset.y * backdropScale.y),
            5f
        );
        backdropObj.transform.localScale = backdropScale;

        SpriteRenderer backdropSR = backdropObj.AddComponent<SpriteRenderer>();
        backdropSR.sprite = squareSprite;
        backdropSR.color = backdropColor;
        backdropSR.sortingOrder = -50; 
        backdropSR.maskInteraction = SpriteMaskInteraction.VisibleOutsideMask; 
    }
}
