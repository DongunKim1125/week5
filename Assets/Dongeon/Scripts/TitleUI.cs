using UnityEngine;

/// <summary>
/// 타이틀 화면의 버튼 이벤트를 처리하는 클래스
/// </summary>
public class TitleUI : MonoBehaviour
{
    public void StartGame()
    {
        // 게임 시작 시 스테이지 선택 화면으로 이동
        SceneLoader.GoToStageSelect();
    }

    public void QuitGame()
    {
        // 게임 종료
        Application.Quit();
        Debug.Log("게임 종료 요청됨");
    }
}
