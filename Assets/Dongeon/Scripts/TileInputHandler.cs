using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 마우스 레이캐스트를 통해 타일을 감지하고 드래그 앤 드롭, 회전, 레이어 우선순위를 처리하는 클래스
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
    [SerializeField] private float dragScaleMultiplier = 1.1f; // 드래그 시 확대 배율
    [SerializeField] private int dragSortingOrderOffset = 100; // 드래그 시 높일 Sorting Order 값

    private Quaternion _targetRotation; // 목표 회전값
    private Vector3 _originalTileScale; // 타일의 원래 크기 저장
    private Dictionary<Renderer, int> _originalSortingOrders = new Dictionary<Renderer, int>(); // 원래 Sorting Order 저장용

    /// <summary>
    /// 현재 타일을 드래그 중인지 여부
    /// </summary>
    public bool IsDragging => _isDragging;

    private void Awake()
    {
        _mainCamera = Camera.main;
    }

    private void Update()
    {
        HandleInput();

        if (_selectedTile != null)
        {
            _selectedTile.transform.rotation = Quaternion.Slerp(
                _selectedTile.transform.rotation, 
                _targetRotation, 
                Time.deltaTime * rotationSpeed
            );
        }

        if (_isDragging && _selectedTile != null)
        {
            HandleRotation();
        }
    }

    private void HandleRotation()
    {
        if (Input.GetKeyDown(KeyCode.Q)) _targetRotation *= Quaternion.Euler(0, 0, 90f);
        else if (Input.GetKeyDown(KeyCode.E)) _targetRotation *= Quaternion.Euler(0, 0, -90f);
    }

    private void HandleInput()
    {
        if (Input.GetMouseButtonDown(0)) TrySelectTile();
        if (_isDragging && _selectedTile != null) DragTile();
        if (Input.GetMouseButtonUp(0) && _isDragging) DropTile();
    }

    private void TrySelectTile()
    {
        // 플레이어가 현재 입력 중(이동, 점프 등)이면 타일 선택 차단
        PlayerController2 player = FindFirstObjectByType<PlayerController2>();
        if (player != null && player.IsInputting)
        {
            Debug.Log("플레이어 조작 중: 타일 이동 불가");
            return;
        }

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
                
                _originalTileScale = _selectedTile.transform.localScale;
                _selectedTile.transform.localScale = _originalTileScale * dragScaleMultiplier;

                _targetRotation = _selectedTile.transform.rotation;
                _offset = _selectedTile.transform.position - (Vector3)mousePos;

                // 1. 드래그 시작 시 자식 콜라이더들 비활성화
                SetChildrenCollidersActive(_selectedTile, false);
                
                // 2. 드래그 시작 시 Sorting Order 올리기
                SetTileSortingOrder(_selectedTile, dragSortingOrderOffset);
                
                Debug.Log($"타일 선택됨: {_selectedTile.name} (우선순위 상향)");
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

        // 1. 드래그 종료 시 자식 콜라이더들 다시 활성화
        SetChildrenCollidersActive(_selectedTile, true);
        
        // 2. 드래그 종료 시 Sorting Order 복구
        ResetTileSortingOrder();

        _selectedTile.transform.localScale = _originalTileScale;

        Vector2Int targetGridPos = GridManager.Instance.WorldToGrid(_selectedTile.transform.position);

        if (GridManager.Instance.IsEmpty(targetGridPos) || targetGridPos == _originalGridPos)
        {
            GridManager.Instance.UpdateTilePosition(_originalGridPos, targetGridPos, _selectedTile);
            _selectedTile.transform.rotation = _targetRotation;
        }
        else
        {
            _selectedTile.transform.position = _originalWorldPos;
        }

        _selectedTile = null;
    }

    private void SetChildrenCollidersActive(Tile tile, bool active)
    {
        Collider2D[] childColliders = tile.GetComponentsInChildren<Collider2D>();
        foreach (var col in childColliders)
        {
            if (col.gameObject != tile.gameObject)
            {
                col.enabled = active;
            }
        }
    }

    /// <summary>
    /// 타일과 모든 자식 렌더러의 Sorting Order를 높임
    /// </summary>
    private void SetTileSortingOrder(Tile tile, int offset)
    {
        _originalSortingOrders.Clear();
        Renderer[] renderers = tile.GetComponentsInChildren<Renderer>(true);
        
        foreach (Renderer r in renderers)
        {
            _originalSortingOrders[r] = r.sortingOrder;
            r.sortingOrder += offset;
        }
    }

    /// <summary>
    /// 타일의 모든 렌더러 Sorting Order를 원래대로 복구
    /// </summary>
    private void ResetTileSortingOrder()
    {
        foreach (var kvp in _originalSortingOrders)
        {
            if (kvp.Key != null)
            {
                kvp.Key.sortingOrder = kvp.Value;
            }
        }
        _originalSortingOrders.Clear();
    }
}
