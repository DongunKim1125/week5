using UnityEngine;

/// <summary>
/// 모든 타일의 플랫폼 시각 설정을 한곳에서 관리하는 데이터 에셋입니다.
/// </summary>
[CreateAssetMenu(fileName = "TileColorSettings", menuName = "Settings/TileColorSettings")]
public class TileColorSettings : ScriptableObject
{
    [Header("Platform Colors")]
    [Tooltip("플랫폼의 기본 색상 (한곳에서 바꾸면 모든 타일이 변함)")]
    public Color platformDefaultColor = Color.white;
    public Color platformFixedColor = new Color(0.5f, 0.5f, 0.5f, 1f);
}
