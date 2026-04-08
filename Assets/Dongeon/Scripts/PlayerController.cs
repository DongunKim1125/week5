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

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
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
            // 일반 중력: 1.0, 반전 중력: -1.0
            float targetGravity = currentTile.InvertGravity ? -1f : 1f;
            _rb.gravityScale = targetGravity;

            // 중력 반전 시 캐릭터 위아래 뒤집기 (시각적 연출)
            transform.localScale = new Vector3(1, currentTile.InvertGravity ? -1 : 1, 1);
        }
    }
}
