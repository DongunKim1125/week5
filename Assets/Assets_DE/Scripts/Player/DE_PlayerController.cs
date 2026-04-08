using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // === 컴포넌트 ===
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    
    // === 이동 관련 ===
    private Vector2 moveInput;
    
    [Header("플레이어 이동")]
    [SerializeField] private float moveSpeed = 20f;
    [SerializeField] private Transform respawnTile;
    
    // === 현재 타일 추적 ===
    private TempTile currentTile;
    private float fallCheckInterval = 0.1f;
    private float fallCheckTimer = 0f;
    
    [Header("낙사 감지")]
    [SerializeField] private float fallDetectionRadius = 0.3f;
    
    // === 상태 ===
    private bool isAlive = true;
    private Vector2 lastSafePosition;
    
    // === 중력 ===
    private float gravityScale = 1f;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        
        rb.gravityScale = 1f;
        rb.freezeRotation = true;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        UpdateCurrentTile();
    }
    
    void Update()
    {
        if (!isAlive) return;
        
        HandleInput();
        
        // 일정 간격으로만 타일 체크
        fallCheckTimer += Time.deltaTime;
        if (fallCheckTimer >= fallCheckInterval)
        {
            UpdateCurrentTile();
            CheckFallOffTile();
            fallCheckTimer = 0f;
        }
    }
    
    void FixedUpdate()
    {
        if (!isAlive) return;
        
        ApplyMovement();
    }
    
    void HandleInput()
    {
        // WASD 입력
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        moveInput = new Vector2(horizontal, vertical).normalized;
    }
    
    void ApplyMovement()
    {
        float targetVelocityX = moveInput.x * moveSpeed;
        rb.linearVelocity = new Vector2(targetVelocityX, rb.linearVelocity.y);
    }
    
    void UpdateCurrentTile()
    {
        // 레이어 "Tile"로 타일 감지
        Collider2D[] colliders = Physics2D.OverlapCircleAll(
            (Vector2)transform.position,
            fallDetectionRadius,
            LayerMask.GetMask("Tile")
        );
        
        TempTile newTile = null;
        
        // 가장 가까운 타일 찾기
        if (colliders.Length > 0)
        {
            newTile = colliders[0].GetComponent<TempTile>();
        }
        
        if (newTile != currentTile)
        {
            if (currentTile != null)
                OnTileExit(currentTile);
            
            currentTile = newTile;
            
            if (currentTile != null)
            {
                OnTileEnter(currentTile);
                lastSafePosition = transform.position;
            }
        }
    }
    
    void CheckFallOffTile()
    {
        if (currentTile == null)
        {
            // 타일이 없으면 즉사
            Debug.Log("타일 없음 - 즉사");
            Die();
            return;
        }
        
        
        if (!currentTile.IsPositionInside((Vector2)transform.position))
        {
            Debug.Log("타일 범위 벗어남 - 인접 타일 확인 중");
            
            // 인접 타일 중 현재 위치가 포함된 타일이 있는가?
            TempTile[] adjacentTiles = currentTile.GetAdjacentTiles();
            bool foundSafeTile = false;
            
            foreach (var tile in adjacentTiles)
            {
                if (tile.IsPositionInside((Vector2)transform.position))
                {
                    Debug.Log($"인접 타일 찾음: ({tile.gridX}, {tile.gridY})");
                    currentTile = tile;
                    foundSafeTile = true;
                    lastSafePosition = transform.position;
                    break;
                }
            }
            
            if (!foundSafeTile)
            {
                Debug.Log("안전한 타일 없음 - 즉사");
                Die();
            }
        }
    }
    
    void OnTileEnter(TempTile tile)
    {
        Debug.Log($"타일 진입: ({tile.gridX}, {tile.gridY})");
        
        // 타일 속성 적용
        if (tile.HasInvertedGravity)
        {
            rb.gravityScale = -1f;
            Debug.Log("중력 반전");
        }
        else
        {
            rb.gravityScale = 1f;
        }
        
        // 타일이 잠김 확인
        if (tile.IsLocked)
        {
            Debug.Log("타일은 잠겨있음");
        }
    }
    
    void OnTileExit(TempTile tile)
    {
        Debug.Log($"타일 퇴출: ({tile.gridX}, {tile.gridY})");
    }
    
    void Die()
    {
        isAlive = false;
        Debug.Log("플레이어 사망!");
        
        // 시각적 피드백
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
        }
        
        // 리스폰 로직 (나중에 추가)
        Invoke(nameof(Respawn), 1f);
    }
    
    void Respawn()
    {
        isAlive = true;
        transform.position = respawnTile.position;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }
        
        Debug.Log("리스폰!");
    }
    
    // 타일 객체 상호작용용
    public void ApplyImpulse(Vector2 force)
    {
        rb.linearVelocity += force;
        Debug.Log($"임펄스 적용: {force}");
    }
    
    public void SetVelocity(Vector2 velocity)
    {
        rb.linearVelocity = velocity;
    }
    
    public Vector2 GetVelocity() => rb.linearVelocity;
    
    public bool IsAlive => isAlive;
    public TempTile CurrentTile => currentTile;
}