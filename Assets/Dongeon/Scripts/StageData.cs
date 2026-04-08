using UnityEngine;

[CreateAssetMenu(fileName = "NewStageData", menuName = "DongeonSystem/StageData")]
public class StageData : ScriptableObject
{
    public string stageDisplayName; // 화면에 표시될 스테이지 이름 (예: "1-1. 시작")
    public string sceneName;        // 실제 유니티 씬 파일 이름
    public bool isUnlocked = false; // 기본적으로 잠금 상태
    
    [TextArea]
    public string stageDescription; // 스테이지 설명 (필요 시)
}
