using UnityEngine;

/// <summary>
/// 플레이어의 좌우 이동과 타일 기반 중력 제어를 담당하는 클래스
/// </summary>
public class DE_PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;

    [Header("Knockback Settings (신규)")]
    [Tooltip("수평으로 밀려난 힘이 서서히 줄어드는 속도 (공기 저항 역할)")]
    [SerializeField] private float knockbackDecay = 15f;

    [Header("Ground Check Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float castDistance = 0.1f;
    [SerializeField] private Vector2 boxSize = new Vector2(0.5f, 0.1f);

    private Rigidbody2D _rb;
    private float _horizontalInput;
    private Vector3 _initialScale;
    private bool _isGrounded;

    /// <summary>
    /// DashObject가 설정하는 입력 차단 타이머.
    /// 0보다 크면 MovePlayer()가 속도를 덮어쓰지 않아 대쉬 속도가 유지된다.
    /// </summary>
    public float DashLockTimer { get; set; } = 0f;
    
    // 외부에서 받은 수평 속도를 저장할 변수
    private float _externalVelocityX = 0f;

    public bool IsInputting =>
        _horizontalInput != 0 ||
        Input.GetKey(KeyCode.LeftArrow) ||
        Input.GetKey(KeyCode.RightArrow) ||
        Input.GetKey(KeyCode.A) ||
        Input.GetKey(KeyCode.D) ||
        !_isGrounded;

    public float MaxHeightInAir { get; private set; }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _initialScale = transform.localScale;
    }

    private void Update()
    {
        TileInputHandler inputHandler = FindFirstObjectByType<TileInputHandler>();
        if (inputHandler != null && inputHandler.IsDragging)
        {
            _horizontalInput = 0;
            _rb.linearVelocity = new Vector2(0, _rb.linearVelocity.y);
            return;
        }

        _horizontalInput = Input.GetAxisRaw("Horizontal");

        UpdateGravityBasedOnTile();
        CheckGrounded();
        
        if (Input.GetButtonDown("Jump") && _isGrounded)
        {
            Jump();
        }
    }

    private void FixedUpdate()
    {
        // 1. 외부 수평 힘을 서서히 0으로 감소 (마찰력 효과)
        if (Mathf.Abs(_externalVelocityX) > 0.01f)
        {
            // MoveTowards를 사용하여 일정한 속도로 0을 향해 감소시킴
            _externalVelocityX = Mathf.MoveTowards(_externalVelocityX, 0f, knockbackDecay * Time.fixedDeltaTime);
        }
        else
        {
            _externalVelocityX = 0f;
        }

        // 2. 물리적 이동 처리
        
        // 대쉬 잠금 타이머 감산
        if (DashLockTimer > 0f)
        {
            DashLockTimer -= Time.fixedDeltaTime;
            return; // 타이머가 남아있는 동안 속도 덮어쓰기 건너뜀
        }

        MovePlayer();
    }

    private void MovePlayer()
    {
        // 핵심 변경점: 플레이어의 기본 방향키 속도 + 점프대로 인한 외부 속도를 합산
        float finalVelocityX = (_horizontalInput * moveSpeed) + _externalVelocityX;
        
        _rb.linearVelocity = new Vector2(finalVelocityX, _rb.linearVelocity.y);
    }

    private void UpdateGravityBasedOnTile()
    {
        Vector2Int gridPos = GridManager.Instance.WorldToGrid(transform.position);
        Tile currentTile = GridManager.Instance.GetTileAt(gridPos);

        if (currentTile != null)
        {
            float targetGravity = currentTile.InvertGravity ? -1f : 1f;
            _rb.gravityScale = targetGravity;

            float flipY = currentTile.InvertGravity ? -_initialScale.y : _initialScale.y;
            transform.localScale = new Vector3(_initialScale.x, flipY, _initialScale.z);
        }
    }

    private void CheckGrounded()
    {
        float direction = _rb.gravityScale > 0 ? -1f : 1f;
        RaycastHit2D hit = Physics2D.BoxCast(transform.position, boxSize, 0f, Vector2.up * direction, castDistance, groundLayer);
        _isGrounded = hit.collider != null;
    }

    private void Jump()
    {
        float jumpDirection = _rb.gravityScale > 0 ? 1f : -1f;
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0);
        _rb.AddForce(Vector2.up * jumpDirection * jumpForce, ForceMode2D.Impulse);
    }

    private void OnDrawGizmosSelected()
    {
        if (_rb == null) return;
        float direction = _rb.gravityScale > 0 ? -1f : 1f;
        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Vector3 checkPos = transform.position + (Vector3.up * direction * castDistance);
        Gizmos.DrawWireCube(checkPos, boxSize);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<Key>(out Key key))
        {
            KeyManager.Instance.OnKeyCollected(key.KeyID);
            Destroy(other.gameObject);
        }
    }

    /// <summary>
    /// 점프대 등 외부 요인에 의해 플레이어가 날아갈 때 호출
    /// </summary>
    public void ApplyExternalForce(Vector2 force)
    {
        // 1. 궤적을 깔끔하게 만들기 위해 기존 속도 초기화
        _rb.linearVelocity = Vector2.zero;

        // 2. X축(수평) 힘은 따로 변수에 담아 MovePlayer()에서 자연스럽게 섞이고 감소하도록 함
        _externalVelocityX = force.x;

        // 3. Y축(수직) 힘은 유니티 물리엔진(중력)이 자연스럽게 처리하도록 AddForce 적용
        _rb.AddForce(new Vector2(0, force.y), ForceMode2D.Impulse);
    }
}