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
    [SerializeField] private LayerMask groundLayer;   // 타일 레이어 설정 필요
    [SerializeField] private float castDistance = 0.1f; // 지면 감지 거리
    [SerializeField] private Vector2 boxSize = new Vector2(0.5f, 0.1f); // 감지 영역 크기
    
    private Rigidbody2D _rb;
    private float _horizontalInput;
    private Vector3 _initialScale; // 플레이어의 초기 크기 저장
    private bool _isGrounded; //점프를 위한 지면감지


    /// <summary>
    /// 플레이어가 현재 이동 입력을 주거나 공중에 떠있는지(조작 중인지) 여부
    /// </summary>
    public bool IsInputting => 
        _horizontalInput != 0 || 
        Input.GetKey(KeyCode.LeftArrow) || 
        Input.GetKey(KeyCode.RightArrow) || 
        Input.GetKey(KeyCode.A) || 
        Input.GetKey(KeyCode.D) || 
        !_isGrounded;

    //외부에서 읽을 수 있는 플레이어 최고 높이
    public float MaxHeightInAir { get; private set; }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _initialScale = transform.localScale; // 시작 시 인스펙터에 설정된 크기 저장
    }

    private void Update()
    {
        // 타일 드래그 중에는 모든 조작 차단
        TileInputHandler inputHandler = FindFirstObjectByType<TileInputHandler>();
        if (inputHandler != null && inputHandler.IsDragging)
        {
            _horizontalInput = 0;
            _rb.linearVelocity = new Vector2(0, _rb.linearVelocity.y);
            return;
        }

        // 1. 좌우 입력 감지
        _horizontalInput = Input.GetAxisRaw("Horizontal");

        // 2. 현재 위치한 타일 확인 및 중력 처리
        UpdateGravityBasedOnTile();

        // 3. 지면 확인 및 점프 입력
        CheckGrounded();
        if (Input.GetButtonDown("Jump") && _isGrounded)
        {
            Jump();
        }

        //UpdateMaxHeight();
    }

    private void FixedUpdate()
    {
        // 3. 물리적 이동 처리
        MovePlayer();
    }

    private void MovePlayer()
    {
        _rb.linearVelocity = new Vector2(_horizontalInput * moveSpeed, _rb.linearVelocity.y);
    }

    private void UpdateGravityBasedOnTile()
    {
        // 현재 위치의 그리드 좌표와 타일 가져오기
        Vector2Int gridPos = GridManager.Instance.WorldToGrid(transform.position);
        Tile currentTile = GridManager.Instance.GetTileAt(gridPos);

        if (currentTile != null)
        {
            // 타일의 InvertGravity 값에 따라 Rigidbody2D의 중력 스케일 조절
            float targetGravity = currentTile.InvertGravity ? -1f : 1f;
            _rb.gravityScale = targetGravity;

            // 초기 크기를 유지하면서 Y축만 반전
            float flipY = currentTile.InvertGravity ? -_initialScale.y : _initialScale.y;
            transform.localScale = new Vector3(_initialScale.x, flipY, _initialScale.z);
        }
    }

    //점프를 위한 지면 감지
    private void CheckGrounded()
    {
        // 중력이 양수면 아래(-1), 음수면 위(1) 방향으로 레이캐스트 발사
        float direction = _rb.gravityScale > 0 ? -1f : 1f;
        RaycastHit2D hit = Physics2D.BoxCast(transform.position, boxSize, 0f, Vector2.up * direction, castDistance, groundLayer);
        
        _isGrounded = hit.collider != null;
    }

    private void Jump()
    {
        // 중력 방향의 반대 방향으로 힘을 가함
        float jumpDirection = _rb.gravityScale > 0 ? 1f : -1f;
        
        // 기존 수직 속도를 초기화하여 중첩 점프 힘 방지
        _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, 0);
        _rb.AddForce(Vector2.up * jumpDirection * jumpForce, ForceMode2D.Impulse);
    }

    //지면 체크 범위 기즈모
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
        // 충돌한 오브젝트에 Key 컴포넌트가 있는지 확인
        if (other.TryGetComponent<Key>(out Key key))
        {
            // KeyManager에 획득 알림
            KeyManager.Instance.OnKeyCollected(key.KeyID);
            
            // 열쇠 오브젝트 파괴
            Destroy(other.gameObject);
            Debug.Log($"{key.KeyID}번 열쇠를 획득했습니다!");
        }
    }
}
