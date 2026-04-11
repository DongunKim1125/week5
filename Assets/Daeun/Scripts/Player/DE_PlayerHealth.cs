using UnityEngine;
using System.Collections;

public class DE_PlayerHealth : MonoBehaviour
{
    [Header("Death Settings")]
    [SerializeField] private GameObject deathEffectPrefab; // 사망 시 생성할 프리팹
    [SerializeField] private float restartDelay = 1.5f;    // 재시작 전 대기 시간

    private bool _isDead = false;

    /// <summary>
    /// 씬 로드 직후 체크포인트가 있으면 해당 위치로 텔레포트함.
    /// 씬 리로드로 타일 배치는 초기화되고, 플레이어 위치만 체크포인트로 이동됨.
    /// </summary>
    private void Start()
    {
        if (CheckpointManager.HasCheckpoint)
        {
            Vector3 respawnPos = CheckpointManager.ConsumeCheckpoint();

            respawnPos.z = transform.position.z;
            transform.position = respawnPos;

            Debug.Log($"<color=cyan>[Checkpoint] 체크포인트에서 리스폰: {respawnPos}</color>");
        }
    }

    private void Update()
    {
        if (_isDead) return;
        CheckVoidDeath();
    }

    private void CheckVoidDeath()
    {
        Vector2Int currentGridPos = GridManager.Instance.WorldToGrid(transform.position);
        Tile currentTile = GridManager.Instance.GetTileAt(currentGridPos);

        if (currentTile == null || !GridManager.Instance.IsInBounds(currentGridPos))
        {
            Die();
        }
    }

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;
        DE_SoundManager.soundManager.PlaySFX(DE_SoundManager.sfx.die);

        Debug.Log("<color=red>플레이어 사망!</color>");

        // 1. 사망 프리팹 생성
        if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        }

        // 2. 플레이어 본체 숨기기 (사망 연출을 더 잘 보이게 함)
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers) r.enabled = false;

        if (TryGetComponent<Collider2D>(out var col)) col.enabled = false;
        if (TryGetComponent<Rigidbody2D>(out var rb)) rb.simulated = false;

        // 3. 지연 후 재시작 코루틴 시작
        StartCoroutine(RestartAfterDelay());
    }

    private IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSeconds(restartDelay);
        SceneLoader.ReloadCurrentScene();
    }
}