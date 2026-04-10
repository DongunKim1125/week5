using UnityEngine;

/// <summary>
/// Height-based bounce object.
/// </summary>
public class JumpObject : MonoBehaviour
{
    [Header("Bounce Settings (Height Based)")]
    [Tooltip("Keeps the original height feel by scaling the impact height.")]
    [SerializeField] private float heightMultiplier = 1.0f;

    [Tooltip("Extra bonus height when hit from above.")]
    [SerializeField] private float topBonusHeight = 3f;
    [Tooltip("Extra bonus height when hit from the side.")]
    [SerializeField] private float sideBonusHeight = 1.5f;
    [Tooltip("Extra bonus height when hit from below.")]
    [SerializeField] private float bottomBonusHeight = 0f;
    [Tooltip("Minimum target height for side hits so walking into the block still bounces away.")]
    [SerializeField] private float sideMinimumTargetHeight = 2.0f;

    [Header("Input Lock Settings")]
    [Tooltip("Input lock time when hit from above.")]
    [SerializeField] private float topInputLockTime = 0.15f;
    [Tooltip("Input lock time when hit from the side.")]
    [SerializeField] private float sideInputLockTime = 0.1f;
    [Tooltip("Input lock time when hit from below.")]
    [SerializeField] private float bottomInputLockTime = 0.2f;

    [Header("Target Layer")]
    [SerializeField] private LayerMask playerLayer;
    
    [Header("Visual Settings (Squash & Stretch)")]
    [Tooltip("출렁거리는 애니메이션을 담당할 그래픽(SpriteRenderer) 객체")]
    [SerializeField] private Transform visualTransform; // 블록의 그래픽(스프라이트) 트랜스폼
    [Tooltip("출렁임 효과의 최대 강도 제한 (너무 커지는 것 방지)")]
    [SerializeField] private float maxJiggleIntensity = 0.5f;
    [Tooltip("출렁임 애니메이션 속도")]
    [SerializeField] private float jiggleSpeed = 15f;

