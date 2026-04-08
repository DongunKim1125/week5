using UnityEngine;

/// <summary>
/// 플레이어가 획득할 수 있는 열쇠 오브젝트 클래스
/// </summary>
public class Key : MonoBehaviour
{
    [Header("Key Settings")]
    [SerializeField] private int keyID = 0; // 이 열쇠와 매칭될 타일의 Lock ID
    [SerializeField] private Color keyColor = Color.white; // 플레이어에게 보여줄 색상
    [SerializeField] private SpriteRenderer spriteRenderer;

    public int KeyID => keyID;

    private void Awake()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = keyColor;
        }
    }

    /// <summary>
    /// 에디터 인스펙터에서 색상을 변경할 때 즉시 반영
    /// </summary>
    private void OnValidate()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = keyColor;
        }
    }
}
