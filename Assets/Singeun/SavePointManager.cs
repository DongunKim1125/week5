using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어의 세이브 포인트를 관리하는 클래스
/// </summary>
public class SavePointManager : MonoBehaviour
{
    [Header("Save Point Settings")]
    [Tooltip("세이브 포인트를 시각적으로 표시할 프리팹 (없으면 기본 구체 생성)")]
    [SerializeField] private GameObject savePointPrefab;

    [Header("Save Hold Settings")]
    [Tooltip("세이브 포인트를 설치하기 위해 Z키를 꾹 눌러야 하는 시간")]
    [SerializeField] private float holdTimeToSave = 1.5f;

    [Header("Load Settings")]
    [Tooltip("세이브 포인트를 로드하기 위해 R키를 꾹 눌러야 하는 시간 (이 시간 동안 화면이 페이드 아웃됩니다)")]
    [SerializeField] private float holdTimeToLoad = 1.5f;

    // Load Fade State
    private float _currentHoldTime = 0f;
    private bool _isFadingIn = false;
    private float _fadeInTime = 0f;
    private UnityEngine.UI.Image _fadeImage;

    // Save Hold State
    private float _currentSaveHoldTime = 0f;

    private GameObject _currentSavePointVisual;
    private bool _hasSavePoint = false;

    // 저장된 데이터
    private Vector3 _savedPlayerPosition;
    private Dictionary<Tile, Vector2Int> _savedTilePositions = new Dictionary<Tile, Vector2Int>();

    private DE_PlayerController _playerController;
    private Rigidbody2D _playerRb;

    private void Start()
    {
        // 씬에서 플레이어를 찾습니다.
        _playerController = FindFirstObjectByType<DE_PlayerController>();
        if (_playerController != null)
        {
            _playerRb = _playerController.GetComponent<Rigidbody2D>();
        }
    }

