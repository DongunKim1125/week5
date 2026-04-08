using UnityEngine;

/// <summary>
/// 마우스 드래그를 통해 타일을 이동시키고 그리드에 스냅시키는 클래스
/// </summary>
[RequireComponent(typeof(Tile))]
public class TileDragger : MonoBehaviour
{
    private Tile _tile;
    private Vector3 _offset;
    private Vector3 _startWorldPosition;
    private Vector2Int _startGridPosition;
    private Camera _mainCamera;
    private bool _isDragging;

    private void Awake()
    {
        _tile = GetComponent<Tile>();
        _mainCamera = Camera.main;
    }

    private void OnMouseDown()
    {
        // 밟고 있거나 잠긴 타일은 이동 불가
        if (!_tile.CanMove) return;

        _isDragging = true;
        _startWorldPosition = transform.position;
        _startGridPosition = _tile.GridPosition;

        // 마우스 클릭 위치와 오브젝트 중심의 차이 계산
        Vector3 mousePos = GetMouseWorldPosition();
        _offset = transform.position - mousePos;
    }

    private void OnMouseDrag()
    {
        if (!_isDragging) return;

        // 마우스 따라 이동
        transform.position = GetMouseWorldPosition() + _offset;
    }

    private void OnMouseUp()
    {
        if (!_isDragging) return;
        _isDragging = false;

        // 현재 마우스 위치를 기반으로 가장 가까운 그리드 좌표 계산
        Vector2Int targetGridPos = GridManager.Instance.WorldToGrid(transform.position);

        // 이동 가능 여부 확인 (범위 안 & 비어있음 & 자기 자신이 원래 있던 자리)
        if (GridManager.Instance.IsEmpty(targetGridPos) || targetGridPos == _startGridPosition)
        {
            // GridManager 데이터 업데이트 및 월드 좌표 스냅
            GridManager.Instance.UpdateTilePosition(_startGridPosition, targetGridPos, _tile);
        }
        else
        {
            // 이동 불가한 자리면 원래 위치로 복귀
            transform.position = _startWorldPosition;
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = -_mainCamera.transform.position.z; // 카메라 거리 보정
        return _mainCamera.ScreenToWorldPoint(mousePos);
    }
}
