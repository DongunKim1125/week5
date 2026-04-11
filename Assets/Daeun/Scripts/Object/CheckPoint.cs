using UnityEngine;

/// <summary>
/// 씬에 배치되는 체크포인트 오브젝트에 부착하는 컴포넌트.
/// 플레이어가 트리거 영역에 진입하면 해당 위치를 스폰 포인트로 등록함.
/// </summary>
public class CheckPoint : MonoBehaviour
{
    [Header("활성화 전 스프라이트")]
    [Tooltip("체크포인트 활성화 전에 표시할 스프라이트 (미설정 시 변경 없음)")]
    [SerializeField] private Sprite inactiveSprite;

    [Tooltip("활성화 후 스프라이트")]
    [SerializeField] private Sprite activeSprite;

    [Tooltip("활성화 시 재생할 파티클 (미설정 시 재생 안 함)")]
    [SerializeField] private ParticleSystem activateParticles;

    [Header("리스폰 위치")]
    [Tooltip("체크포인트 중심에서 실제 스폰 위치까지의 오프셋 (기본값: 그대로)")]
    [SerializeField] private Vector2 spawnOffset = Vector2.zero;

    private SpriteRenderer _spriteRenderer;
    private bool _isActivated = false;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();

        // 최초엔 비활성화 스프라이트로 초기화
        if (_spriteRenderer != null && inactiveSprite != null)
            _spriteRenderer.sprite = inactiveSprite;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isActivated) return;
        if (!other.CompareTag("Player")) return;

        Activate();
    }

    private void Activate()
    {
        _isActivated = true;

        // 스폰 위치: 체크포인트 월드 좌표 + 오프셋
        Vector3 spawnPos = transform.position + new Vector3(spawnOffset.x, spawnOffset.y, 0f);
        CheckpointManager.SetCheckpoint(spawnPos);

        // 스프라이트 교체
        if (_spriteRenderer != null && activeSprite != null)
            _spriteRenderer.sprite = activeSprite;

        // 파티클 재생
        if (activateParticles != null)
            activateParticles.Play();

        // 사운드 넣는다면
        // DE_SoundManager.soundManager.PlaySFX(DE_SoundManager.sfx.checkpoint);
    }


    /// Scene 뷰에서 스폰 위치 오프셋을 시각적으로 확인하기 위한 Gizmo
  
    private void OnDrawGizmos()
    {
        Gizmos.color = _isActivated ? Color.green : Color.yellow;
        Vector3 spawnPos = transform.position + new Vector3(spawnOffset.x, spawnOffset.y, 0f);
        Gizmos.DrawWireSphere(spawnPos, 0.25f);
        Gizmos.DrawLine(transform.position, spawnPos);
    }
}