using UnityEngine;

/// <summary>
/// Handles player movement and gravity-based orientation.
/// </summary>
public class DE_PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;

    [Header("Knockback Settings")]
    [SerializeField] private float knockbackDecay = 15f;

    [Header("Ground Check Settings")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float castDistance = 0.1f;
    [SerializeField] private Vector2 boxSize = new Vector2(0.5f, 0.1f);

    [Tooltip("절벽에서 떨어진 후 점프가 가능한 유예 시간")]
    [SerializeField] private float coyoteTime = 0.15f; 
    private float _coyoteTimeCounter; 

    private Rigidbody2D _rb;
    private float _horizontalInput;
    private Vector3 _initialScale;
    private bool _isGrounded;

    public bool CanReceiveBounceBonus { get; set; } = true;
    public float DashLockTimer { get; set; } = 0f;
    public float InputLockTimer { get; set; } = 0f;

    private float _externalVelocityX = 0f;
    private DE_PlayerVisuals _visuals; // 추가: 시각 효과 스크립트 연결용 변수

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
        
        _visuals = GetComponentInChildren<DE_PlayerVisuals>();
    }

    private void Update()
    {
        TileInputHandler inputHandler = FindFirstObjectByType<TileInputHandler>();
        if (inputHandler != null && inputHandler.IsDragging)
        {
            _horizontalInput = 0f;
            _rb.linearVelocity = new Vector2(0f, _rb.linearVelocity.y);
            return;
        }

        UpdateGravityBasedOnTile();
        CheckGrounded();

        if (_isGrounded)
        {
            // 땅에 닿아있으면 타이머를 꽉 채워줍니다.
            _coyoteTimeCounter = coyoteTime;
        }
        else
        {
            // 공중에 있으면 타이머를 깎습니다.
            _coyoteTimeCounter -= Time.deltaTime;
        }

        if (InputLockTimer > 0f)
        {
            _horizontalInput = 0f;
            InputLockTimer -= Time.deltaTime;
            return;
        }

        _horizontalInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump") && _coyoteTimeCounter > 0f)
            Jump();
    }

    private void FixedUpdate()
    {
        if (Mathf.Abs(_externalVelocityX) > 0.01f)
        {
            _externalVelocityX = Mathf.MoveTowards(_externalVelocityX, 0f, knockbackDecay * Time.fixedDeltaTime);
        }
        else
        {
            _externalVelocityX = 0f;
        }

        if (DashLockTimer > 0f)
        {
            DashLockTimer -= Time.fixedDeltaTime;
            return;
        }

        MovePlayer();
    }

    private void MovePlayer()
    {
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
        RaycastHit2D[] hits = Physics2D.BoxCastAll(transform.position, boxSize, 0f, Vector2.up * direction, castDistance, groundLayer);

        bool wasGrounded = _isGrounded;
        _isGrounded = hits.Length > 0;

        bool touchedJumpObject = false;
        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i].collider != null && hits[i].collider.GetComponentInParent<JumpObject>() != null)
            {
                touchedJumpObject = true;
                break;
            }
        }

        if (!wasGrounded && _isGrounded && !touchedJumpObject)
        {
            CanReceiveBounceBonus = true;
            _visuals?.TriggerLand();
        }
    }

    private void Jump()
    {
        _coyoteTimeCounter = 0f;
        
        float jumpDirection = _rb.gravityScale > 0 ? 1f : -1f;
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0f);
        _rb.AddForce(Vector2.up * jumpDirection * jumpForce, ForceMode2D.Impulse);
        
        _visuals?.TriggerJump();
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
            Debug.Log($"{key.KeyID} key collected!");
        }
    }

    public void ApplyExternalForce(Vector2 force)
    {
        _rb.linearVelocity = Vector2.zero;
        _externalVelocityX = force.x;
        _rb.AddForce(new Vector2(0f, force.y), ForceMode2D.Impulse);
    }
}