    [Tooltip("예상되는 최대 튕김 속도 (이 속도일 때 찌그러짐이 최대치가 됩니다)")]
    [SerializeField] private float maxExpectedSpeed = 30f;
    [Tooltip("속도에 따른 찌그러짐 강도 그래프 (X축: 0~1 속도 비율, Y축: 0~1 강도 비율)")]
    [SerializeField] private AnimationCurve jiggleIntensityCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("타격감이 시작될 최소 속도 (이보다 느리면 찌그러지지 않음)")]
    [SerializeField] private float minImpactSpeed = 2f; 
    [Tooltip("타격감이 최대가 될 속도 (이 속도에서 Ratio가 1이 됨)")]
    [SerializeField] private float maxImpactSpeed = 15f;
    
    private Coroutine jiggleCoroutine;
    private Vector3 originalScale;
    private Vector3 originalPosition;

    private void Start()
    {
        // visualTransform을 할당하지 않았다면 자기 자신을 사용
        if (visualTransform == null) visualTransform = transform;
        originalScale = visualTransform.localScale;
        originalPosition = visualTransform.localPosition;
    }

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

        if (controller == null || rb == null || collision.contactCount == 0)
            return;

        Vector2 bounceDirection = GetBounceDirection(collision);

        // 점프 등 스스로 튀어오르는 방향으로 이미 이동 중일 때는 점프대 효과를 무시합니다.
        float velocityAlongBounce = Vector2.Dot(rb.linearVelocity, bounceDirection);
        if (velocityAlongBounce > 0.1f)
            return;

        DE_SoundManager.soundManager.PlaySFX(DE_SoundManager.sfx.jump);

        float gravity = Mathf.Max(0.01f, Mathf.Abs(Physics2D.gravity.y * rb.gravityScale));
        float impactSpeed = collision.relativeVelocity.magnitude;
        float fallHeight = (impactSpeed * impactSpeed) / (2f * gravity);

        float currentBonusHeight = 0f;
        if (controller.CanReceiveBounceBonus)
        {
            currentBonusHeight = GetDirectionalBonusHeight(bounceDirection);
            controller.CanReceiveBounceBonus = false;
        }

        float targetHeight = (fallHeight * heightMultiplier) + currentBonusHeight;
        targetHeight = Mathf.Max(targetHeight, GetDirectionalMinimumTargetHeight(bounceDirection));
        float requiredSpeed = Mathf.Sqrt(2f * gravity * targetHeight);

        Vector2 appliedForce = bounceDirection * requiredSpeed;
        controller.ApplyExternalForce(appliedForce);
        controller.InputLockTimer = GetDirectionalInputLockTime(bounceDirection);
        controller.GetComponentInChildren<DE_PlayerVisuals>()?.TriggerBounce(bounceDirection);

        if (jiggleCoroutine != null) StopCoroutine(jiggleCoroutine);
        jiggleCoroutine = StartCoroutine(JiggleRoutine(requiredSpeed, bounceDirection));

        Debug.Log($"[Bounce] fallHeight: {fallHeight:F1} | targetHeight: {targetHeight:F1} | speed: {requiredSpeed:F1}");
    }

    private Vector2 GetBounceDirection(Collision2D collision)
    {
        Collider2D selfCollider = GetComponent<Collider2D>();
        Vector2 blockCenter = selfCollider != null ? (Vector2)selfCollider.bounds.center : (Vector2)transform.position;
        Vector2 playerCenter = collision.collider != null ? (Vector2)collision.collider.bounds.center : (Vector2)collision.transform.position;
        Vector2 delta = playerCenter - blockCenter;
        Vector2 blockExtents = selfCollider != null ? selfCollider.bounds.extents : Vector2.one;
        Vector2 normalizedDelta = new Vector2(
            Mathf.Abs(blockExtents.x) > 0.0001f ? Mathf.Abs(delta.x) / blockExtents.x : Mathf.Abs(delta.x),
            Mathf.Abs(blockExtents.y) > 0.0001f ? Mathf.Abs(delta.y) / blockExtents.y : Mathf.Abs(delta.y)
        );

        if (normalizedDelta.x > normalizedDelta.y * 1.1f)
            return new Vector2(Mathf.Sign(delta.x), 0f);

        if (normalizedDelta.y > normalizedDelta.x * 1.1f)
            return new Vector2(0f, Mathf.Sign(delta.y));

        Vector2 normal = collision.contacts[0].normal;
        if (Mathf.Abs(normal.x) > Mathf.Abs(normal.y))
            return new Vector2(Mathf.Sign(-normal.x), 0f);

        return -normal.normalized;
    }

    private float GetDirectionalBonusHeight(Vector2 bounceDirection)
    {
        if (bounceDirection.y > 0.5f)
            return topBonusHeight;

        if (bounceDirection.y < -0.5f)
            return bottomBonusHeight;

        return sideBonusHeight;
    }

    private float GetDirectionalInputLockTime(Vector2 bounceDirection)
    {
        if (bounceDirection.y > 0.5f)
            return topInputLockTime;

        if (bounceDirection.y < -0.5f)
            return bottomInputLockTime;

        return sideInputLockTime;
    }

    private float GetDirectionalMinimumTargetHeight(Vector2 bounceDirection)
    {
        if (bounceDirection.y > 0.5f)
            return 0f;

        if (bounceDirection.y < -0.5f)
            return 0f;

        return sideMinimumTargetHeight;
    }

    // 매개변수 이름을 헷갈리지 않게 intensity에서 impactSpeed로 변경했습니다.
    private System.Collections.IEnumerator JiggleRoutine(float impactSpeed, Vector2 bounceDirection)
    {
        // 0.06~0.14 같은 좁은 범위를 0.0~1.0으로 쫙 늘려줍니다.
        // impactSpeed가 min보다 작으면 0, max보다 크면 1을 반환합니다.
        float speedRatio = Mathf.InverseLerp(minImpactSpeed, maxImpactSpeed, impactSpeed);
        
        float curveMultiplier = jiggleIntensityCurve.Evaluate(speedRatio);
        float finalIntensity = curveMultiplier * maxJiggleIntensity;

        // 최소한의 타격감을 위해 값이 0이 되는 것만 방지
        finalIntensity = Mathf.Max(finalIntensity, 0.05f); 

        float time = 0f;
        while (time < 1f)
        {
            time += Time.deltaTime * jiggleSpeed;
            float dampening = 1f - time; 
            float squash = Mathf.Abs(Mathf.Sin(time * Mathf.PI * 3f) * dampening * finalIntensity); 

            // 🚨 핵심 수정 부분: 스케일과 위치를 동시에 조절합니다.
            if (Mathf.Abs(bounceDirection.y) > Mathf.Abs(bounceDirection.x))
            {
                // 위/아래로 부딪혔을 때 (Y축 찌그러짐)
                visualTransform.localScale = originalScale - new Vector3(0f, squash, 0f);

                // 찌그러지는 방향의 반대쪽으로 위치를 이동시켜 면을 고정합니다.
                // bounceDirection.y가 양수(위에서 밟음)이면, 위쪽 면만 아래로 내려가야 하므로 반대로 +Y 방향으로 이동
                // bounceDirection.y가 음수(아래서 머리로 박음)이면, 아래쪽 면만 위로 올라가야 하므로 반대로 -Y 방향으로 이동
                float offset = (squash * originalScale.y) / 2f; // 스케일 변화량의 절반만큼 이동
                visualTransform.localPosition = originalPosition + new Vector3(0f, offset * -bounceDirection.y, 0f);
            }
            else
            {
                // 양옆으로 부딪혔을 때 (X축 찌그러짐)
                visualTransform.localScale = originalScale - new Vector3(squash, 0f, 0f);

                // X축 면 고정 (bounceDirection.x가 양수이면, 오른쪽 면만 왼쪽으로 들어가야 하므로 반대로 +X 방향 이동)
                float offset = (squash * originalScale.x) / 2f;
                visualTransform.localPosition = originalPosition + new Vector3(offset * -bounceDirection.x, 0f, 0f);
            }

            yield return null;
        }

        // 애니메이션이 끝나면 원래 크기와 위치로 복구
        visualTransform.localScale = originalScale;
        visualTransform.localPosition = originalPosition;
    }
}
