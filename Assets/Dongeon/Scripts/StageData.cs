using UnityEngine;

[CreateAssetMenu(fileName = "NewStageData", menuName = "DongeonSystem/StageData")]
public class StageData : ScriptableObject
{
    public string stageDisplayName;
    public string sceneName;
    [SerializeField] private bool isDefaultUnlocked = false; // 기본 해금 여부 (1번 스테이지만 true)
    
    [TextArea]
    public string stageDescription;

    /// <summary>
    /// PlayerPrefs를 사용하여 스테이지 해금 상태를 관리함 (영구 저장)
    /// </summary>
    public bool IsUnlocked
    {
        get
        {
            // 1번 스테이지이거나 이미 해금된 경우 true 반환
            if (isDefaultUnlocked) return true;
            return PlayerPrefs.GetInt(sceneName + "_Unlocked", 0) == 1;
        }
        set
        {
            PlayerPrefs.SetInt(sceneName + "_Unlocked", value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    // 에디터에서 테스트하기 편하도록 초기화하는 기능 (선택 사항)
    [ContextMenu("Reset Unlock Status")]
    public void ResetUnlock()
    {
        PlayerPrefs.SetInt(sceneName + "_Unlocked", 0);
        PlayerPrefs.Save();
    }
}
