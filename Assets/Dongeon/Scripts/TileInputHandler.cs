using UnityEngine;

/// <summary>
/// 마우스 레이캐스트를 통해 타일을 감지하고 드래그 앤 드롭을 처리하는 클래스
/// </summary>
public class TileInputHandler : MonoBehaviour
{
    private Camera _mainCamera;
    private Tile _selectedTile;
    private Vector3 _offset;
    private Vector3 _originalWorldPos;
    private Vector2Int _originalGridPos; 
    private bool _isDragging;

    [Header("Settings")]
    [SerializeField] private LayerMask tileLayer; // 타일을 감지할 레이어 (필요 시)

    private void Awake()
    {
        _mainCamera = Camera.main;
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        // 1. 클릭 시작 (타일 선택)
        if (Input.GetMouseButtonDown(0))
        {
            TrySelectTile();
        }

        // 2. 드래그 중
        if (_isDragging && _selectedTile != null)
        {
            DragTile();
        }

        // 3. 클릭 해제 (타일 놓기)
        if (Input.GetMouseButtonUp(0))
        {
            if (_isDragging)
            {
                DropTile();
            }
        }
    }

    private void TrySelectTile()
    {
        Vector2 mousePos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        
        // 2D 레이캐스트를 쏴서 타일이 있는지 확인
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

        if (hit.collider != null)
        {
            Tile tile = hit.collider.GetComponent<Tile>();
            
            // 타일이 있고, 이동 가능한 상태인지 확인
            if (tile != null && tile.CanMove)
            {
                _selectedTile = tile;
                _isDragging = true;
                _originalWorldPos = _selectedTile.transform.position;
                _originalGridPos = _selectedTile.GridPosition;

                // 마우스 클릭 위치와 타일 중심의 차이(Offset) 계산
                _offset = _selectedTile.transform.position - (Vector3)mousePos;
                
                // 드래그 중에는 플레이어 감지 트리거 등이 오작동하지 않도록 임시 처리 가능
                Debug.Log($"타일 선택됨: {_selectedTile.name}");
            }
        }
    }

    private void DragTile()
    {
        Vector3 mousePos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        _selectedTile.transform.position = mousePos + _offset;
    }

    private void DropTile()
    {
        _isDragging = false;

        // 현재 위치에서 가장 가까운 그리드 좌표 계산
        Vector2Int targetGridPos = GridManager.Instance.WorldToGrid(_selectedTile.transform.position);

        // 이동 가능한 칸인지 확인 (비어있거나 원래 자리거나)
        if (GridManager.Instance.IsEmpty(targetGridPos) || targetGridPos == _originalGridPos)
        {
            GridManager.Instance.UpdateTilePosition(_originalGridPos, targetGridPos, _selectedTile);
            Debug.Log($"타일 배치됨: {targetGridPos}");
        }
        else
        {
            // 이동 불가하면 원래 위치로 복귀
            _selectedTile.transform.position = _originalWorldPos;
            Debug.Log("이동 불가: 원래 위치로 복귀");
        }

        _selectedTile = null;
    }
}
