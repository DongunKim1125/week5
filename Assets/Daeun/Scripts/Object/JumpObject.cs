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
}
