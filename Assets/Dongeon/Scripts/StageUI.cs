using UnityEngine;

/// <summary>
/// 개별 스테이지 씬 내의 UI 버튼 이벤트를 처리하는 클래스
/// </summary>
public class StageUI : MonoBehaviour
{
    /// <summary>
    /// 스테이지 선택 화면으로 돌아가기
    /// </summary>
    public void OnBackToStageSelect()
    {
        SceneLoader.GoToStageSelect();
    }

    /// <summary>
    /// 현재 스테이지 재시작 (필요 시 버튼에 연결)
    /// </summary>
    public void OnRestartStage()
    {
        SceneLoader.ReloadCurrentScene();
    }
}
