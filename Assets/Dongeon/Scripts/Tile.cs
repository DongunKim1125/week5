using UnityEngine;

/// <summary>
/// 개별 타일의 상태와 속성을 관리하는 클래스
/// </summary>
public class Tile : MonoBehaviour
{
    [Header("Tile Status")]
    [SerializeField] private Vector2Int gridPosition;
    [SerializeField] private bool isLocked; // 시작/목표 지점 등 이동 불가 여부
    [SerializeField] private bool isOccupiedByPlayer; // 플레이어가 밟고 있는지 여부

    [SerializeField] private bool invertGravity; // 이 타일에 있을 때 중력 반전 여부

    public bool InvertGravity => invertGravity;

    public Vector2Int GridPosition 
    { 
        get => gridPosition; 
        set => gridPosition = value; 
    }

    public bool IsLocked 
    { 
        get => isLocked; 
        set => isLocked = value; 
    }

    public bool IsOccupiedByPlayer 
    { 
        get => isOccupiedByPlayer; 
        set => isOccupiedByPlayer = value; 
    }

    [Header("Tile Dimensions")]
    [SerializeField] private Vector2 tileSize = new Vector2(10f, 10f); // 타일(방)의 월드 크기

    public Vector2 TileSize => tileSize;

    /// <summary>
    /// 현재 타일이 드래그하여 이동 가능한 상태인지 확인
    /// </summary>
    public bool CanMove => !isLocked && !isOccupiedByPlayer;

    private void Start()
    {
        // 게임 시작 시 자신의 초기 위치를 GridManager에 등록
        GridManager.Instance.RegisterTile(this);
    }

    /// <summary>
    /// 타일을 새로운 그리드 좌표로 이동시키고 월드 위치를 업데이트함
    /// </summary>
    public void SetGridPosition(Vector2Int newPos)
    {
        gridPosition = newPos;
        // GridManager의 현재 위치를 반영하여 월드 좌표 업데이트
        transform.position = GridManager.Instance.GridToWorld(newPos);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isOccupiedByPlayer = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isOccupiedByPlayer = false;
        }
    }
}
