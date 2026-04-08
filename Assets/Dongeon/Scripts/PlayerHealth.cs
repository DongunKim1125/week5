using UnityEngine;

/// <summary>
/// 플레이어의 체력과 사망 로직을 관리하며, 타일 외부(허공)로 나갔는지 체크함
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    private bool _isDead = false;

    private void Update()
    {
        if (_isDead) return;

        CheckVoidDeath();
    }

    /// <summary>
    /// 플레이어의 현재 위치가 유효한 타일 위인지 확인하고, 아니면 사망 처리함
    /// </summary>
    private void CheckVoidDeath()
    {
        Vector2Int currentGridPos = GridManager.Instance.WorldToGrid(transform.position);
        Tile currentTile = GridManager.Instance.GetTileAt(currentGridPos);

        // 타일이 없는 허공이거나 그리드 범위를 벗어났다면 사망 처리
        if (currentTile == null || !GridManager.Instance.IsInBounds(currentGridPos))
        {
            Die();
        }
    }

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;

        // 사망 로그 출력 후 즉시 현재 스테이지 재시작
        Debug.Log("<color=red>플레이어 사망! 스테이지를 재시작합니다.</color>");
        SceneLoader.ReloadCurrentScene();
    }
}
