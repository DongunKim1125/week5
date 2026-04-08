using UnityEngine;

/// <summary>
/// 마우스 레이캐스트를 통해 타일을 감지하고 드래그 앤 드롭 및 부드러운 회전을 처리하는 클래스
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
    [SerializeField] private LayerMask tileLayer; // 타일을 감지할 레이어
    [SerializeField] private float rotationSpeed = 10f; // 회전 부드러움 속도

    private Quaternion _targetRotation; // 목표 회전값

    private void Awake()
    {
        _mainCamera = Camera.main;
    }

    private void Update()
    {
        HandleInput();

        // 선택된 타일이 있다면 목표 회전값으로 부드럽게 회전시킴
        if (_selectedTile != null)
        {
            _selectedTile.transform.rotation = Quaternion.Slerp(
                _selectedTile.transform.rotation, 
                _targetRotation, 
                Time.deltaTime * rotationSpeed
            );
        }

        // 드래그 중일 때만 회전 입력 감지
        if (_isDragging && _selectedTile != null)
        {
            HandleRotation();
        }
    }

    private void HandleRotation()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            // 왼쪽으로 90도 회전 목표 설정
            _targetRotation *= Quaternion.Euler(0, 0, 90f);
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            // 오른쪽으로 90도 회전 목표 설정
            _targetRotation *= Quaternion.Euler(0, 0, -90f);
        }
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
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

        if (hit.collider != null)
        {
            Tile tile = hit.collider.GetComponent<Tile>();
            
            if (tile != null && tile.CanMove)
            {
                _selectedTile = tile;
                _isDragging = true;
                _originalWorldPos = _selectedTile.transform.position;
                _originalGridPos = _selectedTile.GridPosition;
                
                // 선택한 타일의 현재 회전값을 목표값으로 초기화
                _targetRotation = _selectedTile.transform.rotation;

                _offset = _selectedTile.transform.position - (Vector3)mousePos;
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

        Vector2Int targetGridPos = GridManager.Instance.WorldToGrid(_selectedTile.transform.position);

        if (GridManager.Instance.IsEmpty(targetGridPos) || targetGridPos == _originalGridPos)
        {
            GridManager.Instance.UpdateTilePosition(_originalGridPos, targetGridPos, _selectedTile);
            
            // 타일을 놓을 때 목표 각도로 즉시 고정 (정렬 보장)
            _selectedTile.transform.rotation = _targetRotation;
            Debug.Log($"타일 배치됨: {targetGridPos}");
        }
        else
        {
            _selectedTile.transform.position = _originalWorldPos;
            Debug.Log("이동 불가: 원래 위치로 복귀");
        }

        _selectedTile = null;
    }
}
