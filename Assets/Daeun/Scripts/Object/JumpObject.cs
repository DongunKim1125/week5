using UnityEngine;

/// <summary>
/// 플레이어의 공중 최고점과 타일 크기를 계산하여 특정 높이까지 점프시키는 오브젝트
/// </summary>
public class JumpObject : MonoBehaviour
{
    [Header("Jump Settings")]
    [SerializeField] private Vector2 tileSize = new Vector3(3f, 3f);
    [SerializeField] private LayerMask playerLayer;

    private float _playerMaxY = float.MinValue;
    private bool _hasJumped = false;
    private bool _isMaxYLocked = false; // 높이 계산 고정을 위한 플래그 추가
    private Vector2Int _lastGridPos;
    private Tile _parentTile;

    


    private void Awake()
    {
        _parentTile = GetComponentInParent<Tile>();
        if (_parentTile != null)
        {
            _lastGridPos = _parentTile.GridPosition;
        }
    }

    private void Update()
    {
        // 1. 타일 이동 감지 시 데이터 초기화
        CheckTileMovement();

        // 2. 플레이어가 공중에 있을 때 최고 Y값 갱신
        TrackPlayerMaxY();
    }

    private void CheckTileMovement()
    {
        if (_parentTile != null && _parentTile.GridPosition != _lastGridPos)
        {
            ResetJumpData();
            _lastGridPos = _parentTile.GridPosition;
        }
    }

    private void TrackPlayerMaxY()
    {
        // 이미 최고 높이가 계산되어 고정된 상태라면 갱신하지 않음
        if (_isMaxYLocked) return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        
        if (Mathf.Abs(rb.linearVelocity.y) > 0.1f)
        {
            _playerMaxY = Mathf.Max(_playerMaxY, player.transform.position.y);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_hasJumped) return;

        if (((1 << collision.gameObject.layer) & playerLayer) != 0)
        {
            ExecuteHighJump(collision.gameObject);
        }
    }

    private void ExecuteHighJump(GameObject player)
    {
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb == null) return;

        // 높이가 고정되지 않은 최초 점프 시에만 _playerMaxY를 확정
        // (플레이어가 점프대에 닿기 전에 충분한 낙하를 하지 않았을 경우를 대비한 최소값 방어)
        if (_playerMaxY == float.MinValue)
        {
            _playerMaxY = player.transform.position.y;
        }

        // 1. 목표 높이 계산
        float targetHeightValue = _playerMaxY + (tileSize.y * 0.5f);
        float currentY = player.transform.position.y;
        float displacementY = Mathf.Abs(targetHeightValue - currentY);

        // 2. 필요한 초기 속도 계산
        float gravity = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);
        float requiredVelocityY = Mathf.Sqrt(2 * gravity * displacementY);

        // 3. 중력 방향에 따른 속도 적용
        float direction = rb.gravityScale > 0 ? 1f : -1f;
        
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, requiredVelocityY * direction);

        _hasJumped = true;
        _isMaxYLocked = true; // 이후 점프 시 _playerMaxY가 갱신되지 않도록 잠금

        Debug.Log($"Jump Triggered! Fixed Target Height: {targetHeightValue}");
    }

    private void ResetJumpData()
    {
        _playerMaxY = float.MinValue;
        _hasJumped = false;
        _isMaxYLocked = false; // 타일 이동 시 잠금 해제
        Debug.Log("Tile Moved: Jump Data Reset.");
    }

    // OnCollisionExit2D 대신 OnTriggerExit2D 사용 (OnTriggerEnter2D와 쌍을 맞춤)
    private void OnTriggerExit2D(Collider2D collision)
    {
        // 플레이어가 점프대를 완전히 벗어나면 다시 점프 가능하도록 설정
        // 계속 밟고 통통 튀는 동작을 위해 필수적
        if (((1 << collision.gameObject.layer) & playerLayer) != 0)
        {
            _hasJumped = false;
        }
    }
}