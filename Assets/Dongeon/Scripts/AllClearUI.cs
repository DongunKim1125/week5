using UnityEngine;

/// <summary>
/// 모든 스테이지를 클리어했을 때 나타나는 화면의 UI 기능을 처리하는 클래스
/// </summary>
public class AllClearUI : MonoBehaviour
{
    /// <summary>
    /// 타이틀 화면으로 돌아가기
    /// </summary>
    public void OnBackToTitle()
    {
        DE_SoundManager.soundManager.PlaySFX(DE_SoundManager.sfx.uiclick);
        SceneLoader.BackToTitle();
    }

    /// <summary>
    /// 게임 전체 진행 상황 초기화 및 타이틀로 돌아가기 (선택 사항)
    /// </summary>
    public void OnResetAndTitle()
    {
        DE_SoundManager.soundManager.PlaySFX(DE_SoundManager.sfx.uiclick);
        PlayerPrefs.DeleteAll(); // 모든 저장 데이터 삭제
        Debug.Log("모든 진행 상황이 초기화되었습니다.");
        SceneLoader.BackToTitle();
    }
}
