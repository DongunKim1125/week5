using UnityEngine;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// 마우스 레이캐스트를 통해 타일을 감지하고 드래그 앤 드롭, 회전, 레이어 우선순위를 처리하는 클래스
/// </summary>
public class TileInputHandler : MonoBehaviour
{
    private Camera _mainCamera;
    private Tile _selectedTile;
    private Tile _hoveredTile; // 호버 중인 타일
    private Vector3 _hoveredTileOriginalScale; // 호버 전 타일의 원래 크기
    private Vector3 _offset;
    private Vector3 _originalWorldPos;
    private Vector2Int _originalGridPos; 
    private bool _isDragging;

    [Header("Settings")]
    [SerializeField] private LayerMask tileLayer; // 타일을 감지할 레이어
    [SerializeField] private float rotationSpeed = 10f; // 회전 부드러움 속도
    [SerializeField] private float hoverScaleMultiplier = 1.05f; // 마우스 오버 시 확대 배율 (여기서 배율 수정)
    [SerializeField] private float dragScaleMultiplier = 1.1f; // 드래그 시 확대 배율
    [SerializeField] private int dragSortingOrderOffset = 100; // 드래그 시 높일 Sorting Order 값

    [Header("Hint Messages")]
    [Tooltip("힌트 문구에 사용할 폰트 에셋")]
    [SerializeField] private TMP_FontAsset hintFont;
    [Tooltip("플레이어가 타고 있는 타일을 클릭할 때 맰 문구")]
    [SerializeField] private string occupiedTileHint = "플레이어가 있는 타일입니다";
    [Tooltip("고정(Fixed) 타일을 클릭할 때 맰 문구")]
    [SerializeField] private string fixedTileHint = "고정된 타일입니다";
    [Tooltip("플레이어 조작 중일 때 타일을 클릭할 때 맰 문구")]
    [SerializeField] private string playerInputtingHint = "플레이어 이동 중에는 이동할 수 없습니다";
    [Tooltip("힌트 텍스트 색상")]
    [SerializeField] private Color hintColor = new Color(1f, 0.9f, 0.3f);
    [Tooltip("힌트 폰트 크기 (월드 단위)")]
    [SerializeField] private float hintFontSize = 0.35f;
    [Tooltip("힌트가 떠오르는 속도")]
    [SerializeField] private float hintFloatSpeed = 0.4f;
    [Tooltip("힌트가 표시되는 시간 (초)")]
    [SerializeField] private float hintDuration = 3f;
    [Tooltip("힌트 텍스트의 외곽선 두께 (0~1 사이)")]
    [SerializeField] private float hintOutlineWidth = 0.2f; // 새 필드 추가

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
        if (!_isDragging)
        {
            HandleHover();
        }

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

    private void HandleHover()
    {
        // 플레이어가 입력 중이면 호버 해제
        DE_PlayerController player = FindFirstObjectByType<DE_PlayerController>();
        if (player != null && player.IsInputting)
        {
            ClearHover();
            return;
        }

        Tile tileUnderMouse = GetTileUnderMouse();

        if (_hoveredTile != tileUnderMouse)
        {
            ClearHover();

            if (tileUnderMouse != null)
            {
                _hoveredTile = tileUnderMouse;
                _hoveredTileOriginalScale = _hoveredTile.transform.localScale;
                _hoveredTile.transform.localScale = _hoveredTileOriginalScale * hoverScaleMultiplier;
                _hoveredTile.SetHoverState(true); // 외곽선 호버 색상 적용
            }
        }
    }

    private void ClearHover()
    {
        if (_hoveredTile != null)
        {
            _hoveredTile.transform.localScale = _hoveredTileOriginalScale;
            _hoveredTile.SetHoverState(false); // 외곽선 원래대로 복구
            _hoveredTile = null;
        }
    }

    private Tile GetTileUnderMouse()
    {
        // GridManager 기반 감지: 마우스 월드 좌표 → 그리드 좌표 → 해당 타일 반환
        // CanMove인 타일만 반환
        Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int gridPos = GridManager.Instance.WorldToGrid(mouseWorldPos);
        Tile found = GridManager.Instance.GetTileAt(gridPos);

        if (found != null && found.CanMove)
            return found;

        return null;
    }

    /// <summary>
    /// CanMove 여부에 관계없이 마우스 아래 그리드 좌표의 타일을 반환.
    /// 이동 불가 타일 클릭 감지에 사용.
    /// </summary>
    private Tile GetAnyTileUnderMouse()
    {
        Vector3 mouseWorldPos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector2Int gridPos = GridManager.Instance.WorldToGrid(mouseWorldPos);
        return GridManager.Instance.GetTileAt(gridPos);
    }

