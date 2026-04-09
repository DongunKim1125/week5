using UnityEngine;

/// <summary>
/// 개별 스테이지 씬 내의 UI 버튼 이벤트를 처리하는 클래스
/// </summary>
public class StageUI : MonoBehaviour
{
    private void Update()
    {
        // 매 프레임마다 F12 키 입력 확인
        if (Input.GetKeyDown(KeyCode.F12))
        {
            OnSkipStage();
        }
    }

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

    /// <summary>
    /// 스킵 버튼 클릭 또는 F12 입력 시 다음 스테이지를 해금하고 스테이지 선택 화면으로 이동
    /// </summary>
    public void OnSkipStage()
    {
        // 1. 현재 씬에 배치된 StageClear 객체를 찾습니다.
        StageClear stageClear = FindFirstObjectByType<StageClear>();

        if (stageClear != null)
        {
            // 2. 클리어했을 때와 동일하게 다음 스테이지의 잠금을 해제합니다.
            stageClear.UnlockNextStage();
            Debug.Log("<color=yellow>스테이지 스킵: 다음 스테이지가 해금되었습니다.</color>");
        }
        else
        {
            Debug.LogWarning("씬에서 StageClear 오브젝트를 찾을 수 없어 해금을 건너뜁니다.");
        }

        // 3. 플레이어가 해금된 다음 스테이지를 바로 확인할 수 있도록 선택 화면으로 이동합니다.
        SceneLoader.GoToStageSelect();
    }
}