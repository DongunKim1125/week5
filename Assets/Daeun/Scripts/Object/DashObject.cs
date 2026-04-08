using UnityEngine;

/// <summary>
/// 플레이어를 특정 방향으로 대쉬시키고, 중력 반대 방향으로 보정 힘을 가하는 오브젝트
/// </summary>
public class DashObject : MonoBehaviour
{
    // 인스펙터에서 선택할 수 있는 로컬 방향 목록
    public enum DashLocalDirection { Up, Down, Left, Right }

    [Header("Dash Settings")]
    [SerializeField] private DashLocalDirection dashDirection = DashLocalDirection.Up;
    [SerializeField] private float dashSpeed = 15f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Correction Settings")]
    [Tooltip("대쉬 중 중력의 영향을 상쇄하기 위해 안쪽(중력 반대 방향 등)으로 밀어주는 힘")]
    [SerializeField] private float correctionForce = 5f;

    [Header("Lock Settings")]
    [Tooltip("대쉬 직후 PlayerController의 MovePlayer()가 속도를 덮어쓰지 못하도록 차단할 시간 (초)")]
    [SerializeField] private float dashLockDuration = 0.3f;
    [Tooltip("같은 발판에서 재발동을 막을 쿨타임 (초)")]
    [SerializeField] private float cooldown = 0.5f;

    private float _cooldownTimer = 0f;

    private void Update()
    {
        if (_cooldownTimer > 0f)
            _cooldownTimer -= Time.deltaTime;
    }

    // 선택된 로컬 방향을 타일의 현재 회전값(World Space)으로 변환
    private Vector2 GetWorldDashDirection()
    {
        return dashDirection switch
        {
            DashLocalDirection.Up    => transform.up,
            DashLocalDirection.Down  => -transform.up,
            DashLocalDirection.Left  => -transform.right,
            DashLocalDirection.Right => transform.right,
            _ => transform.up
        };
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_cooldownTimer > 0f) return;

        if (((1 << collision.gameObject.layer) & playerLayer) != 0)
        {
            ExecuteDash(collision.gameObject);
        }
    }

    private void ExecuteDash(GameObject player)
    {
        Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
        if (rb == null) return;

        // 1. 설정된 방향으로의 대쉬 속도
        Vector2 worldDir = GetWorldDashDirection();
        Vector2 dashVelocity = worldDir * dashSpeed;

        // 2. 중력 보정 (안쪽 방향 보정)
        // 플레이어의 중력 스케일을 고려하여 중력의 반대 방향으로 보정 힘을 계산합니다.
        // 이를 통해 대쉬 중에 아래로 쳐지는 현상을 방지합니다.
        Vector2 gravityDirection = Physics2D.gravity.normalized * rb.gravityScale;
        Vector2 correctionVelocity = -gravityDirection * correctionForce;

        // 3. 속도 적용
        // 기존 속도를 초기화하고 대쉬 속도와 보정 속도를 합산하여 적용합니다.
        rb.linearVelocity = dashVelocity + correctionVelocity;

        // 4. PlayerController가 이 속도를 즉시 덮어쓰지 못하도록 잠금
        DE_PlayerController controller = player.GetComponent<DE_PlayerController>();
        if (controller != null)
            controller.DashLockTimer = dashLockDuration;

        // 5. 쿨타임 시작
        _cooldownTimer = cooldown;

        Debug.Log($"Dash! Local: {dashDirection}, World: {worldDir}, Correction: {correctionForce}");
    }

    private void OnDrawGizmos()
    {
        // 인스펙터에서 방향을 바꿀 때마다 씬 뷰에서 즉시 확인 가능하도록 화살표 표시
        Vector2 dir = GetWorldDashDirection();
        Gizmos.color = Color.cyan;
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + (Vector3)dir * 1.0f;

        Gizmos.DrawLine(startPos, endPos);
        
        // 화살표 머리 모양 (간이)
        Gizmos.DrawWireSphere(endPos, 0.1f);
    }
}