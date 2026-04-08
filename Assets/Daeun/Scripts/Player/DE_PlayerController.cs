using UnityEngine;

/// <summary>
/// 플레이어의 좌우 이동과 타일 기반 중력 제어를 담당하는 클래스
/// </summary>
public class DE_PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;

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
            Jump();
    }

    private void FixedUpdate()
    {
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
        _rb.linearVelocity = new Vector2(_horizontalInput * moveSpeed, _rb.linearVelocity.y);
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
            Debug.Log($"{key.KeyID}번 열쇠를 획득했습니다!");
        }
    }
}