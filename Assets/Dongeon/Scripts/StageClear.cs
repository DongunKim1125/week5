using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// 깃발 오브젝트에 부착하여 스테이지 클리어 연출과 다음 스테이지 해제를 처리하는 클래스
/// </summary>
public class StageClear : MonoBehaviour
{
    [Header("Data References")]
    [SerializeField] private StageList stageList;     
    [SerializeField] private float loadNextDelay = 2.5f; // 연출을 위해 대기 시간을 조금 늘리는 것이 좋습니다.

    [Header("Effects & Animation")]
    [SerializeField] private ParticleSystem clearParticles;
    [Tooltip("회전하고 커질 실제 그래픽 오브젝트 (보통 자식 오브젝트)")]
    [SerializeField] private Transform targetVisual; 
    [SerializeField] private float finalScale = 30f;    // 화면을 덮을 최종 크기
    [SerializeField] private float rotationSpeed = 720f; // 초당 회전 각도
    
    [Header("Idle Settings")]
    [Tooltip("대기 상태일 때 회전 속도 (낮을수록 천천히 회전)")]
    [SerializeField] private float idleRotationSpeed = 20f; // 추가: 평상시 회전 속도

    private bool _isCleared = false;
    
    private void Update()
    {
        // 아직 클리어되지 않았고 회전시킬 타겟이 설정되어 있다면 매 프레임 회전
        if (!_isCleared && targetVisual != null)
        {
            targetVisual.Rotate(0, 0, idleRotationSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_isCleared && other.CompareTag("Player"))
        {
            StartCoroutine(ClearSequence());
        }
    }

    private IEnumerator ClearSequence()
    {
        _isCleared = true; // 클리어 상태가 되면 Update의 Idle 회전은 멈춤
        Debug.Log("<color=green>스테이지 클리어!</color>");

        if (clearParticles != null)
        {
            clearParticles.Play();
        }

        UnlockNextStage();

        float elapsed = 0f;
        Vector3 initialScale = targetVisual != null ? targetVisual.localScale : Vector3.one;

        while (elapsed < loadNextDelay)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / loadNextDelay;

            if (targetVisual != null)
            {
                // 2. 클리어 시에는 훨씬 빠른 rotationSpeed로 회전 및 거대화
                targetVisual.localScale = Vector3.Lerp(initialScale, Vector3.one * finalScale, t);
                targetVisual.Rotate(0, 0, rotationSpeed * Time.deltaTime);
            }

            yield return null;
        }

        LoadNextStageScene();
    }

    private void UnlockNextStage()
    {
        if (stageList == null) return;

        string currentSceneName = SceneManager.GetActiveScene().name;

        for (int i = 0; i < stageList.stages.Count; i++)
        {
            if (stageList.stages[i].sceneName == currentSceneName)
            {
                if (i + 1 < stageList.stages.Count)
                {
                    stageList.stages[i + 1].IsUnlocked = true;
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
                if (i + 1 < stageList.stages.Count)
                {
                    SceneLoader.LoadScene(stageList.stages[i + 1].sceneName);
                }
                else
                {
                    SceneLoader.LoadScene("AllClearScene");
                }
                return;
            }
        }
    }
}