    private void TrySelectTile()
    {
        DE_PlayerController player = FindFirstObjectByType<DE_PlayerController>();
        bool playerIsInputting = player != null && player.IsInputting;

        if (playerIsInputting)
        {
            // 플레이어 조작 중에도 클릭한 타일에 힌트는 표시함
            Tile clickedTile = GetAnyTileUnderMouse();
            if (clickedTile != null)
            {
                string hint = "";
                if (clickedTile.IsOccupiedByPlayer)
                    hint = occupiedTileHint;
                else if (clickedTile.Type == TileType.Fixed)
                    hint = fixedTileHint;
                else
                    hint = playerInputtingHint; // 조작 중 전용 문구 사용

                if (!string.IsNullOrEmpty(hint))
                {
                    Vector3 spawnPos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
                    spawnPos.z = 0f;
                    spawnPos.y += 0.3f;
                    FloatingText.Show(hint, spawnPos, hintColor, hintFont, hintFontSize, hintFloatSpeed, hintDuration, 0.6f, hintOutlineWidth);
                }
            }
            Debug.Log("플레이어 조작 중: 타일 이동 불가");
            return;
        }

        Tile tile = GetTileUnderMouse();
        
        if (tile != null)
        {
            // 드래그를 시작하기 전에 호버 상태를 초기화하여 크기가 이중으로 커지는 것 방지
            ClearHover();

            _selectedTile = tile;
            _isDragging = true;
            _originalWorldPos = _selectedTile.transform.position;
            _originalGridPos = _selectedTile.GridPosition;
            
            _originalTileScale = _selectedTile.transform.localScale;
            _selectedTile.transform.localScale = _originalTileScale * dragScaleMultiplier;

            _targetRotation = _selectedTile.transform.rotation;
            Vector2 mousePos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            _offset = _selectedTile.transform.position - (Vector3)mousePos;

            // 1. 드래그 시작 시 자식 콜라이더들 비활성화
            SetChildrenCollidersActive(_selectedTile, false);
            
            // 2. 드래그 시작 시 Sorting Order 올리기
            SetTileSortingOrder(_selectedTile, dragSortingOrderOffset);

            // 3. 드래그 외곽선 색상 적용
            _selectedTile.SetDragState(true);
            
            Debug.Log($"타일 선택됨: {_selectedTile.name} (우선순위 상향)");
        }
        else
        {
            // 이동 불가 타일 클릭 시: 이유에 맞는 힌트 문구 표시
            Tile blockedTile = GetAnyTileUnderMouse();
            if (blockedTile != null)
            {
                string hint = "";
                if (blockedTile.IsOccupiedByPlayer)
                    hint = occupiedTileHint;
                else if (blockedTile.Type == TileType.Fixed)
                    hint = fixedTileHint;

                if (!string.IsNullOrEmpty(hint))
                {
                    // 마우스 클릭 위치 기준으로 약간 위에 표시
                    Vector3 spawnPos = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
                    spawnPos.z = 0f;
                    spawnPos.y += 0.3f;
                    FloatingText.Show(hint, spawnPos, hintColor, hintFont, hintFontSize, hintFloatSpeed, hintDuration, 0.6f, hintOutlineWidth);
                }
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

        // 3. 드래그 외곽선 색상 복구
        _selectedTile.SetDragState(false);

        _selectedTile.transform.localScale = _originalTileScale;

        Vector2Int targetGridPos = GridManager.Instance.WorldToGrid(_selectedTile.transform.position);

        if (GridManager.Instance.IsEmpty(targetGridPos) || targetGridPos == _originalGridPos)
        {
            // 빈 칸이거나 제자리인 경우: 일반 이동
            GridManager.Instance.UpdateTilePosition(_originalGridPos, targetGridPos, _selectedTile);
            _selectedTile.transform.rotation = _targetRotation;
        }
        else if (GridManager.Instance.IsInBounds(targetGridPos))
        {
            // 다른 타일이 있는 경우: 교체 가능 여부 확인
            Tile targetTile = GridManager.Instance.GetTileAt(targetGridPos);
            if (targetTile != null && targetTile.CanMove)
            {
                // 상대방도 이동 가능하면 서로 교체
                GridManager.Instance.SwapTiles(_originalGridPos, targetGridPos);
                _selectedTile.transform.rotation = _targetRotation;
                Debug.Log($"타일 위치 교체: {_selectedTile.name} <-> {targetTile.name}");
            }
            else
            {
                // 상대방이 고정되었거나 플레이어가 있으면 복귀
                _selectedTile.transform.position = _originalWorldPos;
                Debug.Log("대상 타일이 고정되어 있거나 플레이어가 있어 교체 불가");
            }
        }
        else
        {
            // 그리드 밖인 경우 복귀
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
