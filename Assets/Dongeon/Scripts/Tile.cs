using UnityEngine;

public enum TileType { Normal, Fixed, KeyLocked }

public class Tile : MonoBehaviour
{
    [Header("Tile Settings")]
    [SerializeField] private TileType tileType = TileType.Normal;
    [SerializeField] private int lockID = 0; // 열쇠와 매칭될 고유 번호
    [SerializeField] private bool isLocked = false; // 기본적으로 잠기지 않은 상태
    [SerializeField] private bool isOccupiedByPlayer;

    [Header("Visuals (For Player)")]
    [SerializeField] private Color lockedColor = new Color(1f, 0.8f, 0.8f); // 연한 분홍색 (잠김)
    [SerializeField] private Color unlockedColor = Color.white;           // 흰색 (해제)
    [SerializeField] private SpriteRenderer borderRenderer;

    public int LockID => lockID;
    public TileType Type => tileType;
    public Vector2Int GridPosition { get; set; }
    public bool IsOccupiedByPlayer { get => isOccupiedByPlayer; set => isOccupiedByPlayer = value; }
    
    [SerializeField] private bool invertGravity;
    public bool InvertGravity => invertGravity;

    /// <summary>
    /// 드래그 가능 여부: 일반 타일이거나, 잠긴 타일인데 잠금이 풀렸을 때만 가능 (Fixed는 불가)
    /// </summary>
    public bool CanMove => (tileType == TileType.Normal || (tileType == TileType.KeyLocked && !isLocked)) && !isOccupiedByPlayer;

    private void Start()
    {
        GridPosition = GridManager.Instance.WorldToGrid(transform.position);
        GridManager.Instance.RegisterTile(this);
        transform.position = GridManager.Instance.GridToWorld(GridPosition);
        
        UpdateState();
    }

    /// <summary>
    /// 외부(열쇠 시스템)에서 호출하여 잠금을 해제함
    /// </summary>
    public void Unlock()
    {
        isLocked = false;
        UpdateState();
        Debug.Log($"{lockID}번 타일 잠금 해제!");
    }

    private void UpdateState()
    {
        // Normal 타입은 항상 잠기지 않은 상태(isLocked = false)로 간주
        if (tileType == TileType.Normal) isLocked = false;

        // 1. 물리 차단: 잠긴 상태(KeyLocked && isLocked)면 IsTrigger를 꺼서 벽으로 만듦
        // 잠기지 않았거나 일반 타일이면 IsTrigger를 켜서 진입 가능하게 함
        GetComponent<BoxCollider2D>().isTrigger = (tileType != TileType.KeyLocked || !isLocked);
        
        // 2. 시각화: 잠금 여부에 따라 지정된 색상 적용
        if (borderRenderer != null)
        {
            borderRenderer.color = isLocked ? lockedColor : unlockedColor;
        }
    }

    public void SetGridPosition(Vector2Int newPos)
    {
        GridPosition = newPos;
        transform.position = GridManager.Instance.GridToWorld(newPos);
    }

    private void OnTriggerEnter2D(Collider2D other) { if (other.CompareTag("Player")) isOccupiedByPlayer = true; }
    private void OnTriggerExit2D(Collider2D other) { if (other.CompareTag("Player")) isOccupiedByPlayer = false; }

    private void OnValidate() 
    { 
        // 에디터에서 Normal 타입으로 변경하면 즉시 잠금 해제 상태로 보이게 함
        if (tileType == TileType.Normal) isLocked = false;

        if (borderRenderer != null) 
        {
            borderRenderer.color = isLocked ? lockedColor : unlockedColor;
        }
    }
}
