using UnityEngine;

public enum TileType { Normal, Fixed, KeyLocked }

public class Tile : MonoBehaviour
{
    [Header("Tile Settings")]
    [SerializeField] private TileType tileType = TileType.Normal;
    [SerializeField] private int lockID = 0; 
    [SerializeField] private bool isLocked = false; 
    [SerializeField] private bool isOccupiedByPlayer;
    [SerializeField] private bool invertGravity;

    [Header("Visuals (For Player)")]
    [SerializeField] private Color lockedColor = new Color(1f, 0.8f, 0.8f); // 연한 분홍색 (잠김)
    [SerializeField] private Color unlockedColor = Color.white;           // 흰색 (해제/기본)
    
    // 추가: 반중력 타일일 때 보여줄 색상 (예: 연한 파란색)
    [SerializeField] private Color invertGravityColor = new Color(0.8f, 0.8f, 1f); 
    
    [SerializeField] private SpriteRenderer borderRenderer;

    public int LockID => lockID;
    public TileType Type => tileType;
    public Vector2Int GridPosition { get; set; }
    public bool IsOccupiedByPlayer { get => isOccupiedByPlayer; set => isOccupiedByPlayer = value; }
    public bool InvertGravity => invertGravity;

    public bool CanMove => (tileType == TileType.Normal || (tileType == TileType.KeyLocked && !isLocked)) && !isOccupiedByPlayer;

    private void Start()
    {
        GridPosition = GridManager.Instance.WorldToGrid(transform.position);
        GridManager.Instance.RegisterTile(this);
        transform.position = GridManager.Instance.GridToWorld(GridPosition);
        
        UpdateState();
    }

    public void Unlock()
    {
        isLocked = false;
        UpdateState();
        Debug.Log($"{lockID}번 타일 잠금 해제!");
    }

    private void UpdateState()
    {
        if (tileType == TileType.Normal) isLocked = false;

        GetComponent<BoxCollider2D>().isTrigger = (tileType != TileType.KeyLocked || !isLocked);
        
        // 변경점: 시각화 우선순위 로직 적용 (잠김 > 반중력 > 일반)
        if (borderRenderer != null)
        {
            if (isLocked)
            {
                borderRenderer.color = lockedColor;
            }
            else if (invertGravity)
            {
                borderRenderer.color = invertGravityColor;
            }
            else
            {
                borderRenderer.color = unlockedColor;
            }
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
        if (tileType == TileType.Normal) isLocked = false;

        // 에디터에서 값을 수정할 때도 동일한 시각화 우선순위 로직이 적용되도록 수정
        if (borderRenderer != null) 
        {
            if (isLocked)
            {
                borderRenderer.color = lockedColor;
            }
            else if (invertGravity)
            {
                borderRenderer.color = invertGravityColor;
            }
            else
            {
                borderRenderer.color = unlockedColor;
            }
        }
    }
}