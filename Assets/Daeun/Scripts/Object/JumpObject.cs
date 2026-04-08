using UnityEngine;

/// <summary>
/// 점프대가 바라보는 방향(transform.up)을 기준으로 플레이어를 튕겨내는 오브젝트
/// </summary>
public class JumpObject : MonoBehaviour
{
    [Header("Jump Settings")]
    [Tooltip("점프대가 플레이어를 밀어내는 기본 힘")]
    [SerializeField] private float pushPower = 15f; 
    [SerializeField] private LayerMask playerLayer;

    private bool _hasJumped = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (_hasJumped) return;

        if (((1 << collision.gameObject.layer) & playerLayer) != 0)
        {
            ExecuteUnifiedJump(collision.gameObject);
        }
    }

    private void ExecuteUnifiedJump(GameObject player)
    {
        DE_PlayerController controller = player.GetComponent<DE_PlayerController>();
        if (controller == null) return;

        Vector2 jumpDirection = transform.up;

        // 방향 벡터에 밀어낼 힘(상수)을 곱하여 최종 Vector2 도출
        Vector2 appliedForce = jumpDirection * pushPower;
        
        // 플레이어에게 부드러운 힘 적용을 요청
        controller.ApplyExternalForce(appliedForce);

        _hasJumped = true;
        Debug.Log($"점프 발동! 힘 적용: {appliedForce}");
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (((1 << collision.gameObject.layer) & playerLayer) != 0)
        {
            _hasJumped = false;
        }
    }
}