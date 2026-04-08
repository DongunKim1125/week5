using UnityEngine;

/// <summary>
/// 플레이어의 좌우 이동과 타일 기반 중력 제어를 담당하는 클래스
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    
    private Rigidbody2D _rb;
    private float _horizontalInput;
    private Vector3 _initialScale; // 플레이어의 초기 크기 저장

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _initialScale = transform.localScale; // 시작 시 인스펙터에 설정된 크기 저장
    }

    private void Update()
    {
        // 1. 좌우 입력 감지
        _horizontalInput = Input.GetAxisRaw("Horizontal");

        // 2. 현재 위치한 타일 확인 및 중력 처리
        UpdateGravityBasedOnTile();
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
}
