using UnityEngine;

/// <summary>
/// 타이틀 화면의 버튼 이벤트를 처리하는 클래스
/// </summary>
public class TitleUI : MonoBehaviour
{
    public void StartGame()
    {
        DE_SoundManager.soundManager.PlaySFX(DE_SoundManager.sfx.stageclick);
        // 게임 시작 시 스테이지 선택 화면으로 이동
        SceneLoader.GoToStageSelect();
    }

    public void QuitGame()
    {
        // 게임 종료
        DE_SoundManager.soundManager.PlaySFX(DE_SoundManager.sfx.uiclick);
        Application.Quit();
        Debug.Log("게임 종료 요청됨");
    }

    /// <summary>
    /// 모든 스테이지 클리어 데이터와 게임 진행 상황을 초기화합니다.
    /// </summary>
    public void ResetGameProgress()
    {
        DE_SoundManager.soundManager.PlaySFX(DE_SoundManager.sfx.uiclick);
        
        // PlayerPrefs에 저장된 모든 데이터 삭제
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        
        Debug.Log("<color=red>모든 스테이지 저장 데이터가 초기화되었습니다.</color>");
    }
}