    private void EnsureFadeImageExists()
    {
        if (_fadeImage != null) return;

        GameObject canvasGo = new GameObject("SavePointFadeCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // 최상단에 렌더링되도록
        
        GameObject imageGo = new GameObject("FadeImage");
        imageGo.transform.SetParent(canvasGo.transform, false);
        _fadeImage = imageGo.AddComponent<UnityEngine.UI.Image>();
        _fadeImage.color = new Color(0, 0, 0, 0);
        _fadeImage.raycastTarget = false; // 클릭 방해 방지
        
        RectTransform rt = _fadeImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void Update()
    {
        // 1. Z키 꾹 누르기 로직 (세이브 포인트 설치)
        if (Input.GetKey(KeyCode.Z))
        {
            TryCreateSavePoint();
        }
        else
        {
            ResetSaveHold();
        }

        EnsureFadeImageExists();

        // 2. 세이브 포인트로 돌아온 직후 화면이 다시 밝아지는(페이드 인) 효과
        if (_isFadingIn)
        {
            _fadeInTime -= Time.deltaTime;
            float alpha = Mathf.Clamp01(_fadeInTime / holdTimeToLoad);
            _fadeImage.color = new Color(0f, 0f, 0f, alpha);

            if (_fadeInTime <= 0f)
            {
                _isFadingIn = false;
            }
            return; // 페이드 인 도중에는 다시 세이브 로드 불가
        }
        
        // 3. R키 꾹 누르기 로직 (페이드 아웃 효과)
        if (Input.GetKey(KeyCode.R))
        {
            if (_hasSavePoint) // 세이브 포인트가 있을 때만 동작
            {
                _currentHoldTime += Time.deltaTime;
                
                float alpha = Mathf.Clamp01(_currentHoldTime / holdTimeToLoad);
                _fadeImage.color = new Color(0f, 0f, 0f, alpha);

                if (_currentHoldTime >= holdTimeToLoad)
                {
                    _currentHoldTime = 0f;
                    _fadeImage.color = new Color(0f, 0f, 0f, 1f);
                    
                    LoadSavePoint();

                    // 로드 완료 후 페이드 인(화면 밝아짐) 모드로 전환
                    _isFadingIn = true;
                    _fadeInTime = holdTimeToLoad;
                }
            }
        }
        else
        {
            // 키를 떼면 진행도 서서히 취소 (화면 밝아짐)
            if (_currentHoldTime > 0f)
            {
                _currentHoldTime -= Time.deltaTime * 2f; // 뗄 때는 누를 때보다 2배 빨리 복구됨
                if (_currentHoldTime < 0f) _currentHoldTime = 0f;
                
                float alpha = Mathf.Clamp01(_currentHoldTime / holdTimeToLoad);
                _fadeImage.color = new Color(0f, 0f, 0f, alpha);
            }
        }
    }

    private void ResetSaveHold()
    {
        if (_currentSaveHoldTime > 0f)
        {
            _currentSaveHoldTime = 0f;
        }
    }

    private void TryCreateSavePoint()
    {
        if (_playerController == null || _playerRb == null) return;

        // 플레이어가 움직이는 중인지 확인 (입력 중이거나 물리적 속도가 존재할 때)
        if (_playerController.IsInputting || _playerRb.linearVelocity.magnitude > 0.1f)
        {
            ResetSaveHold();
            return;
        }

        _currentSaveHoldTime += Time.deltaTime;

        if (_currentSaveHoldTime >= holdTimeToSave)
        {
            ResetSaveHold();
            CreateSavePoint();
        }
    }

    private void CreateSavePoint()
    {
        // 기존 세이브 포인트가 있으면 제거하여 하나만 존재하도록 보장합니다.
        if (_hasSavePoint)
        {
            RemoveSavePoint();
        }
        
        // 1. 현재 플레이어 위치 저장
        _savedPlayerPosition = _playerController.transform.position;

        // 2. 현재 타일 배치 상태 저장
        _savedTilePositions.Clear();
        GridManager gridManager = GridManager.Instance;
        if (gridManager != null)
        {
            for (int x = 0; x < gridManager.Width; x++)
            {
                for (int y = 0; y < gridManager.Height; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    Tile tile = gridManager.GetTileAt(pos);
                    if (tile != null)
                    {
                        _savedTilePositions[tile] = pos;
                    }
                }
            }
        }
        Debug.Log("중간 단계");

        // 3. 시각적 세이브 포인트 생성
        if (savePointPrefab != null)
        {
            _currentSavePointVisual = Instantiate(savePointPrefab, _savedPlayerPosition, Quaternion.identity);
        }
        else
        {
            // 임시 시각화 오브젝트(구체) 생성
            _currentSavePointVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _currentSavePointVisual.transform.position = _savedPlayerPosition;
            _currentSavePointVisual.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            
            // 플레이어 이동을 방해하지 않도록 콜라이더 제거
            Destroy(_currentSavePointVisual.GetComponent<Collider>());
            
            var renderer = _currentSavePointVisual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.green; // 세이브 포인트의 시각적 식별 색상
            }
        }

        _hasSavePoint = true;
        Debug.Log("세이브 포인트가 생성되었습니다.");
    }

    private void RemoveSavePoint()
    {
        if (!_hasSavePoint) return;

        if (_currentSavePointVisual != null)
        {
            Destroy(_currentSavePointVisual);
        }

        _savedTilePositions.Clear();
        _hasSavePoint = false;
        
        Debug.Log("세이브 포인트가 제거되었습니다.");
    }

    /// <summary>
    /// 저장된 세이브 포인트의 상태를 불러옵니다.
    /// </summary>
    public bool LoadSavePoint()
    {
        if (!_hasSavePoint)
        {
            Debug.LogWarning("불러올 세이브 포인트가 없습니다.");
            return false;
        }

        // 1. 플레이어 위치 복구
        if (_playerController != null)
        {
            _playerController.transform.position = _savedPlayerPosition;
            if (_playerRb != null)
            {
                _playerRb.linearVelocity = Vector2.zero; // 관성 초기화
            }
        }

        // 2. 타일 배치 상태 복구 (GridManager에 추가된 RestoreTileState 메서드 사용)
        if (GridManager.Instance != null)
        {
            GridManager.Instance.RestoreTileState(_savedTilePositions);
        }

        Debug.Log("세이브 포인트로 돌아왔습니다.");
        return true;
    }
}