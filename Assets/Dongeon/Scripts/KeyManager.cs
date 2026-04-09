using UnityEngine;

/// <summary>
/// 열쇠 획득 이벤트를 관리하는 싱글톤 클래스 (ID 방식)
/// </summary>
public class KeyManager : MonoBehaviour
{
    private static KeyManager _instance;
    public static KeyManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<KeyManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("KeyManager");
                    _instance = go.AddComponent<KeyManager>();
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// 열쇠를 획득했을 때 호출됨
    /// </summary>
    /// <param name="keyID">획득한 열쇠의 고유 번호 (lockID와 매칭됨)</param>
    public void OnKeyCollected(int keyID)
    {
        Debug.Log($"{keyID}번 열쇠 획득! 연관된 타일을 해제합니다.");

        // 모든 타일을 찾아 lockID가 일치하는 타일의 잠금을 해제함
        Tile[] allTiles = FindObjectsByType<Tile>(FindObjectsSortMode.None);
        foreach (Tile tile in allTiles)
        {
            if (tile.Type == TileType.KeyLocked && tile.LockID == keyID)
            {
                DE_SoundManager.soundManager.PlaySFX(DE_SoundManager.sfx.unlock);
                tile.Unlock();
            }
        }
    }
}
