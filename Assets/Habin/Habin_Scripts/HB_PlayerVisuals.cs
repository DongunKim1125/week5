using UnityEngine;

public class DE_PlayerVisuals : MonoBehaviour
{
    [Header("Squash & Stretch Settings")]
    [Tooltip("원래 크기로 돌아오는 속도")]
    [SerializeField] private float returnSpeed = 15f; 
    
    [Tooltip("점프할 때 (가로축 감소, 세로축 증가)")]
    [SerializeField] private Vector2 jumpSquash = new Vector2(0.7f, 1.3f); 
    
    [Tooltip("착지할 때 (가로축 증가, 세로축 감소)")]
    [SerializeField] private Vector2 landSquash = new Vector2(1.4f, 0.6f); 
    
    [Tooltip("점프대에서 튕길 때 (매우 얇고 길게)")]
    [SerializeField] private Vector2 bounceSquash = new Vector2(0.4f, 1.8f); 

    private Vector3 _originalScale;
    private Vector3 _targetScale;

    private void Awake()
    {
        _originalScale = transform.localScale;
        _targetScale = _originalScale;
    }

    private void Update()
    {
        // 1. 매 프레임마다 현재 크기에서 목표 크기로 부드럽게 변형
        transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, Time.deltaTime * returnSpeed);
        
        // 2. 목표 크기는 항상 서서히 원래 크기(1, 1, 1)로 복원됨 (탄성 효과)
        _targetScale = Vector3.Lerp(_targetScale, _originalScale, Time.deltaTime * (returnSpeed * 0.5f));
    }

    public void TriggerJump()
    {
        ApplySquash(jumpSquash);
    }

    public void TriggerLand()
    {
        ApplySquash(landSquash);
    }

    public void TriggerBounce(Vector2 bounceDirection)
    {
        // 튕기는 방향이 수평(좌우 벽)인지 수직(바닥/천장)인지에 따라 찌그러지는 방향을 반전
        if (Mathf.Abs(bounceDirection.x) > Mathf.Abs(bounceDirection.y))
        {
            ApplySquash(new Vector2(bounceSquash.y, bounceSquash.x)); // 좌우로 튕길 땐 가로로 길어짐
        }
        else
        {
            ApplySquash(bounceSquash);
        }
    }

    private void ApplySquash(Vector2 modifier)
    {
        _targetScale = new Vector3(_originalScale.x * modifier.x, _originalScale.y * modifier.y, _originalScale.z);
    }
}