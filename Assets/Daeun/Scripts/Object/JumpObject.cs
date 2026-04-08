using UnityEngine;

/// <summary>
/// 중력을 고려한 목표 높이(Height) 기반의 탄성 오브젝트
/// </summary>
public class JumpObject : MonoBehaviour
{
    [Header("Bounce Settings (Height Based)")]
    [Tooltip("기존에 떨어진 높이에 비례하여 얼마나 뛸 것인가 (1이면 떨어진 높이와 정확히 동일)")]
    [SerializeField] private float heightMultiplier = 1.0f; 
    
    [Tooltip("추가로 더 높이 뛸 보정 높이 (단위: 타일 칸 수 등 유니티 Unit 거리 기준)")]
    [SerializeField] private float bonusHeight = 3f; 

    [Header("Target Layer")]
    [SerializeField] private LayerMask playerLayer;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (((1 << collision.gameObject.layer) & playerLayer) != 0)
        {
            ExecuteHeightBasedBounce(collision);
        }
    }

    private void ExecuteHeightBasedBounce(Collision2D collision)
    {
        DE_PlayerController controller = collision.gameObject.GetComponent<DE_PlayerController>();
        Rigidbody2D rb = collision.gameObject.GetComponent<Rigidbody2D>();
        
        if (controller == null || rb == null) return;

        // 1. 튕겨나갈 방향 결정 (부딪힌 면의 수직 반대 방향)
        Vector2 bounceDirection = -collision.contacts[0].normal;

        // 2. 현재 플레이어가 받고 있는 중력 가속도 크기 구하기 (0으로 나누기 방지)
        float gravity = Mathf.Max(0.01f, Mathf.Abs(Physics2D.gravity.y * rb.gravityScale));

        // 3. 충돌 속도를 바탕으로 "내가 어느 정도 높이에서 떨어졌는가?" 가상의 높이 역산
        float impactSpeed = collision.relativeVelocity.magnitude;
        float fallHeight = (impactSpeed * impactSpeed) / (2f * gravity);

        // 4. 보정값 1회성 적용 로직
        float currentBonusHeight = 0f;
        if (controller.CanReceiveBounceBonus)
        {
            currentBonusHeight = bonusHeight;
            controller.CanReceiveBounceBonus = false; 
        }

        // 5. 최종 목표 높이 계산
        // (떨어진 높이 * 배율) + 1회성 보정 높이
        float targetHeight = (fallHeight * heightMultiplier) + currentBonusHeight;

        // 6. 목표 높이에 도달하기 위해 중력을 이겨내는 정확한 초기 속도 계산
        float requiredSpeed = Mathf.Sqrt(2f * gravity * targetHeight);

        // 7. 플레이어에게 힘 전달
        Vector2 appliedForce = bounceDirection * requiredSpeed;
        controller.ApplyExternalForce(appliedForce);

        Debug.Log($"[탄성 점프] 역산된 낙하높이: {fallHeight:F1} | 최종 목표높이: {targetHeight:F1} | 적용속도: {requiredSpeed:F1}");
    }
}