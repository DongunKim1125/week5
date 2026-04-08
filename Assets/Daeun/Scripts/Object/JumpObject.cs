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
        // 씬 내의 플레이어를 찾음 (성능 최적화가 필요할 경우 참조 저장 권장)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        
        // 지면에 닿아있지 않을 때만 최고 높이 갱신
        // DE_PlayerController의 _isGrounded 상태를 확인하거나 물리 엔진 속도로 판단
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

        // 1. 목표 높이 계산
        // 플레이어 최고점 + 타일 크기의 1/2
        float targetHeightValue = _playerMaxY + (tileSize.y * 0.5f);
        float currentY = player.transform.position.y;
        float displacementY = Mathf.Abs(targetHeightValue - currentY);

        // 2. 필요한 초기 속도 계산 (에너지 보존 법칙 활용)
        // v = sqrt(2 * g * h)
        float gravity = Mathf.Abs(Physics2D.gravity.y * rb.gravityScale);
        float requiredVelocityY = Mathf.Sqrt(2 * gravity * displacementY);

        // 3. 중력 방향에 따른 속도 적용
        float direction = rb.gravityScale > 0 ? 1f : -1f;
        
        // X축 속도는 유지, Y축은 계산된 속도로 덮어쓰기 (포물선 운동 시작)
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, requiredVelocityY * direction);

        _hasJumped = true;
        Debug.Log($"Jump Triggered! Target Height: {targetHeightValue}");
    }

    private void ResetJumpData()
    {
        _playerMaxY = float.MinValue;
        _hasJumped = false;
        Debug.Log("Tile Moved: Jump Data Reset.");
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        // 플레이어가 점프대를 완전히 벗어나면 다시 점프 가능하도록 설정 (필요 시)
        if (((1 << collision.gameObject.layer) & playerLayer) != 0)
        {
            _hasJumped = false;
        }
    }
}