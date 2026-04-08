using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 깃발 오브젝트에 부착하여 스테이지 클리어와 다음 스테이지 해제를 처리하는 클래스
/// </summary>
public class StageClear : MonoBehaviour
{
    [Header("Data References")]
    [SerializeField] private StageList stageList;     // 전체 스테이지 목록
    [SerializeField] private float loadNextDelay = 2.0f; // 다음 씬 로딩 전 대기 시간

    [Header("Effects")]
    [SerializeField] private ParticleSystem clearParticles; // 클리어 시 터질 폭죽 파티클

    private bool _isCleared = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 1. 플레이어와 부딪혔고 아직 클리어되지 않았다면
        if (!_isCleared && other.CompareTag("Player"))
        {
            StartCoroutine(ClearSequence());
        }
    }

    private IEnumerator ClearSequence()
    {
        _isCleared = true;
        Debug.Log("<color=green>스테이지 클리어!</color>");

        // 2. 폭죽 파티클 실행
        if (clearParticles != null)
        {
            clearParticles.Play();
        }

        // 3. 현재 스테이지 정보 찾기 및 다음 스테이지 해제
        UnlockNextStage();

        // 4. 잠시 대기 후 다음 씬으로 이동
        yield return new WaitForSeconds(loadNextDelay);
        LoadNextStageScene();
    }

    private void UnlockNextStage()
    {
        if (stageList == null) return;

        string currentSceneName = SceneManager.GetActiveScene().name;

        // 현재 씬의 인덱스를 리스트에서 찾음
        for (int i = 0; i < stageList.stages.Count; i++)
        {
            if (stageList.stages[i].sceneName == currentSceneName)
            {
                // 다음 인덱스의 스테이지가 있다면 잠금 해제 (PlayerPrefs 저장)
                if (i + 1 < stageList.stages.Count)
                {
                    stageList.stages[i + 1].IsUnlocked = true;
                    Debug.Log($"<color=cyan>해금 완료!</color> {stageList.stages[i+1].stageDisplayName} (Scene: {stageList.stages[i+1].sceneName})");
                }
                break;
            }
        }
    }

    private void LoadNextStageScene()
    {
        if (stageList == null) return;

        string currentSceneName = SceneManager.GetActiveScene().name;

        for (int i = 0; i < stageList.stages.Count; i++)
        {
            if (stageList.stages[i].sceneName == currentSceneName)
            {
                // 다음 씬이 있다면 로딩, 없으면 스테이지 선택 화면으로
                if (i + 1 < stageList.stages.Count)
                {
                    SceneLoader.LoadScene(stageList.stages[i + 1].sceneName);
                }
                else
                {
                    Debug.Log("모든 스테이지를 클리어했습니다!");
                    SceneLoader.GoToStageSelect();
                }
                return;
            }
        }
    }
}
