using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 스테이지 리스트를 불러와 UI 버튼을 동적으로 생성하는 클래스
/// </summary>
public class StageSelectUI : MonoBehaviour
{
    [Header("Data References")]
    [SerializeField] private StageList stageList;     // 스테이지 데이터 목록
    
    [Header("UI Objects")]
    [SerializeField] private GameObject buttonPrefab; // 생성할 버튼 프리팹
    [SerializeField] private Transform container;      // 버튼들이 담길 부모 오브젝트 (예: Grid Layout)

    private void Start()
    {
        // 씬 시작 시 버튼 자동 생성
        PopulateStageButtons();
    }

    private void PopulateStageButtons()
    {
        if (stageList == null || buttonPrefab == null || container == null)
        {
            Debug.LogError("StageSelectUI: 필수 설정 값이 누락되었습니다.");
            return;
        }

        // 기존 버튼 초기화
        foreach (Transform child in container) Destroy(child.gameObject);

        // 데이터 리스트만큼 반복하며 버튼 생성
        foreach (StageData stage in stageList.stages)
        {
            GameObject btnObj = Instantiate(buttonPrefab, container);
            
            // 버튼 텍스트 설정
            TextMeshProUGUI text = btnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.text = stage.stageDisplayName;

            // 버튼 클릭 이벤트 설정
            Button btn = btnObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.interactable = stage.IsUnlocked; // PlayerPrefs에서 저장된 값 확인
                btn.onClick.AddListener(() => SceneLoader.LoadScene(stage.sceneName));
            }
        }
    }

    /// <summary>
    /// 타이틀 화면으로 돌아가기 버튼
    /// </summary>
    public void OnBackToTitle()
    {
        SceneLoader.BackToTitle();
    }
}
